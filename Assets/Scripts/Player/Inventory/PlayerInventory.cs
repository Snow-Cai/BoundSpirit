using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    const string EmptySlotMarker = "__EMPTY_SLOT__";
    public List<ItemData> inventory = new List<ItemData>();

    public void PickUpItem(ItemData itemID)
    {
        if(!inventory.Contains(itemID))
        {
            inventory.Add(itemID);
            Debug.Log("Picked up: " + itemID.itemID);
        }
    }

    public bool HasItem(ItemData itemID)
    {
        return FindItemIndex(itemID) >= 0;
    }

    public void RemoveItem(ItemData itemID)
    {
        int itemIndex = FindItemIndex(itemID);
        if(itemIndex >= 0)
            inventory.RemoveAt(itemIndex);
    }

    public ItemData GetInventoryItem(int index)
    {
        if (index >= 0 && index < inventory.Count)
            return inventory[index];
        else
            return null;
    }

    public List<string> GetInventoryItemIDs()
    {
        List<string> ids = new List<string>();
        foreach (var item in inventory)
        {
            ids.Add(item != null ? item.itemID : EmptySlotMarker);
        }
        return ids;
    }

    public void LoadInventoryFromIDs(List<string> ids)
    {
        inventory.Clear();
        if (ids == null)
            return;

        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || id == EmptySlotMarker)
            {
                inventory.Add(null);
                continue;
            }

            ItemData item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItemByID(id) : null;
            if(item != null)
            {
                inventory.Add(item);
            }
            else
            {
                Debug.LogWarning("PlayerInventory: Saved item ID not found in ItemDatabase: " + id);
            }
        }
    }

    public List<ItemData> GetItems()
    {
        return new List<ItemData>(inventory);
    }

    private int FindItemIndex(ItemData item)
    {
        if (item == null)
        {
            return -1;
        }

        for (int i = 0; i < inventory.Count; i++)
        {
            ItemData inventoryItem = inventory[i];
            if (inventoryItem == null)
            {
                continue;
            }

            if (inventoryItem == item)
            {
                return i;
            }

            if (!string.IsNullOrWhiteSpace(inventoryItem.itemID) &&
                inventoryItem.itemID == item.itemID)
            {
                return i;
            }
        }

        return -1;
    }
}
