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
        panel.SetActive(false);
    }

    public void Show(ItemData item)
    {
        IsOpen = true;

        panel.SetActive(true);

        if (inventoryUI != null)
            inventoryUI.SetVisible(false);

        nameText.text = item.itemName;
        descriptionText.text = item.description;
        icon.sprite = item.icon;
        dimBG.SetActive(true);

        TooltipUI.Instance.Hide();          // Hide tooltip after opening the inspect screen
        InputLock.Instance.CanToggleInventory = false;
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        dimBG.SetActive(false);
        if (inventoryUI != null)
            inventoryUI.SetVisible(true);
        InputLock.Instance.CanToggleInventory = true;
    }
}
