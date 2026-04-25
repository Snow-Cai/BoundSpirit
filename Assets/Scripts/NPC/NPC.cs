using UnityEngine;

public class NPC : MonoBehaviour
{
    public string npcName;
    [TextArea] public string[] dialogueLines;

    public void Interact()
    {
        Debug.Log($"{npcName} says: {dialogueLines[0]}");
    }
}
sbyte