using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset", fileName = "NewDialogue")]
public class DialogueAsset : ScriptableObject
{
    [Header("Identity")]
    public string dialogueID;

    [Header("Content")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Choices")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Options")]
    public bool saveAfterDialogue = true;
    public bool showChoicesAtEnd = true;
}