using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class GhostHintNPC : MonoBehaviour
{
    [Header("Required Item")]
    [SerializeField] private ItemData requiredItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset needItemDialogue;
    [SerializeField] private DialogueAsset repeatNeedDialogue;
    [SerializeField] private DialogueAsset wrongItemDialogue;
    [SerializeField] private DialogueAsset successDialogue;
    [SerializeField] private DialogueAsset alreadyHelpedDialogue;
    [SerializeField] private GhostOfferUI offerUI;

    [Header("Save Progress")]
    [SerializeField] private string ghostPuzzleID = "ghost1_helped";

    private PlayerInventory playerInventory;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerInventory = playerObject.GetComponent<PlayerInventory>();
        }
    }

    public void Interact()
    {
        Debug.Log("GhostHintNPC.Interact() called");

        if (IsSolved())
        {
            PlayDialogue(alreadyHelpedDialogue);
            return;
        }

        bool hasSeenNeedDialogueBefore = HasSeenNeedDialogueBefore();

        if (!hasSeenNeedDialogueBefore)
        {
            if (needItemDialogue != null && DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.OnDialogueEnded -= HandleNeedDialogueEnded;
                DialogueSystem.Instance.OnDialogueEnded += HandleNeedDialogueEnded;
                DialogueSystem.Instance.StartDialogue(needItemDialogue);
            }

            return;
        }

        if (repeatNeedDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded -= HandleRepeatNeedDialogueEnded;
            DialogueSystem.Instance.OnDialogueEnded += HandleRepeatNeedDialogueEnded;
            DialogueSystem.Instance.StartDialogue(repeatNeedDialogue);
            return;
        }

        if (offerUI != null)
        {
            offerUI.Open(this);
        }
    }

    private void HandleNeedDialogueEnded(DialogueAsset finishedDialogue)
    {
        if (finishedDialogue != needItemDialogue)
        {
            return;
        }

        DialogueSystem.Instance.OnDialogueEnded -= HandleNeedDialogueEnded;

        if (offerUI != null && !IsSolved())
        {
            offerUI.Open(this);
        }
    }

    private void HandleRepeatNeedDialogueEnded(DialogueAsset finishedDialogue)
    {
        if (finishedDialogue != repeatNeedDialogue)
        {
            return;
        }

        DialogueSystem.Instance.OnDialogueEnded -= HandleRepeatNeedDialogueEnded;

        if (offerUI != null && !IsSolved())
        {
            offerUI.Open(this);
        }
    }

    public void OfferItem(ItemData offeredItem)
    {
        if (playerInventory == null || offeredItem == null)
        {
            return;
        }

        if (IsSolved())
        {
            PlayDialogue(alreadyHelpedDialogue);
            return;
        }

        if (requiredItem == null)
        {
            Debug.LogWarning("GhostHintNPC: requiredItem is not assigned.");
            return;
        }

        if (offeredItem != requiredItem)
        {
            PlayDialogue(wrongItemDialogue);
            return;
        }

        if (!playerInventory.HasItem(requiredItem))
        {
            PlayDialogue(needItemDialogue);
            return;
        }

        playerInventory.RemoveItem(requiredItem);

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(ghostPuzzleID);
        }

        PlayDialogue(successDialogue);
    }

    private bool IsSolved()
    {
        return SaveSystem.Instance != null &&
               SaveSystem.Instance.IsPuzzleSolved(ghostPuzzleID);
    }

    private void PlayDialogue(DialogueAsset dialogue)
    {
        if (dialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogue);
        }
    }

    private bool HasSeenNeedDialogueBefore()
    {
        return needItemDialogue != null &&
               SaveSystem.Instance != null &&
               !string.IsNullOrEmpty(needItemDialogue.dialogueID) &&
               SaveSystem.Instance.HasViewedDialogue(needItemDialogue.dialogueID);
    }
}