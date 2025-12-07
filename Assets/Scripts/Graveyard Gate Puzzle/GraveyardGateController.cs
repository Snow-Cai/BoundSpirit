using UnityEngine;

public class GraveyardGateController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 2f;

    [Header("Requirements")]
    [SerializeField] private DialogueAsset requiredIntroDialogue; // Player must view this before leaving to next area
    [SerializeField] private string gatePuzzleID = "chapter0_graveyard_gate";

    [Header("Dialogue Feedback")]
    [SerializeField] private DialogueAsset lockedWithoutNameDialogue; // Shown if player has not learned their name
    [SerializeField] private DialogueAsset lockedPuzzleDialogue;      // Hint before the player uses the puzzle

    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleUI;

    [Header("Gate Visuals")]
    [Tooltip("Visual object to disable when the gate is unlocked")]
    [SerializeField] private GameObject gateVisuals;

    public KeyCode InteractKey => interactKey;
    private Transform player;

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
            return;
        }

        // 2. Player knows name but puzzle not solved.
        // First interaction after that: show a one-time hint before opening the puzzle.
        if (!HasSeenPuzzleHint() &&
            lockedPuzzleDialogue != null &&
            DialogueSystem.Instance != null)
        {
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
        if (requiredIntroDialogue == null ||
            SaveSystem.Instance == null ||
            string.IsNullOrEmpty(requiredIntroDialogue.dialogueID))
        {
            return false;
        }

        return SaveSystem.Instance.HasViewedDialogue(requiredIntroDialogue.dialogueID);
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
