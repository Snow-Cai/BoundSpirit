using UnityEngine;

public class GraveyardGateController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 2f;

    [Header("Requirements")]
    [SerializeField] private DialogueAsset requiredIntroDialogue; // Player must view this before leaving to next area
    [SerializeField] private string gatePuzzleID = "Chapter0_graveyard_gate";

    [Header("Dialogue Feedback")]
    [SerializeField] private DialogueAsset lockedWithoutNameDialogue; // Shown if player has not learned their name
    [SerializeField] private DialogueAsset lockedPuzzleDialogue;      // Hint before the player uses the puzzle
    [SerializeField] private bool openPuzzleAfterHintDialogue;

    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleUI;

    [Header("Gate Visuals")]
    [Tooltip("Visual object to disable when the gate is unlocked")]
    [SerializeField] private GameObject gateVisuals;

    public KeyCode InteractKey => interactKey;

    private Transform player;
    private bool puzzleHintShownThisSession;

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
        UnsubscribeFromDialogueEnded();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDialogueEnded();
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

        // 1. Player must know their name first.
        if (!HasPlayerSeenRequiredDialogue())
        {
            if (lockedWithoutNameDialogue != null && DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.StartDialogue(lockedWithoutNameDialogue);
            }

            // Optional: subtle guidance if they hit the gate too early.
            ObjectiveBanner.Instance?.ShowMessage("Maybe check your surroundings first…");

            return;
        }

        // 2. Player knows name but puzzle not solved.
        // First meaningful interaction: show a one-time hint, then always open the puzzle afterwards.
        bool hasSeenHintPersisted = HasSeenPuzzleHint();
        bool shouldShowHint =
            !puzzleHintShownThisSession &&
            !hasSeenHintPersisted &&
            lockedPuzzleDialogue != null &&
            DialogueSystem.Instance != null;

        if (shouldShowHint)
        {
            puzzleHintShownThisSession = true;

            // open the puzzle immediately after this hint dialogue finishes.
            openPuzzleAfterHintDialogue = true;
            SubscribeToDialogueEnded();

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

    private void SubscribeToDialogueEnded()
    {
        if (DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueEnded;
        DialogueSystem.Instance.OnDialogueEnded += HandleDialogueEnded;
    }

    private void UnsubscribeFromDialogueEnded()
    {
        if (DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleDialogueEnded(DialogueAsset finished)
    {
        if (!openPuzzleAfterHintDialogue)
            return;

        // Only react to the gate's hint dialogue.
        if (finished == null || lockedPuzzleDialogue == null)
            return;

        // If you have dialogueIDs, use them for precise matching.
        if (!string.IsNullOrEmpty(lockedPuzzleDialogue.dialogueID) &&
            finished.dialogueID != lockedPuzzleDialogue.dialogueID)
        {
            return;
        }

        openPuzzleAfterHintDialogue = false;
        UnsubscribeFromDialogueEnded();

        // If player walked away, don't force open.
        if (player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > interactionRange)
            return;

        if (IsGatePuzzleSolved())
            return;

        bool puzzleOpen = puzzleUI != null && puzzleUI.activeSelf;
        if (!puzzleOpen)
            OpenPuzzleUI();
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
