using UnityEngine;

public class GraveyardGateController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 2f;

    [Header("Requirements")]
    [SerializeField] private DialogueAsset requiredIntroDialogue; // Player must view this dialogue before leaving to next area
    [SerializeField] private string gatePuzzleID = "chapter0_graveyard_gate";

    [Header("Dialogue Feedback")]
    [SerializeField] private DialogueAsset lockedWithoutNameDialogue; // Shown if player has not learned their name
    [SerializeField] private DialogueAsset lockedPuzzleDialogue; // Wrong puzzle attempt dialogue

    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleUI;

    [Header("Gate Visuals")]
    [Tooltip("Visual object to disable when the gate is unlocked")]
    [SerializeField] private GameObject gateVisuals;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }

        // If the puzzle was already solved in a previous session, start with the gate removed.
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

        // E-interaction near the gate.
        if (distance <= interactionRange && Input.GetKeyDown(interactKey))
        {
            HandleGateInteraction();
        }
    }

    private void HandleGateInteraction()
    {
        // If the gate is already unlocked, do nothing (another system handles exit).
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

        // 2. Player knows name but puzzle not solved → toggle puzzle UI.
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

        GameInputState.DialogueActive = true; // freeze movement while puzzle is open
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
        // Hide the visual gate and its collider.
        if (gateVisuals != null)
        {
            gateVisuals.SetActive(false);
        }

        // No further interaction once the gate is gone.
        enabled = false;
    }
}
