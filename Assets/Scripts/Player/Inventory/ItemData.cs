using UnityEngine;

[CreateAssetMenu(menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemID;       //identifier string e.g. "SAFE_KEY"
    public string itemName;     //display name
    public Sprite icon;

    [TextArea(3, 5)]
    public string description;

    public bool canInspect = true;
}
