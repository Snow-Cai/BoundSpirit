using UnityEngine;

[CreateAssetMenu(fileName = "JournalPage", menuName = "Journal/Page")]
public class JournalPage : ScriptableObject
{
    [TextArea(5, 10)]
    public string leftPageText;

    [TextArea(5, 10)]
    public string rightPageText;

    public Sprite leftImage;
    public Sprite rightImage;
}