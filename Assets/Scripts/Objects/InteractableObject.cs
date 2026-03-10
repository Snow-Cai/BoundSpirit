using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string objectName = "Object";
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f;

    [Header("Dialogue")]
    public DialogueAsset objectDialogue;
    public bool hasDialogue = true;

    [SerializeField] private bool playPrimaryOnlyOnce = false;
    [SerializeField] private DialogueAsset primaryDialogue;
    [SerializeField] private DialogueAsset repeatDialogue;

    [Header("Puzzle")]
    public bool isPuzzle = false;
    public string puzzleID;
    public GameObject puzzleUI;
    public bool isPuzzleOpen = false;

    [Header("Puzzle Components")]
    public LoginPuzzle loginPuzzle;

    [Header("Informational Tidbit")]
    [TextArea]
    public string tidbitMessage;
    public bool showTidbitOnSolve = false;

    [Header("UI Prompt")]
    public GameObject interactPrompt;
    public TextMeshProUGUI promptText;
    public float promptDistance = 3f;

    [Header("Audio")]
    public AudioClip interactSound;

    [Header("NPC")]
    private NPCController npcController;

    private Transform player;
    private Collider2D objectCollider;
    private bool playerInRange = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.text = "Press " + interactKey.ToString() + " to interact";
        }

        npcController = GetComponent<NPCController>();
        objectCollider = GetComponent<Collider2D>();

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null) return;

        // If computer screen is open, only allow closing it (unless typing)
        if (isPuzzleOpen)
        {
            if (!IsTypingInUI() && Input.GetKeyDown(interactKey))
            {
                ClosePuzzle();
            }
            return;
        }

        float distance = Vector3.Distance(
            objectCollider != null ? objectCollider.bounds.center : transform.position,
            player.position
        );

        if (distance <= promptDistance)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }

            if (InputLock.Instance != null &&
                InputLock.Instance.GameplayInputEnabled &&
                distance <= interactionRange &&
                Input.GetKeyDown(interactKey))
            {
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
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
        }
    }

    void HidePrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    void Interact()
    {
        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return;
        Debug.Log("INTERACT() FIRED on " + gameObject.name);

        if (DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsDialogueActive())
        {
            return;
        }

        if (npcController != null)
        {
            npcController.StartInteraction();
        }
        GhostHintNPC ghostHintNpc = GetComponent<GhostHintNPC>();
        if (ghostHintNpc != null)
        {
            ghostHintNpc.Interact();
            return;
        }

        if (interactSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(interactSound);
        }

        if (isPuzzle && puzzleUI != null && !isPuzzleOpen)
        {
            OpenPuzzle();
            return;
        }

        if (hasDialogue)
        {
            PlayDialogue();
        }
    }

    void OpenPuzzle()
    {
        if (loginPuzzle != null)
        {
            loginPuzzle.ResetFields();
        }

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
        }

        isPuzzleOpen = true;
        SetGameplayInputEnabled(false);
        Time.timeScale = 0f;
    }


    void ClosePuzzle()
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
        SetGameplayInputEnabled(true);
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
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(puzzleID);
        }

        Time.timeScale = 1f;

        if (hasDialogue)
        {
            PlayDialogue();
        }

        if (showTidbitOnSolve && !string.IsNullOrEmpty(tidbitMessage))
        {
            UICluePopup popup = Object.FindFirstObjectByType<UICluePopup>();
            if (popup != null)
            {
                popup.ShowClue(tidbitMessage);
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

        // If this is the afterlife gate AND player has reached an ending
        if (gameObject.CompareTag("AfterlifeGate") && StoryFlags.HasEnoughForEnding())
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

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, promptDistance);
    }
}
