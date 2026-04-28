using UnityEngine;

public class PickupPreviewCanvas : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    private DialogueAsset queuedDialogueAfterClose;
    private bool popupOpen;
    private CharMovement movementScript;
    private Rigidbody2D playerRigidbody;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            movementScript = player.GetComponent<CharMovement>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        Debug.Log("PickupPreview Update running");
        if (!popupOpen)
            return;

        if (Input.GetKeyDown(closeKey))
        {
            Debug.Log("E detected - CLOSING");
            ClosePreview();
        }
    }

    public void Show(DialogueAsset dialogueAfterClose = null)
    {
        queuedDialogueAfterClose = dialogueAfterClose;
        popupOpen = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = Vector2.zero;

        if (movementScript != null)
            movementScript.enabled = false;

        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = false;
            InputLock.Instance.GameplayInputEnabled = false;
        }
    }

    public void ClosePreview()
    {
        if (!popupOpen)
            return;

        QueueDialogueAfterCloseIfNeeded();

        popupOpen = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        if (movementScript != null)
            movementScript.enabled = true;

        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = true;
            InputLock.Instance.GameplayInputEnabled = true;
        }

        gameObject.SetActive(false);
    }

    public bool IsPreviewOpen()
    {
        return popupOpen;
    }

    private void OnDisable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        if (movementScript != null)
            movementScript.enabled = true;

        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = true;
            InputLock.Instance.GameplayInputEnabled = true;
        }
    }

    private void QueueDialogueAfterCloseIfNeeded()
    {
        if (queuedDialogueAfterClose != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(queuedDialogueAfterClose);
        }

        queuedDialogueAfterClose = null;
    }
}
