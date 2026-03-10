using UnityEngine;

public sealed class BenchCaesarDecodeInteractable : MonoBehaviour
{
    [Header("Requirements")]
    [SerializeField] private ItemData requiredNoteItem;
    [SerializeField] private ItemData requiredWheelItem;

    [Header("Puzzle")]
    [SerializeField] private CaesarNotePuzzleData puzzleData;

    [Header("References (Optional)")]
    [SerializeField] private CaesarDecodePanel decodePanel;
    [SerializeField] private SaveSystem saveSystem;            // optional: auto-found
    [SerializeField] private PlayerInventory playerInventory;  // optional: auto-found

    [Header("Auto Find")]
    [SerializeField] private bool autoFindSaveSystem = true;
    [SerializeField] private bool autoFindPlayerInventory = true;
    [SerializeField] private string playerTag = "Player"; // only used if autoFindPlayerInventory is true

    [Header("Feedback (Optional)")]
    [SerializeField] private string missingNoteMessage = "You need the encoded note.";
    [SerializeField] private string missingWheelMessage = "You need the Caesar Cipher Wheel.";

    private void Awake()
    {
        ResolveReferences();
        if (saveSystem == null)
            saveSystem = FindFirstObjectByType<SaveSystem>();
    }

    private void OnEnable()
    {
        // In case this object is enabled later or scenes change.
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (autoFindSaveSystem && saveSystem == null)
        {
            // Unity 2023+/6: FindFirstObjectByType is the modern alternative to FindObjectOfType
            saveSystem = Object.FindFirstObjectByType<SaveSystem>();
            if (saveSystem == null)
                Debug.LogWarning("[BenchCaesarDecodeInteractable] SaveSystem not found. (Is Main scene loaded first?)");
        }

        if (autoFindPlayerInventory && playerInventory == null)
        {
            // Prefer tag lookup if you have a consistent Player tag
            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                var player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                    playerInventory = player.GetComponent<PlayerInventory>();
            }

            // Fallback: find any inventory in scene
            if (playerInventory == null)
                playerInventory = Object.FindFirstObjectByType<PlayerInventory>();

            if (playerInventory == null)
                Debug.LogWarning("[BenchCaesarDecodeInteractable] PlayerInventory not found.");
        }
    }

    // Call this from your interact system
    public void Interact()
    {
        if (decodePanel == null || puzzleData == null)
        {
            Debug.LogError("[BenchCaesarDecodeInteractable] Missing DecodePanel or PuzzleData reference.");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("[BenchCaesarDecodeInteractable] PlayerInventory missing.");
            return;
        }

        if (saveSystem == null)
        {
            Debug.LogError("[BenchCaesarDecodeInteractable] SaveSystem missing.");
            return;
        }

        if (requiredNoteItem != null && !playerInventory.HasItem(requiredNoteItem))
        {
            Debug.Log($"[Bench Caesar] {missingNoteMessage}");
            return;
        }

        if (requiredWheelItem != null && !playerInventory.HasItem(requiredWheelItem))
        {
            Debug.Log($"[Bench Caesar] {missingWheelMessage}");
            return;
        }

    }
}
