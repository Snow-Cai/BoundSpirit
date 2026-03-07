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

        // Show choices after a specific line index (inline node),
        // but the chosen branch will go to a separate DialogueAsset.
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
                currentDialogue.choicesAfterLineIndex < 0 &&
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
    /// Saves the choice (if applicable), executes events, and branches via nextDialogue.
    /// The current DialogueAsset always finishes after a choice.
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

        // Branch to a separate DialogueAsset.
        if (choice.nextDialogue != null)
        {
            QueueDialogue(choice.nextDialogue);
        }

        // This asset always ends after a choice; no inline continuation.
        FinishCurrentDialogue();
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
            GameInputState.DialogueActive = false;
            return;
        }

        currentDialogue = dialogueQueue.Dequeue();
        currentLineIndex = 0;

        State = DialogueState.PlayingLine;
        GameInputState.DialogueActive = true;

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
        HandleDialogueCompletionEffects(finishedDialogue);

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

    private void HandleDialogueCompletionEffects(DialogueAsset finishedDialogue)
    {
        if (finishedDialogue == null || SaveSystem.Instance == null)
            return;

        switch (finishedDialogue.dialogueID)
        {
            case "Chapter0_tombstonePrimary":
                SaveSystem.Instance.SetKnowsNameIsAkila(true);
                Debug.Log("Story Flag Set: knowsNameIsAkila");
                break;
        }
    }

    // Ensure input state is reset if object is disabled or destroyed.
    private void OnDisable()
    {
        // If we leave the scene while dialogue is active, make sure we don't soft-lock input.
        if (GameInputState.DialogueActive)
            GameInputState.DialogueActive = false;
    }

    private void OnDestroy()
    {
        // Extra safety in case object is destroyed during scene load
        if (GameInputState.DialogueActive)
            GameInputState.DialogueActive = false;
    }

}