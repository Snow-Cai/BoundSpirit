using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class GhostHintNPC : MonoBehaviour
{
    [Header("Progress Gate")]
    [SerializeField] private bool requireGateClueFirst = true;
    [SerializeField] private string requiredDialogueID = "Chapter0_gateCluePrimary";
    [Tooltip("Played when requireGateClueFirst is on and the player has not viewed the gate stone clue yet.")]
    [SerializeField] private DialogueAsset blockedBeforeGateClueDialogue;
    [SerializeField] private string blockedObjectiveMessage = "I want to get out of here first...";

    [Header("Required Item")]
    [SerializeField] private ItemData requiredItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset needItemDialogue;
    [SerializeField] private DialogueAsset repeatNeedDialogue;
    [SerializeField] private DialogueAsset wrongItemDialogue;
    [SerializeField] private DialogueAsset successDialogue;
    [SerializeField] private DialogueAsset alreadyHelpedDialogue;
    [SerializeField] private GhostOfferUI offerUI;
    [SerializeField] private DialogueAsset noItemsDialogue;

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

        if (!CanStartGhostInteraction())
        {
            return;
        }

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

        if (!IsSolved())
        {
            if (!PlayerHasAnyItems())
            {
                QueueDialogue(noItemsDialogue);
                return;
            }

            if (offerUI != null)
            {
                offerUI.Open(this);
            }
        }
    }

    private void HandleNeedDialogueEnded(DialogueAsset finishedDialogue)
    {
        if (finishedDialogue != needItemDialogue)
        {
            return;
        }

        DialogueSystem.Instance.OnDialogueEnded -= HandleNeedDialogueEnded;

        if (!IsSolved())
        {
            if (!PlayerHasAnyItems())
            {
                QueueDialogue(noItemsDialogue);
                return;
            }

            if (offerUI != null)
            {
                offerUI.Open(this);
            }
        }
    }

    private void HandleRepeatNeedDialogueEnded(DialogueAsset finishedDialogue)
    {
        if (finishedDialogue != repeatNeedDialogue)
        {
            return;
        }

        DialogueSystem.Instance.OnDialogueEnded -= HandleRepeatNeedDialogueEnded;

        if (!IsSolved())
        {
            if (!PlayerHasAnyItems())
            {
                QueueDialogue(noItemsDialogue);
                return;
            }

            if (offerUI != null)
            {
                offerUI.Open(this);
            }
        }
    }

    private bool PlayerHasAnyItems()
    {
        return playerInventory != null &&
               playerInventory.GetItems() != null &&
               playerInventory.GetItems().Count > 0;
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

    private void QueueDialogue(DialogueAsset dialogue)
    {
        if (dialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(dialogue);
        }
    }

    private bool HasSeenNeedDialogueBefore()
    {
        return needItemDialogue != null &&
               SaveSystem.Instance != null &&
               !string.IsNullOrEmpty(needItemDialogue.dialogueID) &&
               SaveSystem.Instance.HasViewedDialogue(needItemDialogue.dialogueID);
    }

    private bool CanStartGhostInteraction()
    {
        if (!requireGateClueFirst)
        {
            return true;
        }

        if (SaveSystem.Instance == null || string.IsNullOrEmpty(requiredDialogueID))
        {
            return true;
        }

        if (SaveSystem.Instance.HasViewedDialogue(requiredDialogueID))
        {
            return true;
        }

        if (blockedBeforeGateClueDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(blockedBeforeGateClueDialogue);
        }
        else
        {
            ObjectiveBanner.Instance?.ShowMessage(blockedObjectiveMessage);
        }

        return false;
    }
}