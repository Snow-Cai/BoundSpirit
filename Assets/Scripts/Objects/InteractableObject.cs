using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string objectName = "Object";
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f;
    public bool itemRequired = false;
    public ItemData requiredItem;

    [Header("Dialogue")]
    public DialogueAsset objectDialogue;
    public bool hasDialogue = true;
    [SerializeField] private bool triggerEndingSequence = false;

    [SerializeField] private bool playPrimaryOnlyOnce = false;
    [SerializeField] private DialogueAsset primaryDialogue;
    [SerializeField] private DialogueAsset repeatDialogue;

    [SerializeField] private DialogueAsset missingItemDialogue;

    [Header("Progress Flags")]
    [SerializeField] private bool setFoundHiddenTombstoneOnInteract = false;

    [Header("Puzzle")]
    public bool isPuzzle = false;
    public string puzzleID;
    public GameObject puzzleUI;
    public bool isPuzzleOpen = false;
    [Tooltip("Use this when another component on the same object handles the actual E-key interaction and puzzle open/close flow.")]
    public bool useExternalInteractionHandler = false;
    [Tooltip("If enabled, the player can still open this puzzle after it has already been solved.")]
    public bool allowSolvedPuzzleReopen = false;
    public bool timeRemainsOn = false;

    [Header("Puzzle Dialogue")]
    [SerializeField] private DialogueAsset puzzleFirstApproachDialogue;
    [SerializeField] private DialogueAsset puzzleSolvedDialogue;
    [SerializeField] private DialogueAsset puzzleSolvedRepeatDialogue;
    [SerializeField] private bool puzzlePlaySolvedDialogueOnlyOnce = true;
    [SerializeField] private bool openPuzzleAfterFirstApproachDialogue = true;
    [SerializeField] private string puzzleFirstApproachViewedKeyOverride;
    [SerializeField] private string puzzleSolvedViewedKeyOverride;

    [Header("Puzzle Components")]
    public LoginPuzzle loginPuzzle;

    [Header("Informational Tidbit")]
    public GameObject tidbitPopupCanvas;
    public InformationalTidbitData informationalTidbit;
    [TextArea]
    public string tidbitMessage;
    public bool showTidbitOnSolve = false;

    [Header("UI Prompt")]
    [Tooltip("If false, this object never shows/hides interactPrompt — use when InteractionHintUI (or one shared canvas) is the only prompt. Otherwise HidePrompt() can SetActive(false) on a shared UI.")]
    [SerializeField] private bool useLocalInteractPrompt = true;

    public GameObject interactPrompt;
    public TextMeshProUGUI promptText;

    [Tooltip("Legacy field kept for serialized scenes. Prompt show/hide now uses Interaction Range only.")]
    [HideInInspector]
    public float promptDistance = 3f;

    [Header("Audio")]
    public AudioClip interactSound;

    [Header("NPC")]
    private NPCController npcController;

    [Header("Visual Highlight")]
    [Tooltip("If enabled, this interactable sparkles while the player is within interaction range. NPCs are excluded.")]
    [SerializeField] private bool glowWhenInRange = true;
    [SerializeField] private InteractableGlow glow;

    private Transform player;
    private Collider2D objectCollider;
    private bool playerInRange = false;
    private DialogueAsset pendingPuzzleDialogueToWatch;
    private bool pendingOpenPuzzleAfterDialogue;

    private void Reset()
    {
        EnsureGlowReference();
    }

    private void OnDisable()
    {
        UnsubscribePuzzleDialogueHandler();
    }

    private void OnValidate()
    {
        EnsureGlowReference();
        SetGlow(false);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (useLocalInteractPrompt && interactPrompt != null && !IsSharedInteractionHintPrompt())
        {
            interactPrompt.SetActive(false);
        }

        if (useLocalInteractPrompt && promptText != null)
        {
            promptText.text = "Press " + interactKey.ToString() + " to interact";
        }

        npcController = GetComponent<NPCController>();
        objectCollider = GetComponent<Collider2D>();
        EnsureGlowReference();

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        // Ensure stale serialized scene state does not make a puzzle act as already open.
        isPuzzleOpen = false;
    }

    void Update()
    {
        if (player == null)
        {
            SetGlow(false);
            return;
        }

        float distance = Vector3.Distance(
            objectCollider != null ? objectCollider.bounds.center : transform.position,
            player.position
        );

        bool withinInteractRange = distance <= interactionRange;

        if (ShouldShowGlow(withinInteractRange))
        {
            SetGlow(true);
        }
        else
        {
            SetGlow(false);
        }

        if (isPuzzleOpen)
        {
            if (useExternalInteractionHandler)
            {
                return;
            }

            if (InputLock.Instance != null && InputLock.Instance.InteractEnabled && !IsTypingInUI() && Input.GetKeyDown(interactKey))
            {
                if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
                    return;
                ClosePuzzle();
            }

            return;
        }

        if (ShouldSuppressPromptOrInteraction())
        {
            if (playerInRange)
            {
                playerInRange = false;
                HidePrompt();
            }

            return;
        }

        if (withinInteractRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }

            if (useExternalInteractionHandler)
            {
                return;
            }

            if (InputLock.Instance != null &&
                InputLock.Instance.GameplayInputEnabled &&
                InputLock.Instance.InteractEnabled &&
                Input.GetKeyDown(interactKey))
            {
                if (!InteractionPriorityResolver.IsHighestPriorityTarget(this, player))
                {
                    return;
                }

                if (!InteractionPriorityResolver.TryConsumeInteraction())
                {
                    return;
                }

                Interact();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                HidePrompt();
            }
        }
    }

    void ShowPrompt()
    {
        if (!useLocalInteractPrompt || interactPrompt == null)
        {
            return;
        }

        if (IsSharedInteractionHintPrompt())
        {
            return;
        }

        if (promptText != null)
        {
            promptText.text = "Press " + interactKey.ToString() + " to interact";
        }

        interactPrompt.SetActive(true);
    }

    void HidePrompt()
    {
        if (!useLocalInteractPrompt || interactPrompt == null)
        {
            return;
        }

        if (IsSharedInteractionHintPrompt())
        {
            return;
        }

        if (ShouldKeepSharedPromptVisible())
        {
            return;
        }

        interactPrompt.SetActive(false);
    }

    private bool ShouldKeepSharedPromptVisible()
    {
        InteractableObject[] interactables = FindObjectsByType<InteractableObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null || interactable == this || !interactable.enabled)
            {
                continue;
            }

            if (!interactable.useLocalInteractPrompt || interactable.interactPrompt != interactPrompt)
            {
                continue;
            }

            if (!interactable.IsPlayerWithinInteractionRange())
            {
                continue;
            }

            if (promptText != null)
            {
                promptText.text = "Press " + interactable.interactKey.ToString() + " to interact";
            }

            interactPrompt.SetActive(true);
            return true;
        }

        return false;
    }

    private bool IsSharedInteractionHintPrompt()
    {
        if (interactPrompt == null)
        {
            return false;
        }

        return interactPrompt.GetComponent<InteractionHintUI>() != null ||
               interactPrompt.GetComponentInParent<InteractionHintUI>() != null;
    }

    private bool IsPlayerWithinInteractionRange()
    {
        return IsWithinInteractionRange(player);
    }

    void Interact()
    {
        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return;

        Debug.Log("INTERACT() FIRED on " + gameObject.name);

        //ALWAYS PLAY AUDIO FIRST
        //            if (pickupSound != null)
        //AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        //
        if (interactSound != null)
        {
            Debug.Log("PLAYING SOUND on " + gameObject.name);
            SfxPlayback.PlayClipAtPoint(interactSound, transform.position);

        }
        else
        {
            Debug.LogWarning("No interactSound or UIAudioManager missing on " + gameObject.name);
        }

        //Delay other logic by 1 frame so audio isn't swallowed
        StartCoroutine(DelayedInteractionLogic());
    }

    private IEnumerator DelayedInteractionLogic()
    {
        // Wait 1 frame to guarantee audio plays first
        yield return null;

        // If dialogue is already active, stop here
        if (DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsDialogueActive())
        {
            yield break;
        }

        if (itemRequired)
        {
            PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
            if(inv == null || requiredItem == null || !inv.HasItem(requiredItem))
            {
                if(missingItemDialogue != null && DialogueSystem.Instance != null)
                    DialogueSystem.Instance.StartDialogue(missingItemDialogue);
                yield break;
            }
        }

        if (setFoundHiddenTombstoneOnInteract && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetFoundHiddenTombstone(true);
        }

        // NPC interaction
        if (npcController != null)
        {
            npcController.StartInteraction();
        }

        // Ghost interaction
        GhostHintNPC ghost = GetComponent<GhostHintNPC>();
        if (ghost != null)
        {
            // Tiny delay so ghost dialogue doesn't swallow audio
            yield return new WaitForSecondsRealtime(.1f);
            ghost.Interact();
            yield break;
        }

        // Puzzle interaction
        if (isPuzzle && puzzleUI != null && !isPuzzleOpen)
        {
            if(puzzleID == "PaintingPuzzle" && LibraryPuzzleStateBridge.Instance != null && !LibraryPuzzleStateBridge.Instance.cipherHalfSolved)        // Special case for library connected puzzle states
            {
                if (hasDialogue) PlayDialogue();
                yield break;
            }
            
            bool puzzleAlreadySolved =
                SaveSystem.Instance != null &&
                !string.IsNullOrEmpty(puzzleID) &&
                SaveSystem.Instance.IsPuzzleSolved(puzzleID);

            if (!puzzleAlreadySolved && TryHandlePuzzleFirstApproachDialogue())
            {
                yield break;
            }

            if (puzzleAlreadySolved && TryHandleSolvedPuzzleDialogue())
            {
                yield break;
            }

            if (puzzleAlreadySolved && !allowSolvedPuzzleReopen)
            {
                if (hasDialogue)
                {
                    PlayDialogue();
                }

                yield break;
            }

            OpenPuzzle();

            if (puzzleAlreadySolved && hasDialogue)
            {
                PlayDialogue();
            }

            yield break;
        }

        // Dialogue interaction
        if (hasDialogue)
        {
            PlayDialogue();
        }
    }

    private bool TryHandlePuzzleFirstApproachDialogue()
    {
        if (puzzleFirstApproachDialogue == null || DialogueSystem.Instance == null)
        {
            return false;
        }

        string viewedKey = GetPuzzleDialogueKey(puzzleFirstApproachDialogue, puzzleFirstApproachViewedKeyOverride);
        bool alreadyViewed =
            SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(viewedKey) &&
            SaveSystem.Instance.HasViewedDialogue(viewedKey);

        if (alreadyViewed)
        {
            return false;
        }

        if (SaveSystem.Instance != null && !string.IsNullOrEmpty(viewedKey))
        {
            SaveSystem.Instance.MarkDialogueViewed(viewedKey);
        }

        pendingPuzzleDialogueToWatch = puzzleFirstApproachDialogue;
        pendingOpenPuzzleAfterDialogue = openPuzzleAfterFirstApproachDialogue;

        DialogueSystem.Instance.OnDialogueEnded -= HandlePuzzleDialogueEnded;
        DialogueSystem.Instance.OnDialogueEnded += HandlePuzzleDialogueEnded;
        DialogueSystem.Instance.StartDialogue(puzzleFirstApproachDialogue);
        return true;
    }

    private bool TryHandleSolvedPuzzleDialogue()
    {
        if (DialogueSystem.Instance == null)
        {
            return false;
        }

        DialogueAsset dialogueToPlay = null;
        string viewedKey = GetPuzzleDialogueKey(puzzleSolvedDialogue, puzzleSolvedViewedKeyOverride);
        bool solvedDialogueAlreadyViewed =
            SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(viewedKey) &&
            SaveSystem.Instance.HasViewedDialogue(viewedKey);

        if (!solvedDialogueAlreadyViewed || !puzzlePlaySolvedDialogueOnlyOnce)
        {
            dialogueToPlay = puzzleSolvedDialogue;
        }

        if (dialogueToPlay == null)
        {
            dialogueToPlay = puzzleSolvedRepeatDialogue;
        }

        if (dialogueToPlay == null)
        {
            return false;
        }

        OpenPuzzle();

        if (!solvedDialogueAlreadyViewed && SaveSystem.Instance != null && !string.IsNullOrEmpty(viewedKey))
        {
            SaveSystem.Instance.MarkDialogueViewed(viewedKey);
        }

        DialogueSystem.Instance.StartDialogue(dialogueToPlay);
        return true;
    }

    private void HandlePuzzleDialogueEnded(DialogueAsset asset)
    {
        if (!DialogueMatches(pendingPuzzleDialogueToWatch, asset))
        {
            return;
        }

        bool shouldOpenPuzzle = pendingOpenPuzzleAfterDialogue;

        pendingPuzzleDialogueToWatch = null;
        pendingOpenPuzzleAfterDialogue = false;
        UnsubscribePuzzleDialogueHandler();

        if (shouldOpenPuzzle)
        {
            OpenPuzzle();
        }
    }

    private void UnsubscribePuzzleDialogueHandler()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded -= HandlePuzzleDialogueEnded;
        }
    }

    private string GetPuzzleDialogueKey(DialogueAsset asset, string overrideKey)
    {
        if (!string.IsNullOrWhiteSpace(overrideKey))
        {
            return overrideKey;
        }

        return asset != null ? asset.dialogueID : string.Empty;
    }

    private static bool DialogueMatches(DialogueAsset expected, DialogueAsset finished)
    {
        if (expected == null || finished == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(expected.dialogueID) && !string.IsNullOrEmpty(finished.dialogueID))
        {
            return expected.dialogueID == finished.dialogueID;
        }

        return ReferenceEquals(expected, finished);
    }


    public void OpenPuzzle()
    {
        PuzzleBridge.currentPuzzleSource = this;

        if (loginPuzzle != null)
        {
            loginPuzzle.ResetFields();
        }

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
        }

        isPuzzleOpen = true;
        playerInRange = false;
        HidePrompt();
        SetGameplayInputEnabled(false);
        InputLock.Instance.CanToggleInventory = false;
        if (!timeRemainsOn) Time.timeScale = 0f;
    }


    public void ClosePuzzle()
    {
        if (loginPuzzle != null)
        {
            loginPuzzle.ResetFields();
        }

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        isPuzzleOpen = false;
        if (PuzzleBridge.currentPuzzleSource == this)
        {
            PuzzleBridge.currentPuzzleSource = null;
        }
        SetGameplayInputEnabled(true);
        InputLock.Instance.CanToggleInventory = true;
        Time.timeScale = 1f;
  
    }

    private void SetGameplayInputEnabled(bool enabled)
    {
        if (InputLock.Instance != null)
        {
            InputLock.Instance.GameplayInputEnabled = enabled;
        }
    }

    public void OnPuzzleSolved()
    {
        bool wasAlreadySolved = SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(puzzleID) &&
            SaveSystem.Instance.IsPuzzleSolved(puzzleID);

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(puzzleID);
        }

        Time.timeScale = 1f;

        if (hasDialogue)
        {
            PlayDialogue();
        }

        if (!wasAlreadySolved && showTidbitOnSolve)
        {
            UICluePopup popup = null;

            if (tidbitPopupCanvas != null)
            {
                popup = tidbitPopupCanvas.GetComponent<UICluePopup>();

                if (popup == null)
                {
                    popup = tidbitPopupCanvas.GetComponentInChildren<UICluePopup>(true);
                }
            }

            if (popup == null)
            {
                popup = Object.FindFirstObjectByType<UICluePopup>(FindObjectsInactive.Include);
            }

            if (popup == null)
            {
                GameObject popupPrefab = Resources.Load<GameObject>("PopupCanvas");
                if (popupPrefab != null)
                {
                    GameObject popupInstance = Object.Instantiate(popupPrefab);
                    popup = popupInstance.GetComponent<UICluePopup>();

                    if (popup == null)
                    {
                        popup = popupInstance.GetComponentInChildren<UICluePopup>(true);
                    }
                }
            }

            if (popup != null)
            {
                if (informationalTidbit != null)
                    popup.ShowTidbit(informationalTidbit);
                else if (!string.IsNullOrWhiteSpace(tidbitMessage))
                    popup.ShowTidbitMessage(tidbitMessage);
            }
        }
    }

    private void PlayDialogue()
    {
        if (DialogueSystem.Instance == null)
            return;
        // TEMP DEBUG
        Debug.Log("HasEnoughForEnding: " + StoryFlags.HasEnoughForEnding());
        Debug.Log("EndingManager exists: " + (EndingManager.Instance != null));

        if (triggerEndingSequence)
        {
            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.TriggerEnding();
                return;
            }
        }

        DialogueAsset dialogueToPlay =
            primaryDialogue != null ? primaryDialogue : objectDialogue;

        if (playPrimaryOnlyOnce &&
            primaryDialogue != null &&
            SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(primaryDialogue.dialogueID) &&
            SaveSystem.Instance.HasViewedDialogue(primaryDialogue.dialogueID))
        {
            if (repeatDialogue != null)
            {
                dialogueToPlay = repeatDialogue;
            }
        }

        if (dialogueToPlay != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogueToPlay);

            if (npcController != null)
            {
                DialogueSystem.Instance.ActiveNPC = npcController;
            }
        }
    }

    bool IsTypingInUI()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    public bool CanBeInteractedWith(Transform targetPlayer)
    {
        if (targetPlayer == null || !isActiveAndEnabled)
        {
            return false;
        }

        if (useExternalInteractionHandler)
        {
            return false;
        }

        if (GetComponent<CollectibleObject>() != null)
        {
            return false;
        }

        if (isPuzzleOpen)
        {
            return false;
        }

        if (ShouldSuppressPromptOrInteraction())
        {
            return false;
        }

        if (InputLock.Instance == null ||
            !InputLock.Instance.GameplayInputEnabled ||
            !InputLock.Instance.InteractEnabled)
        {
            return false;
        }

        return IsWithinInteractionRange(targetPlayer);
    }

    public bool IsDialoguePriorityTarget()
    {
        return npcController != null ||
               GetComponent<GhostHintNPC>() != null;
    }

    public bool IsPuzzlePriorityTarget()
    {
        return isPuzzle;
    }

    public float GetDistanceTo(Transform targetPlayer)
    {
        if (targetPlayer == null)
        {
            return float.MaxValue;
        }

        Vector3 promptOrigin = objectCollider != null ? objectCollider.bounds.center : transform.position;
        return Vector2.Distance(targetPlayer.position, promptOrigin);
    }

    public bool IsWithinInteractionRange(Transform targetPlayer)
    {
        return GetDistanceTo(targetPlayer) <= interactionRange;
    }

    private bool ShouldShowGlow(bool withinInteractRange)
    {
        if (GetComponent<CollectibleObject>() != null)
        {
            return false;
        }

        if (npcController != null || GetComponent<GhostHintNPC>() != null)
        {
            return false;
        }

        if (!glowWhenInRange)
        {
            return false;
        }

        if (isPuzzleOpen)
        {
            return false;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
        {
            return false;
        }

        if (InspectUI.Instance != null && InspectUI.Instance.IsOpen)
        {
            return false;
        }

        if (CaesarDecodePanel.IsPanelActuallyOpen || LibraryWordSearchPanel.IsPanelActuallyOpen)
        {
            return false;
        }

        return withinInteractRange;
    }

    private bool ShouldSuppressPromptOrInteraction()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
            return true;

        if (InspectUI.Instance != null && InspectUI.Instance.IsOpen)
            return true;

        if (CaesarDecodePanel.IsPanelActuallyOpen || LibraryWordSearchPanel.IsPanelActuallyOpen)
            return true;

        return false;
    }

    private void SetGlow(bool enabled)
    {
        if (glow == null)
        {
            return;
        }

        glow.SetHighlighted(enabled);
    }

    private void EnsureGlowReference()
    {
        npcController = GetComponent<NPCController>();
        GhostHintNPC ghostHint = GetComponent<GhostHintNPC>();
        CollectibleObject collectible = GetComponent<CollectibleObject>();

        if (npcController != null || ghostHint != null || collectible != null)
        {
            InteractableGlow existingGlow = glow != null ? glow : GetComponent<InteractableGlow>();
            if (existingGlow != null)
            {
                existingGlow.SetHighlighted(false);
            }
            glow = null;
            return;
        }

        if (glow == null)
        {
            glow = GetComponent<InteractableGlow>();
        }

        if (glow == null)
        {
            glow = gameObject.AddComponent<InteractableGlow>();
        }

        if (glow != null)
        {
            glow.ApplyStyle(InteractableGlow.HighlightStyle.InteractableSparkle);
        }
    }

}
