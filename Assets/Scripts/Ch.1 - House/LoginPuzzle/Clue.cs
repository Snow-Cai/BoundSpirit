using UnityEngine;

[CreateAssetMenu(fileName = "NewClue", menuName = "Puzzle/Clue")]
public class Clue : ItemData
{ 
    [Header("Identification")]
    public string clueID;
    public string clueName;
    public string clueLetter;

    [Header("Descriptions")]
    [TextArea(2, 6)]
    public string clueText;

    [TextArea(2, 6)]
    public string hintDescription;

    [Header("Visual Data")]
    public Sprite clueIcon;
    public Color highlightColor = Color.yellow;

    [Header("Gameplay Flags")]
    public bool isRequiredForPassword = true;
    public bool canBeReviewedInJournal = true;
    public bool canTriggerEvents = false;

    //validation
    public bool IsValid()
    {
        return !(string.IsNullOrWhiteSpace(clueName) || string.IsNullOrWhiteSpace(clueLetter));
    }
}