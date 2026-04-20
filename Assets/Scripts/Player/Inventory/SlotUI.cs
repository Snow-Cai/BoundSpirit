using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public ItemData item;
    public Image icon;
    public int slotIndex;
    private GameObject dragIcon;

    public void SetItem(ItemData newItem)
    {
        item = newItem;
        UpdateSlot();
    }

    public ItemData GetItem() => item;
    public void UpdateSlot()
    {
        if(item != null)
        {
            icon.enabled = true;
            icon.sprite = item.icon;
        }
        else
        {
            icon.enabled = false;
            icon.sprite = null;
        }
    }
    
    //following functions handle dragging and swapping items around in player inventory slots
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;
        icon.enabled = false;                       //hide original icon
        dragIcon = new GameObject("DragIcon");      //create a temporary icon for dragging visual
        dragIcon.transform.SetParent(transform.root);
        dragIcon.transform.SetAsLastSibling();

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite;
        img.raycastTarget = false;
        RectTransform rt = dragIcon.GetComponent<RectTransform>();                                  //rt to maintain size and position during dragging
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(48, 48);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        Canvas canvas = dragIcon.GetComponentInParent<Canvas>();
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out pos);        //convert eventData.position properly to ensure dragIcon shows up correctly
        rt.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(dragIcon != null)
            Destroy(dragIcon);
        UpdateSlot();
    }

    public void OnDrop(PointerEventData eventData)
    {
        SlotUI other = eventData.pointerDrag?.GetComponentInParent<SlotUI>();
        if (other == null || other == this) return;
        if (System.Object.ReferenceEquals(other.item, null)) return;        //prevents swapping null slots onto an existing item, only allowing dragging on an existing item
        //if item already in slot, swap the items
        ItemData temp = item;
        item = other.item;
        other.item = temp;

        UpdateSlot();
        other.UpdateSlot();

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if(inventory != null)
        {
            while (inventory.inventory.Count <= slotIndex)      //ensures the ability to place an item in any inventory slot even if at a slotIndex greater than the heldItems count
                inventory.inventory.Add(null);
            while (inventory.inventory.Count <= other.slotIndex)
                inventory.inventory.Add(null);
            inventory.inventory[slotIndex] = item;
            inventory.inventory[other.slotIndex] = other.item;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InspectUI.Instance != null && InspectUI.Instance.IsOpen) return;
        Debug.Log("Hover triggered");
        Debug.Log("Tooltip instance: " + TooltipUI.Instance);
        if (item != null)
            TooltipUI.Instance.Show(item.itemName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InspectUI.Instance != null && InspectUI.Instance.IsOpen) return;
        if (!InputLock.Instance.AllowInspect) return;
        if (dragIcon != null) return;
        Debug.Log("Slot clicked!");
        if (item != null && item.canInspect)
            Debug.Log("Opening inspect for: " + item.itemName);
            InspectUI.Instance.Show(item);
    }
}
