using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private string requiredWordSearchPuzzleKey = "Library_WordSearch_13";
    [SerializeField] private string legacyPuzzleID = "CaesarCipher Puzzle";

    [Header("Runtime Dependencies (Auto-Resolved)")]
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private string playerTag = "Player";

    [Header("UI References")]
    [SerializeField] private TMP_Text encodedText;
    [SerializeField] private TMP_Text mappingText;
    [SerializeField] private TMP_Text shiftValueText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button shiftBackwardButton;
    [SerializeField] private Button shiftForwardButton;

    [Header("Events")]
    [SerializeField] private DialogueAsset onSolveDialogue;
    [SerializeField] private UnityEvent onSolved;

    private bool solved;
    private int currentPreviewShift;

    private void OnEnable()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (shiftBackwardButton != null)
            shiftBackwardButton.onClick.AddListener(PreviewPreviousShift);

        if (shiftForwardButton != null)
            shiftForwardButton.onClick.AddListener(PreviewNextShift);

        InitializeOrGate();
    }

    private void OnDisable()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);

        if (shiftBackwardButton != null)
            shiftBackwardButton.onClick.RemoveListener(PreviewPreviousShift);

        if (shiftForwardButton != null)
            shiftForwardButton.onClick.RemoveListener(PreviewNextShift);
    }

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

        if (!string.IsNullOrWhiteSpace(requiredWordSearchPuzzleKey) &&
            saveSystem != null &&
            !saveSystem.IsPuzzleSolved(requiredWordSearchPuzzleKey))
        {
            SetBlockedState("Solve the library word search to learn the shift hint.");
            return;
        }

        if (saveSystem != null && saveSystem.IsPuzzleSolved(puzzleData.PuzzleKey))
        {
            solved = true;
            SetSolvedState();
            return;
        }

        solved = false;
        currentPreviewShift = 0;

        encodedText.text = CaesarCipher.Shift(puzzleData.Plaintext, puzzleData.Shift);
        RefreshShiftPreview();
        feedbackText.text = "Decode the message.";

        answerInput.text = string.Empty;
        answerInput.interactable = true;

        if (submitButton != null)
            submitButton.interactable = true;

        SetShiftControlsInteractable(true);
    }

    private void ResolveDependencies()
    {
        if (saveSystem == null)
            saveSystem = FindFirstObjectByType<SaveSystem>();

        if (playerInventory != null)
            return;

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerInventory = player.GetComponent<PlayerInventory>();
        }

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void SetSolvedState()
    {
        currentPreviewShift = Mathf.Abs(puzzleData.Shift % 26);
        encodedText.text = CaesarCipher.Shift(puzzleData.Plaintext, puzzleData.Shift);
        RefreshShiftPreview();
        feedbackText.text = "Decoded!";

        answerInput.text = puzzleData.Plaintext;
        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;

        SetShiftControlsInteractable(false);
    }

    private void SetBlockedState(string message)
    {
        encodedText.text = string.Empty;
        mappingText.text = string.Empty;

        if (shiftValueText != null)
            shiftValueText.text = string.Empty;

        feedbackText.text = message;

        answerInput.text = string.Empty;
        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;

        SetShiftControlsInteractable(false);
    }

    private void Submit()
    {
        if (puzzleData == null || solved)
            return;

        string typed = CaesarCipher.NormalizeForCompare(answerInput.text);
        string expected = CaesarCipher.NormalizeForCompare(puzzleData.Plaintext);

        if (typed != expected)
        {
            feedbackText.text = "Not quite... try again.";
            return;
        }

        solved = true;
        feedbackText.text = "Decoded!";

        if (saveSystem != null)
        {
            saveSystem.UnlockPuzzle(puzzleData.PuzzleKey);

            if (!string.IsNullOrWhiteSpace(legacyPuzzleID))
                saveSystem.UnlockPuzzle(legacyPuzzleID);
        }

        answerInput.interactable = false;

        if (submitButton != null)
            submitButton.interactable = false;

        if (onSolveDialogue != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.StartDialogue(onSolveDialogue);

        onSolved?.Invoke();
    }

    private void PreviewPreviousShift()
    {
        currentPreviewShift = (currentPreviewShift + 25) % 26;
        RefreshShiftPreview();
    }

    private void PreviewNextShift()
    {
        currentPreviewShift = (currentPreviewShift + 1) % 26;
        RefreshShiftPreview();
    }

    private void RefreshShiftPreview()
    {
        if (mappingText != null)
            mappingText.text = CaesarCipher.BuildAlphabetStrip(-currentPreviewShift);

        if (shiftValueText != null)
            shiftValueText.text = $"+{currentPreviewShift}";
    }

    private void SetShiftControlsInteractable(bool interactable)
    {
        if (shiftBackwardButton != null)
            shiftBackwardButton.interactable = interactable;

        if (shiftForwardButton != null)
            shiftForwardButton.interactable = interactable;
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
