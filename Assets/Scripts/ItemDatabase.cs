using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    public ItemData[] allItems;     //assign all ItemData assets

    private static readonly Dictionary<string, ItemData> GlobalItemsById = new Dictionary<string, ItemData>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        GlobalItemsById.Clear();
    }

    private void Awake()
    {
        Instance = this;
        RegisterItems(allItems);
    }

    public ItemData GetItemByID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        RegisterItems(allItems);

        if (GlobalItemsById.TryGetValue(id, out ItemData cachedItem) && cachedItem != null)
            return cachedItem;

        ItemData[] loadedItems = Resources.FindObjectsOfTypeAll<ItemData>();
        RegisterItems(loadedItems);

        if (GlobalItemsById.TryGetValue(id, out cachedItem) && cachedItem != null)
            return cachedItem;

        foreach (ItemData item in allItems)
        {
            if(item.itemID == id) return item;
        }

        return null;
    }

    private static void RegisterItems(ItemData[] items)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            ItemData item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemID))
                continue;

            GlobalItemsById[item.itemID] = item;
        }
    }
}
