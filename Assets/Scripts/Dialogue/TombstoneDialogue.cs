using UnityEngine;
using System.Collections.Generic;

public class TombstoneDialogue : MonoBehaviour
{
    void Start()
    {
        CreateDialogue();
    }

    void CreateDialogue()
    {
        var interactable = GetComponent<InteractableObject>();
        if (interactable == null) return;

        //Create the dialogue
        Dialogue dialogue = new Dialogue();
        dialogue.dialogueID = "tombstone_awakening";
        dialogue.lines = new List<DialogueLine>();

        //Line 1: Confusion
        DialogueLine line1 = new DialogueLine();
        line1.speakerName = "???";
        line1.dialogueText = "What... where am I?";
        dialogue.lines.Add(line1);

        //Line 2: Looking around
        DialogueLine line2 = new DialogueLine();
        line2.speakerName = "???";
        line2.dialogueText = "A graveyard? Why am I here? Why does everything feel so... strange?";
        dialogue.lines.Add(line2);

        //Line 3: Notice the tombstone
        DialogueLine line3 = new DialogueLine();
        line3.speakerName = "???";
        line3.dialogueText = "Wait... this tombstone. Let me read it closer...";
        dialogue.lines.Add(line3);

        //Line 4: Reading the name
        DialogueLine line4 = new DialogueLine();
        line4.speakerName = "???";
        line4.dialogueText = "Akila... that's... that's my name. But why is it on a—";
        dialogue.lines.Add(line4);

        //Line 5: The realization
        DialogueLine line5 = new DialogueLine();
        line5.speakerName = "Akila";
        line5.dialogueText = "No. No, no, no. This can't be right.";
        dialogue.lines.Add(line5);

        // Line 6: Denial
        DialogueLine line6 = new DialogueLine();
        line6.speakerName = "Akila";
        line6.dialogueText = "I'm... I'm dead? How? When? I don't remember anything...";
        dialogue.lines.Add(line6);

        //Line 7: Looking at hands
        DialogueLine line7 = new DialogueLine();
        line7.speakerName = "Akila";
        line7.dialogueText = "My hands... I can see through them. I really am... a ghost.";
        dialogue.lines.Add(line7);

        //Line 8: The questions
        DialogueLine line8 = new DialogueLine();
        line8.speakerName = "Akila";
        line8.dialogueText = "What happened to me? How did I die? I need to remember...";
        dialogue.lines.Add(line8);

        //Line 9: The pull
        DialogueLine line9 = new DialogueLine();
        line9.speakerName = "Akila";
        line9.dialogueText = "There's something... unfinished. I can feel it. Something keeping me here.";
        dialogue.lines.Add(line9);

        //Line 10: The determination
        DialogueLine line10 = new DialogueLine();
        line10.speakerName = "Akila";
        line10.dialogueText = "I have to find out what happened. I have to remember.";
        dialogue.lines.Add(line10);

        //No choices for this dialogue. linear story moment
        dialogue.choices = new List<DialogueChoice>();

        //Assign to interactable
        interactable.objectDialogue = dialogue;
    }
}