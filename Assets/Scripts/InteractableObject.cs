using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string objectName = "Object";
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f;

    [Header("Dialogue")]
    public DialogueAsset objectDialogue;
    public bool hasDialogue = true;

    [Header("Item Collection")]
    public bool isCollectible = false;
    public string itemID;
    public GameObject itemVisual; //The object to hide after collection

    [Header("Puzzle")]
    public bool isPuzzle = false;
    public string puzzleID;
    public GameObject puzzleUI; //UI to show when interacting
    public bool isPuzzleOpen = false;   //check if puzzle screen is open

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
        //Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        //Hide prompt at start
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        //Check if already collected
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

        //Update prompt text
        if (promptText != null)
        {
            promptText.text = "Press " + interactKey.ToString() + " to interact";
        }

        //setup npc
        npcController = GetComponent<NPCController>();
    }

    void Update()
    {
        if (player == null || hasBeenCollected) return;

        //Check distance to player
        float distance = Vector3.Distance(GetComponent<Collider2D>().bounds.center, player.position);


        if (distance <= promptDistance)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }
            
            //Check for interaction input
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
        //npc set up for dialogue interaction
        if (npcController != null)
            npcController.StartInteraction();

        //Don't interact if dialogue is already active
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
        {
            return;
        }

        //Play interact sound
        if (interactSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(interactSound);
        }

        //Handle collection
        if (isCollectible)
        {
            CollectItem();
            return;
        }

        //Handle puzzle
        if (isPuzzle && puzzleUI != null && isPuzzleOpen == false)
        {
            OpenPuzzle();
            return;
        }

        if (isPuzzle && puzzleUI != null && isPuzzleOpen == true)
        {
            ClosePuzzle();
            return;
        }

        //Handle dialogue
        if (hasDialogue && objectDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(objectDialogue);

            // handle npc movement after ended dialogue ended
            DialogueSystem.Instance.OnDialogueEnded += HandleDialogueEnd;
        }
    }

    //for npc
    private void HandleDialogueEnd(DialogueAsset asset)
    {
        // Resume NPC movement
        if (npcController != null) 
        {
            npcController.EndInteraction();
        }
            
        // Prevent multiple events at once
        DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueEnd;
    }

    void CollectItem()
    {
        if (hasBeenCollected) return;

        hasBeenCollected = true;

        //Play collect sound
        if (collectSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(collectSound);
        }

        //Save to inventory
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.CollectItem(itemID);
        }

        //Show dialogue if exists
        if (hasDialogue && objectDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(objectDialogue);
        }

        //Hide visual
        if (itemVisual != null)
        {
            itemVisual.SetActive(false);
        }

        //Disable object
        HidePrompt();
        gameObject.SetActive(false);
    }

    void OpenPuzzle()
    {
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
            isPuzzleOpen = true;
            //Pause game while puzzle is open
            Time.timeScale = 0f;
        }
    }

    void ClosePuzzle()
    {
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false);
            isPuzzleOpen = false;
            //Resume game after puzzle is closed
            Time.timeScale = 1f;
        }
    }

    //Call this from puzzle UI when solved
    public void OnPuzzleSolved()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(puzzleID);
        }

        //Resume game
        Time.timeScale = 1f;

        //Optionally play dialogue
        if (hasDialogue && objectDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(objectDialogue);
        }
    }

    void OnDrawGizmosSelected()
    {
        //Visualize interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, promptDistance);
    }
}