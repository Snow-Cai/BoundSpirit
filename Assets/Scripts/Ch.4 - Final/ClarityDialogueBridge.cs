using UnityEngine;

public class ClarityDialogueBridge : MonoBehaviour
{
    [Header("Clarity Choice")]
    [Tooltip("The DialogueAsset with the branching clarity choice. Must have a unique dialogueID set.")]
    public DialogueAsset clarityChoiceDialogue;

    //The InteractableObject on this same GameObject
    private InteractableObject interactable;

    private bool listeningForPrimary = false;

    private void Awake()
    {
        interactable = GetComponent<InteractableObject>();

        if (interactable == null)
            Debug.LogWarning($"[ClarityDialogueBridge] No InteractableObject found on {gameObject.name}. " +
                             "This component must be on the same GameObject.");

        if (clarityChoiceDialogue == null)
            Debug.LogWarning($"[ClarityDialogueBridge] clarityChoiceDialogue is not assigned on {gameObject.name}.");

        if (clarityChoiceDialogue != null && string.IsNullOrEmpty(clarityChoiceDialogue.dialogueID))
            Debug.LogWarning($"[ClarityDialogueBridge] clarityChoiceDialogue on {gameObject.name} has no dialogueID. " +
                             "The choice will fire every time instead of once.");
    }

    private void OnEnable()
    {
        // Subscribe to the global DialogueSystem started event so know when the primary dialogue begins
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueStarted += HandleDialogueStarted;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void HandleDialogueStarted(DialogueAsset started)
    {
        if (interactable == null || clarityChoiceDialogue == null)
            return;

        //Already queued / answered — nothing to do.
        if (ClarityChoiceAlreadyAnswered())
            return;

        //Only care about our own primaryDialogue starting
        DialogueAsset primary = GetPrimaryDialogue();
        if (primary == null || started != primary)
            return;

        //Subscribe to OnDialogueEnded to catch when primary finishes
        if (!listeningForPrimary && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded += HandlePrimaryDialogueEnded;
            listeningForPrimary = true;
        }
    }

    private void HandlePrimaryDialogueEnded(DialogueAsset ended)
    {
        if (interactable == null || clarityChoiceDialogue == null)
        {
            Unsubscribe();
            return;
        }

        DialogueAsset primary = GetPrimaryDialogue();
        if (primary == null || ended != primary)
            return;

        //only need to fire once
        Unsubscribe();

        // Double-check the choice hasn't been answered already
        // (edge case: save loaded mid-session)
        if (ClarityChoiceAlreadyAnswered())
            return;

        // Queue the clarity choice. It plays right after primary ends before the player can interact again.
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(clarityChoiceDialogue);
            Debug.Log($"[ClarityDialogueBridge] Queued clarity choice '{clarityChoiceDialogue.dialogueID}' " +
                      $"after primary dialogue on {gameObject.name}.");
        }
    }

    private void Unsubscribe()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueStarted -= HandleDialogueStarted;
            DialogueSystem.Instance.OnDialogueEnded -= HandlePrimaryDialogueEnded;
        }

        listeningForPrimary = false;
    }

    private bool ClarityChoiceAlreadyAnswered()
    {
        if (clarityChoiceDialogue == null)
            return true;

        if (string.IsNullOrEmpty(clarityChoiceDialogue.dialogueID))
            return false;

        return SaveSystem.Instance != null &&
               SaveSystem.Instance.HasViewedDialogue(clarityChoiceDialogue.dialogueID);
    }

    private DialogueAsset GetPrimaryDialogue()
    {
        if (interactable == null)
            return null;

        if (_primaryDialogueOverride != null)
            return _primaryDialogueOverride;

        Debug.LogWarning($"[ClarityDialogueBridge] primaryDialogueOverride is not assigned on {gameObject.name}. " +
                         "Assign the same DialogueAsset that is in InteractableObject.primaryDialogue.");
        return null;
    }

    [Header("Mirror of InteractableObject.primaryDialogue")]
    [Tooltip("Assign the SAME DialogueAsset that is in the InteractableObject's 'Primary Dialogue' field. " +
             "This tells the bridge which dialogue to listen for so it can queue the clarity choice after it.")]
    public DialogueAsset _primaryDialogueOverride;
}