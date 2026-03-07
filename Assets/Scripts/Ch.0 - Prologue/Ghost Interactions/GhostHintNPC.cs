using UnityEngine;

public class GhostHintNPC : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 2f;

    [Header("Required Item")]
    [SerializeField] private ItemData requiredItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset needItemDialogue;
    [SerializeField] private DialogueAsset successDialogue;
    [SerializeField] private DialogueAsset alreadyHelpedDialogue;

    [Header("Save Progress")]
    [SerializeField] private string ghostPuzzleID = "graveyard_ghost_rose";

    private Transform player;
    private PlayerInventory playerInventory;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerInventory = playerObject.GetComponent<PlayerInventory>();
        }
    }

    private void Update()
    {
        if (player == null || playerInventory == null)
        {
            return;
        }

        if (GameInputState.DialogueActive)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > interactionRange)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }
    }

    private void HandleInteraction()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsPuzzleSolved(ghostPuzzleID))
        {
            PlayDialogue(alreadyHelpedDialogue);
            return;
        }

        if (requiredItem == null)
        {
            Debug.LogWarning("GhostHintNPC: Required item is not assigned.");
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

        Debug.Log($"Ghost accepted item: {requiredItem.itemID}");
    }

    private void PlayDialogue(DialogueAsset dialogue)
    {
        if (dialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogue);
        }
    }
}