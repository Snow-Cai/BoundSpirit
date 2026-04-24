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

        IsOpen = true;

        if (panel != null)
            panel.SetActive(true);

        if (inventoryUI != null)
            inventoryUI.SetVisible(false);

        nameText.text = item.itemName;
        descriptionText.text = item.description;
        icon.sprite = item.icon;
        if (dimBG != null)
            dimBG.SetActive(true);

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();          // Hide tooltip after opening the inspect screen
        if (InputLock.Instance != null)
            InputLock.Instance.CanToggleInventory = false;
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
        if (dimBG != null)
            dimBG.SetActive(false);
        if (inventoryUI != null)
            inventoryUI.SetVisible(true);
        if (InputLock.Instance != null)
            InputLock.Instance.CanToggleInventory = true;
    }

    private void OnDisable()
    {
        IsOpen = false;

        if (panel != null)
            panel.SetActive(false);
        if (dimBG != null)
            dimBG.SetActive(false);
    }
}
