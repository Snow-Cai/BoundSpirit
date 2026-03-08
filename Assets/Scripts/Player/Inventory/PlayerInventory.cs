using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
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
        return inventory.Contains(itemID);
    }

    public void RemoveItem(ItemData itemID)
    {
        if(inventory.Contains(itemID))
            inventory.Remove(itemID);
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
            ids.Add(item.itemID);
        return ids;
    }

    public void LoadInventoryFromIDs(List<string> ids)
    {
        inventory.Clear();
        foreach (string id in ids)
        {
            ItemData item = ItemDatabase.Instance.GetItemByID(id);
            if(item != null)
                inventory.Add(item);
        }
    }

    public List<ItemData> GetItems()
    {
        return new List<ItemData>(inventory);
    }
}
