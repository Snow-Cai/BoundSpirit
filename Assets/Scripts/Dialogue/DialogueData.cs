using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(3, 10)]
    public string dialogueText;

    public AudioClip voiceClip;
}

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 5)]
    public string choiceText;

    public DialogueAsset nextDialogue;

    public List<string> requiredFlags = new List<string>();
    public List<string> forbiddenFlags = new List<string>();

    public UnityEvent onChoiceSelected;
}