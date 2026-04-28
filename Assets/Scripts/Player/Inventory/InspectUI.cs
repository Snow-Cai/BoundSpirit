using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InspectUI : MonoBehaviour
{
    public static InspectUI Instance;

    public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image icon;
    public GameObject dimBG;
    public InventoryUI inventoryUI;

    public bool IsOpen {  get; private set; }
    private bool restoreInventoryOnClose;
    private bool lockGameplayOnClose;
    private DialogueAsset queuedDialogueAfterClose;

    private void Awake()
    {
        Instance = this;
        if (panel != null)
            panel.SetActive(false);
        if (dimBG != null)
            dimBG.SetActive(false);
    }

    private void Start()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();
    }

    public void Show(ItemData item)
    {
        if (item == null)
            return;

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        ShowInternal(item.itemName, item.description, item.icon, true, false, null);
    }

    public void ShowPreview(string title, string description, Sprite previewSprite, DialogueAsset dialogueAfterClose = null)
    {
        ShowInternal(title, description, previewSprite, false, true, dialogueAfterClose);
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
        if (dimBG != null)
            dimBG.SetActive(false);
        if (restoreInventoryOnClose && inventoryUI != null)
            inventoryUI.SetVisible(true);

        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = true;
            if (lockGameplayOnClose)
                InputLock.Instance.GameplayInputEnabled = true;
        }

        if (queuedDialogueAfterClose != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(queuedDialogueAfterClose);

        restoreInventoryOnClose = false;
        lockGameplayOnClose = false;
        queuedDialogueAfterClose = null;
    }

    private void OnDisable()
    {
        bool shouldQueueDialogue = IsOpen;
        IsOpen = false;

        if (panel != null)
            panel.SetActive(false);
        if (dimBG != null)
            dimBG.SetActive(false);

        if (InputLock.Instance != null && lockGameplayOnClose)
            InputLock.Instance.GameplayInputEnabled = true;

        if (shouldQueueDialogue && queuedDialogueAfterClose != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(queuedDialogueAfterClose);

        restoreInventoryOnClose = false;
        lockGameplayOnClose = false;
        queuedDialogueAfterClose = null;
    }

    private void ShowInternal(
        string title,
        string description,
        Sprite previewSprite,
        bool hideInventory,
        bool lockGameplay,
        DialogueAsset dialogueAfterClose)
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        IsOpen = true;
        restoreInventoryOnClose = hideInventory;
        lockGameplayOnClose = lockGameplay;
        queuedDialogueAfterClose = dialogueAfterClose;

        if (panel != null)
            panel.SetActive(true);

        if (hideInventory && inventoryUI != null)
            inventoryUI.SetVisible(false);

        if (nameText != null)
            nameText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (icon != null)
            icon.sprite = previewSprite;

        if (dimBG != null)
            dimBG.SetActive(true);

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();

        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = false;
            if (lockGameplay)
                InputLock.Instance.GameplayInputEnabled = false;
        }
    }
}
