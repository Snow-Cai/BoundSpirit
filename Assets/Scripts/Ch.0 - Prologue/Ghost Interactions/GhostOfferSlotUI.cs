using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GhostOfferSlotUI : MonoBehaviour
{
    [SerializeField] private SlotUI slotUI;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI numberLabel;
    [SerializeField] private TextMeshProUGUI itemNameLabel;

    private ItemData currentItem;
    private GhostOfferUI parentUI;

    public void Setup(ItemData item, int displayIndex, GhostOfferUI ui)
    {
        currentItem = item;
        parentUI = ui;

        if (slotUI != null)
        {
            slotUI.SetItem(item);
        }

        if (numberLabel != null)
        {
            numberLabel.text = (displayIndex + 1).ToString();
        }

        if (itemNameLabel != null)
        {
            itemNameLabel.text = item != null ? item.itemName : string.Empty;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OfferThisItem);
        }

        gameObject.SetActive(item != null);
    }

    public void OfferThisItem()
    {
        if (parentUI != null && currentItem != null)
        {
            parentUI.OfferItem(currentItem);
        }
    }
}