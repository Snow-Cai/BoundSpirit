using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueState
{
    Inactive,
    PlayingLine,
    WaitingForAdvance,
    WaitingForChoice
}

public class DialogueSystem : MonoBehaviour
{
    /// <summary>
    /// Global dialogue system instance.
    /// Ensures only one active controller exists.
    /// </summary>
    public static DialogueSystem Instance { get; private set; }

    [Header("Typing Settings")]
    [Min(1f)]
    public float charactersPerSecond = 40f;

    public event Action<DialogueAsset> OnDialogueStarted;
    public event Action<DialogueLine> OnLineStarted;
    public event Action<List<DialogueChoice>> OnChoicesOffered;
    public event Action<DialogueAsset> OnDialogueEnded;

    private readonly Queue<DialogueAsset> dialogueQueue = new Queue<DialogueAsset>();
    private DialogueAsset currentDialogue;
    private int currentLineIndex = -1;

    public DialogueState State { get; private set; } = DialogueState.Inactive;

    public float TypingSpeed => charactersPerSecond;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DialogueSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns true if any dialogue is currently running.
    /// Used by interactables and UI to block input.
    /// </summary>
    public bool IsDialogueActive()
    {
        return State != DialogueState.Inactive;
    }

    /// <summary>
    /// Begins a new dialogue immediately, replacing anything in the queue.
    /// </summary>
    public void StartDialogue(DialogueAsset asset)
    {
        if (asset == null)
        {
            Debug.LogWarning("StartDialogue called with null asset.");
            return;
        }

        dialogueQueue.Clear();
        dialogueQueue.Enqueue(asset);
        StartNextDialogueFromQueue();
    }

    /// <summary>
    /// Adds a dialogue to the queue.
    /// If no dialogue is active, it will start immediately.
    /// </summary>
    public void QueueDialogue(DialogueAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        dialogueQueue.Enqueue(asset);

        if (!IsDialogueActive())
        {
            StartNextDialogueFromQueue();
        }
    }

    /// <summary>
    /// Called by the UI when the player advances after fully displaying a line.
    /// Moves to the next line or displays choices.
    /// </summary>
    public void AdvanceLine()
    {
        if (currentDialogue == null || State != DialogueState.WaitingForAdvance)
        {
            return;
        }

        // Inline choices: show choices after a specific line index
        if (currentDialogue.choicesAfterLineIndex >= 0 &&
            currentDialogue.choices != null &&
            currentDialogue.choices.Count > 0 &&
            currentLineIndex == currentDialogue.choicesAfterLineIndex)
        {
            State = DialogueState.WaitingForChoice;
            OnChoicesOffered?.Invoke(currentDialogue.choices);
            return;
        }

        currentLineIndex++;

        // End-of-dialogue behavior
        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            bool canShowEndChoices =
                currentDialogue.showChoicesAtEnd &&
                currentDialogue.choicesAfterLineIndex < 0 &&      // only if not already used inline
                currentDialogue.choices != null &&
                currentDialogue.choices.Count > 0;

            if (canShowEndChoices)
            {
                State = DialogueState.WaitingForChoice;
                OnChoicesOffered?.Invoke(currentDialogue.choices);
            }
            else
            {
                FinishCurrentDialogue();
            }
        }
        else
        {
            PlayCurrentLine();
        }
    }

    /// <summary>
    /// Wrapper used if the UI does not supply a choice index.
    /// </summary>
    public void SelectChoice(DialogueChoice choice)
    {
        SelectChoice(choice, -1);
    }

    /// <summary>
    /// Called when a choice button is selected.
    /// Saves the choice (if applicable), executes events, and branches if needed.
    /// </summary>
    public void SelectChoice(DialogueChoice choice, int choiceIndex)
    {
        if (State != DialogueState.WaitingForChoice || choice == null)
        {
            return;
        }

        choice.onChoiceSelected?.Invoke();

        if (SaveSystem.Instance != null &&
            currentDialogue != null &&
            !string.IsNullOrEmpty(currentDialogue.dialogueID) &&
            choiceIndex >= 0)
        {
            SaveSystem.Instance.SaveDialogueChoice(currentDialogue.dialogueID, choiceIndex);
        }

        // Branching: queue follow-up dialogue to run after this one finishes
        if (choice.nextDialogue != null)
        {
            QueueDialogue(choice.nextDialogue);
        }

        bool hasInlineChoices =
            currentDialogue != null &&
            currentDialogue.choicesAfterLineIndex >= 0 &&
            currentDialogue.choicesAfterLineIndex < currentDialogue.lines.Count - 1;

        if (hasInlineChoices)
        {
            // Resume this dialogue on the line after the one that triggered the choices
            State = DialogueState.PlayingLine;
            currentLineIndex = currentDialogue.choicesAfterLineIndex + 1;
            PlayCurrentLine();
        }
        else
        {
            FinishCurrentDialogue();
        }
    }

    /// <summary>
    /// UI notifies the system that text finished typing, enabling progression.
    /// </summary>
    public void NotifyLineFinishedTyping()
    {
        if (State == DialogueState.PlayingLine)
        {
            State = DialogueState.WaitingForAdvance;
        }
    }

    /// <summary>
    /// Pops the next dialogue in the queue and begins playing it.
    /// </summary>
    private void StartNextDialogueFromQueue()
    {
        if (dialogueQueue.Count == 0)
        {
            currentDialogue = null;
            State = DialogueState.Inactive;
            return;
        }

        currentDialogue = dialogueQueue.Dequeue();
        currentLineIndex = 0;

        State = DialogueState.PlayingLine;

        TryNotifyDialogueStarted(currentDialogue);
        OnDialogueStarted?.Invoke(currentDialogue);

        PlayCurrentLine();
    }

    /// <summary>
    /// Plays the current line and notifies UI to begin typing.
    /// </summary>
    private void PlayCurrentLine()
    {
        if (currentDialogue == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentDialogue.lines.Count)
        {
            Debug.LogWarning("PlayCurrentLine: invalid index.");
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        State = DialogueState.PlayingLine;
        OnLineStarted?.Invoke(line);
    }

    /// <summary>
    /// Finalizes a dialogue asset, saves (if configured), and moves to next in queue.
    /// </summary>
    private void FinishCurrentDialogue()
    {
        DialogueAsset finishedDialogue = currentDialogue;

        TryNotifyDialogueEnded(finishedDialogue);
        OnDialogueEnded?.Invoke(finishedDialogue);

        currentDialogue = null;
        State = DialogueState.Inactive;

        StartNextDialogueFromQueue();
    }

    /// <summary>
    /// Saves that a dialogue has been viewed.
    /// </summary>
    private void TryNotifyDialogueStarted(DialogueAsset asset)
    {
        if (asset == null ||
            SaveSystem.Instance == null ||
            string.IsNullOrEmpty(asset.dialogueID))
        {
            return;
        }

        SaveSystem.Instance.MarkDialogueViewed(asset.dialogueID);
    }

    /// <summary>
    /// Saves the game if the dialogue asset is marked to auto-save when finished.
    /// </summary>
    private void TryNotifyDialogueEnded(DialogueAsset asset)
    {
        if (asset == null ||
            SaveSystem.Instance == null ||
            !asset.saveAfterDialogue)
        {
            return;
        }

        SaveSystem.Instance.SaveGame();
    }
}