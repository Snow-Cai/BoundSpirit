using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> items = new HashSet<string>();

    public void PickUpItem(string itemID)
    {
        if(!items.Contains(itemID))
        {
            items.Add(itemID);
            Debug.Log($"Picked up {itemID}");
        }
    }

    public bool HasItem(string itemID)
    {
        return items.Contains(itemID);
    }
}
