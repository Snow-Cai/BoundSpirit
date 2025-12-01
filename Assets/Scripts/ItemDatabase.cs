using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    public ItemData[] allItems;     //assign all ItemData assets
    private void Awake()
    {
        Instance = this;
    }

    public ItemData GetItemByID(string id)
    {
        foreach (ItemData item in allItems)
        {
            if(item.itemID == id) return item;
        }
        return null;
    }
}
