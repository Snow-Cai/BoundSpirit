using UnityEngine;

public class GraveyardGateController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 2f;

    [Header("Requirements")]
    [SerializeField] private DialogueAsset requiredIntroDialogue; // Player must view this before leaving to next area
    [SerializeField] private string gatePuzzleID = "Chapter0_graveyard_gate";

    [Header("Ghost requirements")]
    [Tooltip("GhostHintNPC ghostPuzzleID values when each spirit receives their item. Leave empty to not require ghosts.")]
    [SerializeField] private string[] requiredGhostPuzzleIds =
    {
        "graveyard_ghost_rose",
        "graveyard_ghost_crumpledPaper",
        "graveyard_ghost_key"
    };

    [SerializeField] private DialogueAsset lockedGhostsIncompleteDialogue;

    [Tooltip("Shown when ghosts are not all helped — in addition to lockedGhostsIncompleteDialogue when that plays.")]
    [SerializeField] private string ghostsIncompleteObjectiveMessage =
        "Help the spirits in the graveyard to get the gate code.";

    [Header("Dialogue Feedback")]
    [SerializeField] private DialogueAsset lockedWithoutNameDialogue; // Shown if player has not learned their name
    [SerializeField] private DialogueAsset lockedBeforeGateClueDialogue; // After name known; gate gravestone clue not read yet
    [Tooltip("Banner when the player should read the engraving on the stone beneath the gate first.")]
    [SerializeField] private string gateClueObjectiveMessage =
        "Examine the stone around the gate and read the engraving.";
    [SerializeField] private DialogueAsset lockedPuzzleDialogue;      // Hint before the player uses the puzzle (first time; puzzle shows as teaser, then closes)

    [Tooltip("If the player has read this dialogue (e.g. gate gravestone), skip lockedPuzzleDialogue — they already know about the engraving.")]
    [SerializeField] private string skipGateHintIfClueDialogueViewed = "Chapter0_gateCluePrimary";

    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleUI;

    [Header("Gate Visuals")]
    [Tooltip("Visual object to disable when the gate is unlocked")]
    [SerializeField] private GameObject gateVisuals;

    public KeyCode InteractKey => interactKey;

    private Transform player;
    private bool puzzleHintShownThisSession;

    private DialogueAsset deferredBannerAfterDialogue;
    private string deferredBannerMessage;

    private bool closePuzzleUiAfterBeforeGateClueDialogue;
    private bool closePuzzleUiAfterGateHintDialogue;
    private bool closePuzzleUiAfterGhostsIncompleteDialogue;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        if (IsGatePuzzleSolved())
        {
            ApplyGateUnlockedState();
        }
    }

    private void OnDisable()
    {
        if (IsPuzzleTeaserActive())
        {
            ClosePuzzleUI();
        }

        deferredBannerAfterDialogue = null;
        deferredBannerMessage = null;
        RemoveDialogueEndedSubscription();
    }

    private void OnDestroy()
    {
        if (IsPuzzleTeaserActive())
        {
            ClosePuzzleUI();
        }

        deferredBannerAfterDialogue = null;
        deferredBannerMessage = null;
        RemoveDialogueEndedSubscription();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        bool puzzleOpen = puzzleUI != null && puzzleUI.activeSelf;

        // When no puzzle is open, respect the global input lock.
        if (GameInputState.DialogueActive && !puzzleOpen)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // Allow closing the puzzle with Escape while it is open.
        if (puzzleOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzleUI();
            return;
        }

        if (distance <= interactionRange && Input.GetKeyDown(interactKey))
        {
            HandleGateInteraction();
        }
    }

    private void HandleGateInteraction()
    {
        if (IsGatePuzzleSolved())
        {
            Debug.Log("Gate is already unlocked. Player may proceed to the next area.");
            return;
        }

        if (IsPuzzleTeaserActive())
        {
            return;
        }

        // 1. Player must know their name first.
        if (!HasPlayerSeenRequiredDialogue())
        {
            const string earlyBanner = "Maybe check the surroundings graves first...";
            if (lockedWithoutNameDialogue != null && DialogueSystem.Instance != null)
            {
                QueueBannerAfterDialogue(lockedWithoutNameDialogue, earlyBanner);
                DialogueSystem.Instance.StartDialogue(lockedWithoutNameDialogue);
            }
            else
            {
                ObjectiveBanner.Instance?.ShowMessage(earlyBanner);
            }

            return;
        }

        if (!HasViewedGateGravestoneClue())
        {
            TryPlayNeedGateEngravingFeedback();
            return;
        }

        if (!AllRequiredGhostsHelped())
        {
            TryPlayGhostsIncompleteFeedback();
            return;
        }

        // 2. Player knows name, read the gate clue, and helped all required ghosts; puzzle not solved.
        // First time: show gate hint dialogue with puzzle UI as a teaser (closes when dialogue ends, like pre–clue flow).
        bool hasSeenHintPersisted = HasSeenPuzzleHint();
        bool shouldShowHint =
            !puzzleHintShownThisSession &&
            !hasSeenHintPersisted &&
            lockedPuzzleDialogue != null &&
            DialogueSystem.Instance != null;

        if (shouldShowHint)
        {
            puzzleHintShownThisSession = true;

            OpenPuzzleUI();
            closePuzzleUiAfterGateHintDialogue = true;
            QueueBannerAfterDialogue(lockedPuzzleDialogue, string.Empty);
            DialogueSystem.Instance.StartDialogue(lockedPuzzleDialogue);
            return;
        }

        // After the hint has been seen at least once, toggle the puzzle.
        bool puzzleOpen = puzzleUI != null && puzzleUI.activeSelf;

        if (puzzleOpen)
        {
            ClosePuzzleUI();
        }
        else
        {
            OpenPuzzleUI();
        }
    }

    private void TryPlayNeedGateEngravingFeedback()
    {
        if (lockedBeforeGateClueDialogue != null && DialogueSystem.Instance != null)
        {
            OpenPuzzleUI();
            closePuzzleUiAfterBeforeGateClueDialogue = true;
            QueueBannerAfterDialogue(lockedBeforeGateClueDialogue, gateClueObjectiveMessage);
            DialogueSystem.Instance.StartDialogue(lockedBeforeGateClueDialogue);
        }
        else
        {
            if (lockedBeforeGateClueDialogue != null && DialogueSystem.Instance == null)
            {
                Debug.LogWarning(
                    $"{nameof(GraveyardGateController)} on {name}: {nameof(lockedBeforeGateClueDialogue)} is assigned but {nameof(DialogueSystem)}.{nameof(DialogueSystem.Instance)} is null.",
                    this);
            }

            if (!string.IsNullOrWhiteSpace(gateClueObjectiveMessage))
            {
                ObjectiveBanner.Instance?.ShowMessage(gateClueObjectiveMessage);
            }
        }
    }

    private void TryPlayGhostsIncompleteFeedback()
    {
        if (lockedGhostsIncompleteDialogue != null && DialogueSystem.Instance != null)
        {
            OpenPuzzleUI();
            closePuzzleUiAfterGhostsIncompleteDialogue = true;
            QueueBannerAfterDialogue(lockedGhostsIncompleteDialogue, ghostsIncompleteObjectiveMessage);
            DialogueSystem.Instance.StartDialogue(lockedGhostsIncompleteDialogue);
        }
        else
        {
            if (lockedGhostsIncompleteDialogue != null && DialogueSystem.Instance == null)
            {
                Debug.LogWarning(
                    $"{nameof(GraveyardGateController)} on {name}: {nameof(lockedGhostsIncompleteDialogue)} is assigned but {nameof(DialogueSystem)}.{nameof(DialogueSystem.Instance)} is null.",
                    this);
            }

            if (!string.IsNullOrWhiteSpace(ghostsIncompleteObjectiveMessage))
            {
                ObjectiveBanner.Instance?.ShowMessage(ghostsIncompleteObjectiveMessage);
            }
        }
    }

    private void QueueBannerAfterDialogue(DialogueAsset dialogue, string bannerMessage)
    {
        deferredBannerAfterDialogue = dialogue;
        deferredBannerMessage = bannerMessage;
        EnsureDialogueEndedSubscription();
    }

    private void EnsureDialogueEndedSubscription()
    {
        if (DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.OnDialogueEnded -= OnGateDialogueEnded;
        DialogueSystem.Instance.OnDialogueEnded += OnGateDialogueEnded;
    }

    private void RemoveDialogueEndedSubscription()
    {
        if (DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.OnDialogueEnded -= OnGateDialogueEnded;
    }

    private void TryRemoveDialogueEndedSubscriptionIfIdle()
    {
        if (deferredBannerAfterDialogue != null ||
            IsPuzzleTeaserActive())
        {
            return;
        }

        RemoveDialogueEndedSubscription();
    }

    private static bool DialogueMatches(DialogueAsset expected, DialogueAsset finished)
    {
        if (expected == null || finished == null)
            return false;

        if (!string.IsNullOrEmpty(expected.dialogueID) && !string.IsNullOrEmpty(finished.dialogueID))
            return expected.dialogueID == finished.dialogueID;

        return ReferenceEquals(expected, finished);
    }

    private void OnGateDialogueEnded(DialogueAsset finished)
    {
        if (deferredBannerAfterDialogue != null && DialogueMatches(deferredBannerAfterDialogue, finished))
        {
            bool teaserWasBeforeGateClue =
                closePuzzleUiAfterBeforeGateClueDialogue &&
                lockedBeforeGateClueDialogue != null &&
                DialogueMatches(lockedBeforeGateClueDialogue, finished);

            bool teaserWasGateHint =
                closePuzzleUiAfterGateHintDialogue &&
                lockedPuzzleDialogue != null &&
                DialogueMatches(lockedPuzzleDialogue, finished);

            bool teaserWasGhostsIncomplete =
                closePuzzleUiAfterGhostsIncompleteDialogue &&
                lockedGhostsIncompleteDialogue != null &&
                DialogueMatches(lockedGhostsIncompleteDialogue, finished);

            if (!string.IsNullOrWhiteSpace(deferredBannerMessage))
            {
                ObjectiveBanner.Instance?.ShowMessage(deferredBannerMessage);
            }

            deferredBannerAfterDialogue = null;
            deferredBannerMessage = null;

            if (teaserWasBeforeGateClue || teaserWasGateHint || teaserWasGhostsIncomplete)
            {
                ClosePuzzleUI();
            }

            TryRemoveDialogueEndedSubscriptionIfIdle();
        }
    }

    private bool IsPuzzleTeaserActive()
    {
        return closePuzzleUiAfterBeforeGateClueDialogue ||
               closePuzzleUiAfterGateHintDialogue ||
               closePuzzleUiAfterGhostsIncompleteDialogue;
    }

    public bool CanUseGatePuzzleRunesAndSubmit()
    {
        if (!AllRequiredGhostsHelped())
        {
            return false;
        }

        return !IsPuzzleTeaserActive();
    }

    private void OpenPuzzleUI()
    {
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
        }

        GameInputState.DialogueActive = true;
    }

    private void ClosePuzzleUI()
    {
        closePuzzleUiAfterBeforeGateClueDialogue = false;
        closePuzzleUiAfterGateHintDialogue = false;
        closePuzzleUiAfterGhostsIncompleteDialogue = false;

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        GameInputState.DialogueActive = false;
    }

    private bool HasPlayerSeenRequiredDialogue()
    {
        return SaveSystem.Instance != null && SaveSystem.Instance.KnowsNameIsAkila();
    }

    private bool HasSeenPuzzleHint()
    {
        if (lockedPuzzleDialogue == null ||
            SaveSystem.Instance == null ||
            string.IsNullOrEmpty(lockedPuzzleDialogue.dialogueID))
        {
            return false;
        }

        return SaveSystem.Instance.HasViewedDialogue(lockedPuzzleDialogue.dialogueID);
    }

    private bool HasViewedGateGravestoneClue()
    {
        if (SaveSystem.Instance == null || string.IsNullOrEmpty(skipGateHintIfClueDialogueViewed))
        {
            return false;
        }

        return SaveSystem.Instance.HasViewedDialogue(skipGateHintIfClueDialogueViewed);
    }

    private bool AllRequiredGhostsHelped()
    {
        if (requiredGhostPuzzleIds == null || requiredGhostPuzzleIds.Length == 0)
        {
            return true;
        }

        if (SaveSystem.Instance == null)
        {
            return false;
        }

        foreach (string puzzleId in requiredGhostPuzzleIds)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                continue;
            }

            if (!SaveSystem.Instance.IsPuzzleSolved(puzzleId))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsGatePuzzleSolved()
    {
        if (SaveSystem.Instance == null || string.IsNullOrEmpty(gatePuzzleID))
        {
            return false;
        }

        return SaveSystem.Instance.IsPuzzleSolved(gatePuzzleID);
    }

    public void OnGatePuzzleSolved()
    {
        if (SaveSystem.Instance != null && !string.IsNullOrEmpty(gatePuzzleID))
        {
            SaveSystem.Instance.UnlockPuzzle(gatePuzzleID);
        }

        ClosePuzzleUI();
        ApplyGateUnlockedState();

        ObjectiveBanner.Instance?.ShowMessage("Objective complete: The graveyard gate is open.");

        Debug.Log("Gate puzzle solved. Gate unlocked.");
    }

    private void ApplyGateUnlockedState()
    {
        if (gateVisuals != null)
        {
            gateVisuals.SetActive(false);
        }

        enabled = false;
    }
}
