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

    [Header("Item Collection")]
    public bool isCollectible = false;
    public string itemID;
    public GameObject itemVisual;

    [Header("Puzzle / Computer Screen")]
    public bool isPuzzle = false;
    public string puzzleID;
    public GameObject puzzleUI;
    public bool isPuzzleOpen = false;

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
    public AudioClip collectSound;

    [Header("NPC")]
    private NPCController npcController;

    private Transform player;
    private bool playerInRange = false;
    private bool hasBeenCollected = false;

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

        if (isCollectible && SaveSystem.Instance != null)
        {
            if (SaveSystem.Instance.HasItem(itemID))
            {
                hasBeenCollected = true;
                if (itemVisual != null)
                {
                    itemVisual.SetActive(false);
                }
                gameObject.SetActive(false);
            }
        }

        if (promptText != null)
        {
            promptText.text = "Press " + interactKey.ToString() + " to interact";
        }

        npcController = GetComponent<NPCController>();

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null || hasBeenCollected) return;

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
            GetComponent<Collider2D>().bounds.center,
            player.position
        );

        if (distance <= promptDistance)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }

            if (distance <= interactionRange && Input.GetKeyDown(interactKey))
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
        Debug.Log("INTERACT() FIRED on " + gameObject.name);

        if (npcController != null)
        {
            npcController.StartInteraction();
        }

        if (DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsDialogueActive())
        {
            return;
        }

        if (interactSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(interactSound);
        }

        if (isCollectible)
        {
            CollectItem();
            return;
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

    void CollectItem()
    {
        if (hasBeenCollected) return;

        hasBeenCollected = true;

        if (collectSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(collectSound);
        }

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.CollectItem(itemID);
        }

        if (hasDialogue)
        {
            PlayDialogue();
        }

        if (itemVisual != null)
        {
            itemVisual.SetActive(false);
        }

        HidePrompt();
        gameObject.SetActive(false);
    }

    void OpenPuzzle()
    {
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
            isPuzzleOpen = true;
            InputLock.Instance.GameplayInputEnabled = false;
            Time.timeScale = 0f;
        }
    }

    void ClosePuzzle()
    {
        if (puzzleUI != null)
        {
              
            puzzleUI.SetActive(false);
            isPuzzleOpen = false;
            InputLock.Instance.GameplayInputEnabled = true;
            Time.timeScale = 1f;
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
