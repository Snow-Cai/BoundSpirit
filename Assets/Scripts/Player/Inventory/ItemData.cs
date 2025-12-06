using UnityEngine;

[CreateAssetMenu(menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemID;       //identifier string e.g. "SAFE_KEY"
    public string itemName;     //display name
    public Sprite icon;
}
