using UnityEngine;

[CreateAssetMenu(menuName = "Puzzles/PolaroidData", fileName = "NewPolaroid")]
public class PolaroidData : ScriptableObject
{
    [Header("Identity")]
    public string polaroidID;           //like "polaroid_01_birth"
    public int correctOrder;            //0 = first, 5 = last

    [Header("Display")]
    public Sprite polaroidImage;        //placeholder or real art
    public string year;                 // like "2001"
    public string captionText;          //text shown when inspecting

    [Header("Clue Description")]
    [TextArea(2, 4)]
    public string inspectDescription;   //shown in inspect popup
}