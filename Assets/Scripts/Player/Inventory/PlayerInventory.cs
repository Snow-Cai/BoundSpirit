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

    public List<ItemData> GetItems()        //use with UI to be able to display list of items
    {
        return inventory;       
    }
}
