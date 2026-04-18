using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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

    [Header("Progress Flags")]
    [SerializeField] private bool setFoundHiddenTombstoneOnInteract = false;

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
    [Tooltip("If false, this object never shows/hides interactPrompt — use when InteractionHintUI (or one shared canvas) is the only prompt. Otherwise HidePrompt() can SetActive(false) on a shared UI.")]
    [SerializeField] private bool useLocalInteractPrompt = true;

    public GameObject interactPrompt;
    public TextMeshProUGUI promptText;

    [Tooltip("Legacy field kept for serialized scenes. Prompt show/hide now uses Interaction Range only.")]
    [HideInInspector]
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

        if (useLocalInteractPrompt && interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }

        if (useLocalInteractPrompt && promptText != null)
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
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            objectCollider != null ? objectCollider.bounds.center : transform.position,
            player.position
        );

        bool withinInteractRange = distance <= interactionRange;

        if (isPuzzleOpen)
        {
            if (!IsTypingInUI() && Input.GetKeyDown(interactKey))
            {
                if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
                    return;
                ClosePuzzle();
            }

            return;
        }

        if (withinInteractRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }

            if (InputLock.Instance != null &&
                InputLock.Instance.GameplayInputEnabled &&
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
        if (!useLocalInteractPrompt || interactPrompt == null)
        {
            return;
        }

        interactPrompt.SetActive(true);
    }

    void HidePrompt()
    {
        if (!useLocalInteractPrompt || interactPrompt == null)
        {
            return;
        }

        interactPrompt.SetActive(false);
    }

    void Interact()
    {
        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return;

        Debug.Log("INTERACT() FIRED on " + gameObject.name);

        //ALWAYS PLAY AUDIO FIRST
        //            if (pickupSound != null)
        //AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        //
        if (interactSound != null)
        {
            Debug.Log("PLAYING SOUND on " + gameObject.name);
            SfxPlayback.PlayClipAtPoint(interactSound, transform.position);

        }
        else
        {
            Debug.LogWarning("No interactSound or UIAudioManager missing on " + gameObject.name);
        }

        //Delay other logic by 1 frame so audio isn't swallowed
        StartCoroutine(DelayedInteractionLogic());
    }

    private IEnumerator DelayedInteractionLogic()
    {
        // Wait 1 frame to guarantee audio plays first
        yield return null;

        // If dialogue is already active, stop here
        if (DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsDialogueActive())
        {
            yield break;
        }

        if (setFoundHiddenTombstoneOnInteract && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetFoundHiddenTombstone(true);
        }

        // NPC interaction
        if (npcController != null)
        {
            npcController.StartInteraction();
        }

        // Ghost interaction
        GhostHintNPC ghost = GetComponent<GhostHintNPC>();
        if (ghost != null)
        {
            // Tiny delay so ghost dialogue doesn't swallow audio
            yield return new WaitForSecondsRealtime(.1f);
            ghost.Interact();
            yield break;
        }

        // Puzzle interaction
        if (isPuzzle && puzzleUI != null && !isPuzzleOpen)
        {
            if (SaveSystem.Instance != null &&
                !string.IsNullOrEmpty(puzzleID) &&
                SaveSystem.Instance.IsPuzzleSolved(puzzleID))
            {
                yield break;
            }

            OpenPuzzle();
            yield break;
        }

        // Dialogue interaction
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
        playerInRange = false;
        HidePrompt();
        SetGameplayInputEnabled(false);
        Time.timeScale = 0f;
    }


    public void ClosePuzzle()
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
    }

}
