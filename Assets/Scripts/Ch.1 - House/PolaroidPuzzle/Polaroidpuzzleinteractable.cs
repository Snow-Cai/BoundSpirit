using UnityEngine;

public class PolaroidPuzzleInteractable : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2f;
    public float promptRange = 3f;

    [Header("References")]
    public PolaroidTimelinePuzzle puzzleManager;
    public GameObject interactPrompt;

    [Header("Dialogue - First Approach")]
    public DialogueAsset firstApproachDialogue;

    [Header("Dialogue - After Solve")]
    public DialogueAsset solvedDialogue;
    public DialogueAsset solvedRepeatDialogue;
    public bool playSolvedDialogueOnlyOnce = true;

    private Transform player;
    private bool playerInRange = false;
    private bool firstApproachDone = false;
    private float inputCooldown = 0f;
    private const string FIRST_APPROACH_KEY = "Chapter1_polaroid_firstApproach";
    private const string SOLVED_DIALOGUE_KEY = "Chapter1_polaroid_solvedDialogue";

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (SaveSystem.Instance != null)
            firstApproachDone = SaveSystem.Instance.HasViewedDialogue(FIRST_APPROACH_KEY);

    }

    private void Update()
    {
        if (player == null) return;

        inputCooldown -= Time.deltaTime;

        float dist = Vector2.Distance(player.position, transform.position);

        bool puzzleOpen = puzzleManager != null &&
                          puzzleManager.puzzlePanel != null &&
                          puzzleManager.puzzlePanel.activeSelf;

        //always check for close first
        if (puzzleOpen)
        {
            if (Input.GetKeyDown(interactKey) && inputCooldown <= 0f)
            {
                inputCooldown = 0.3f;
                puzzleManager.ClosePuzzle();
            }
            return;
        }

        //only block opening if input is locked
        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled) return;

        if (dist <= promptRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                if (interactPrompt != null)
                    interactPrompt.SetActive(true);
            }

            if (dist <= interactRange && Input.GetKeyDown(interactKey) && inputCooldown <= 0f)
            {
                inputCooldown = 0.3f;
                HandleInteract();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (interactPrompt != null)
                    interactPrompt.SetActive(false);
            }
        }
    }

    private void HandleInteract()
    {
        if (puzzleManager != null && puzzleManager.IsSolved)
        {
            TryHandleSolvedInteraction();
            return;
        }

        if (!firstApproachDone && firstApproachDialogue != null && DialogueSystem.Instance != null)
        {
            firstApproachDone = true;
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.MarkDialogueViewed(FIRST_APPROACH_KEY);

            DialogueSystem.Instance.StartDialogue(firstApproachDialogue);
            DialogueSystem.Instance.OnDialogueEnded += OnFirstDialogueEnded;
            return;
        }

        puzzleManager?.OpenPuzzle();
    }

    private void TryHandleSolvedInteraction()
    {
        if (DialogueSystem.Instance == null)
        {
            puzzleManager?.OpenPuzzle();
            return;
        }

        DialogueAsset dialogueToPlay = null;
        bool solvedDialogueAlreadyViewed =
            SaveSystem.Instance != null &&
            SaveSystem.Instance.HasViewedDialogue(SOLVED_DIALOGUE_KEY);

        if (!solvedDialogueAlreadyViewed || !playSolvedDialogueOnlyOnce)
        {
            dialogueToPlay = solvedDialogue;
        }

        if (dialogueToPlay == null)
        {
            dialogueToPlay = solvedRepeatDialogue;
        }

        if (dialogueToPlay == null)
        {
            puzzleManager?.OpenPuzzle();
            return;
        }

        puzzleManager?.OpenPuzzle();

        if (!solvedDialogueAlreadyViewed && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.MarkDialogueViewed(SOLVED_DIALOGUE_KEY);
        }

        DialogueSystem.Instance.StartDialogue(dialogueToPlay);
    }

    private void OnFirstDialogueEnded(DialogueAsset asset)
    {
        DialogueSystem.Instance.OnDialogueEnded -= OnFirstDialogueEnded;
        puzzleManager?.OpenPuzzle();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, promptRange);
    }
}
