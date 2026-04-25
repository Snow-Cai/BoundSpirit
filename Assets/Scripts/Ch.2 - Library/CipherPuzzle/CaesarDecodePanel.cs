using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-contained Caesar cipher decode panel.
/// Intended to be enabled by a generic interaction system.
/// Handles gating, initialization, solving, and persistence.
/// </summary>
public sealed class CaesarDecodePanel : MonoBehaviour
{
    [Header("Puzzle Config")]
    [SerializeField] private CaesarNotePuzzleData puzzleData;
    [SerializeField] private ItemData requiredNoteItem;
    [SerializeField] private ItemData requiredWheelItem;
    [SerializeField] private string legacyPuzzleID = "CaesarCipher Puzzle";

    [Header("Runtime Dependencies (Auto-Resolved)")]
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private string playerTag = "Player";

    [Header("UI References")]
    [SerializeField] private TMP_Text encodedText;
    [SerializeField] private TMP_Text mappingText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;

    private bool solved;

    #region Unity Lifecycle

    private void OnEnable()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        InitializeOrGate();
    }

    private void OnDisable()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);
    }

    #endregion

    #region Initialization

    private void InitializeOrGate()
    {
        ResolveDependencies();

        if (puzzleData == null)
        {
            SetBlockedState("Missing puzzle data.");
            Debug.LogError("[CaesarDecodePanel] PuzzleData not assigned.", this);
            return;
        }

        if (playerInventory == null)
        {
            SetBlockedState("Missing inventory.");
            Debug.LogError("[CaesarDecodePanel] PlayerInventory not found.", this);
            return;
        }

        if (requiredNoteItem != null && !playerInventory.HasItem(requiredNoteItem))
        {
            SetBlockedState("You need the encoded note.");
            return;
        }

        if (requiredWheelItem != null && !playerInventory.HasItem(requiredWheelItem))
        {
            SetBlockedState("You need the Caesar Cipher Wheel.");
            return;
        }

        // Check solved state from SaveSystem
        if (saveSystem != null && saveSystem.IsPuzzleSolved(puzzleData.PuzzleKey))
        {
            solved = true;
            SetSolvedState();
            return;
        }

        solved = false;

        string encoded = CaesarCipher.Shift(puzzleData.Plaintext, puzzleData.Shift);
        encodedText.text = encoded;

        // Player is decoding → show negative shift mapping
        mappingText.text = CaesarCipher.BuildAlphabetStrip(-puzzleData.Shift);

        feedbackText.text = "Find out the decoded message.";

        answerInput.text = string.Empty;
        answerInput.interactable = true;

        if (submitButton != null)
            submitButton.interactable = true;
    }

    private void ResolveDependencies()
    {
        if (saveSystem == null)
            saveSystem = FindFirstObjectByType<SaveSystem>();

        if (playerInventory != null)
            return;

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerInventory = player.GetComponent<PlayerInventory>();
        }

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    #endregion

    #region States

    private void SetSolvedState()
    {
        encodedText.text = CaesarCipher.Shift(puzzleData.Plaintext, puzzleData.Shift);
        mappingText.text = CaesarCipher.BuildAlphabetStrip(-puzzleData.Shift);

        feedbackText.text = "Decoded!";

        answerInput.text = puzzleData.Plaintext;
        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;
    }

    private void SetBlockedState(string message)
    {
        encodedText.text = string.Empty;
        mappingText.text = string.Empty;
        feedbackText.text = message;

        answerInput.text = string.Empty;
        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;
    }

    #endregion

    #region Actions

    private void Submit()
    {
        if (puzzleData == null || solved)
            return;

        string typed = CaesarCipher.NormalizeForCompare(answerInput.text);
        string expected = CaesarCipher.NormalizeForCompare(puzzleData.Plaintext);

        if (typed != expected)
        {
            feedbackText.text = "Not quite… try again.";
            return;
        }

        solved = true;
        feedbackText.text = "Decoded!";

        if (saveSystem != null)
        {
            saveSystem.UnlockPuzzle(puzzleData.PuzzleKey);
        }

        if (PuzzleBridge.currentPuzzleSource != null)
        {
            PuzzleBridge.currentPuzzleSource.OnPuzzleSolved();
        }
        else if (saveSystem != null && !string.IsNullOrWhiteSpace(legacyPuzzleID))
        {
            saveSystem.UnlockPuzzle(legacyPuzzleID);
        }

        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;

        onSolved?.Invoke();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
