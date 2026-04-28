using System;
using System.Collections;
using System.Collections.Generic;
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
    private const string SavedAnswerPrefix = "ANSWER=";
    private const string SavedShiftPrefix = "SHIFT=";
    private const string SavedPartialPrefix = "PARTIAL=";
    private static CaesarDecodePanel instance;

    [Header("Clarity Choice")]
    [SerializeField] private DialogueAsset cipherReactionDialogue;


    public static bool IsPanelActuallyOpen =>
        instance != null &&
        instance.isActiveAndEnabled &&
        instance.gameObject.activeInHierarchy;

    [Header("Puzzle Config")]
    [SerializeField] private CaesarNotePuzzleData puzzleData;
    [SerializeField] private ItemData requiredNoteItem;
    [SerializeField] private ItemData requiredWheelItem;
    [SerializeField] private string requiredWordSearchPuzzleKey = "Library_WordSearch_13";
    [SerializeField] private string legacyPuzzleID = "CaesarCipher Puzzle";
    [SerializeField] private string maskedDecodedPhrase = "case file";
    [SerializeField] private string maskedPhrasePlaceholder = "&#@* ^~+*";
    [TextArea(2, 6)]
    [SerializeField] private string maskedEncodedTextOverride = "Gur &#@* ^~+*\njvyy erirny\ngur gehgu.";
    [SerializeField] private string revealMaskedPhraseAfterPuzzleKey = "StationLocker";
    [SerializeField] private string revealMaskedPhraseAfterLegacyPuzzleKey = "";

    [Header("Runtime Dependencies (Auto-Resolved)")]
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private string playerTag = "Player";

    [Header("UI References")]
    [SerializeField] private TMP_Text encodedText;
    [SerializeField] private TMP_Text mappingText;
    [SerializeField] private TMP_Text shiftValueText;
    [SerializeField] private GameObject shiftHeaderObject;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private RectTransform answerSlotsRoot;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button shiftBackwardButton;
    [SerializeField] private Button shiftForwardButton;

    [Header("Wheel Visual")]
    [SerializeField] private RectTransform wheelVisualRoot;
    [SerializeField] private RectTransform rotatingInnerWheel;
    [SerializeField] private float wheelPointerRadius = 135f;
    [SerializeField] private float wheelAngleOffset = 0f;
    [SerializeField] private bool keepAlphabetStripVisible = true;

    [Header("Answer Slots")]
    [SerializeField] private int[] wordsPerRow = { 3, 2, 2 };
    [SerializeField] private int maxLettersPerRow = 14;
    [SerializeField] private Vector2 slotSize = new(34f, 42f);
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private float wordSpacing = 18f;
    [SerializeField] private float rowSpacing = 10f;
    [SerializeField] private Color slotBackgroundColor = new Color32(0xB4, 0xB4, 0xB4, 0xFF);
    [SerializeField] private float staticTokenFontSize = 35f;
    [SerializeField] private Color staticTokenFontColor = Color.black;
    [SerializeField] private Color hiddenSlotTextColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private float encodedLineSpacing = 18f;

    [Header("Events")]
    [SerializeField] private DialogueAsset onCorrectShiftDialogue;
    [SerializeField] private DialogueAsset onFinalSolveDialogue;
    [SerializeField] private DialogueAsset onWordSearchRequiredDialogue;
    [SerializeField] private UnityEvent onSolved;
    [SerializeField] private bool wasSolved = false;            // Use to ensure onPuzzleSolved() is run only once

    [SerializeField] private GameObject decodeUIPanel;
    [SerializeField] private GameObject resultPaperPanel;

    [Header("Solve Bridge")]
    [SerializeField] private InteractableObject puzzleInteractable;

    [Header("Puzzle Identity")]
    public string puzzleID = "CaesarCipher Puzzle";

    private readonly List<TMP_InputField> answerSlots = new();
    private readonly Dictionary<TMP_InputField, TMP_Text> answerSlotTextComponents = new();
    private readonly Dictionary<TMP_InputField, Color> answerSlotVisibleColors = new();
    private readonly List<int> wordLengths = new();
    private bool solved;
    private bool partialDecodeComplete;
    private int currentPreviewShift;
    private string savedAnswer = string.Empty;
    private bool suppressProgressSave;
    private bool suppressSlotCallbacks;
    private void Awake()
    {
        instance = this;
        ResolveWheelVisualReferences();
    }

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
        if (!Application.isPlaying)
            return;

        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);

        if (shiftBackwardButton != null)
            shiftBackwardButton.onClick.RemoveListener(PreviewPreviousShift);

        if (shiftForwardButton != null)
            shiftForwardButton.onClick.RemoveListener(PreviewNextShift);

        PersistProgress();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void InitializeOrGate()
    {
        ResolveDependencies();
        ResolveWheelVisualReferences();

        if(resultPaperPanel != null) resultPaperPanel.SetActive(false);

        bool hasNote = playerInventory != null && requiredNoteItem != null && playerInventory.HasItem(requiredNoteItem);
        bool hasWheel = playerInventory != null && requiredWheelItem != null && playerInventory.HasItem(requiredWheelItem);

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

        bool missingNote = requiredNoteItem != null && !hasNote;
        bool missingWheel = requiredWheelItem != null && !hasWheel;

        if (missingNote || missingWheel)
        {
            SetBlockedState(
                BuildMissingItemMessage(missingNote, missingWheel),
                keepPuzzleLayoutVisible: hasNote && !missingNote,
                showEncodedWriting: hasNote,
                showShiftPreview: hasWheel);
            return;
        }

        ResolvePuzzleInteractableReference();
        ApplySavedProgress();

        bool finalRevealAlreadyCompleted = HasFinalRevealBeenCompleted();
        if (finalRevealAlreadyCompleted)
        {
            ApplyFinalRevealState(false);
            return;
        }

        if (!IsRequiredWordSearchSolved())
        {
            SetBlockedState(
                "Solve the library word search to learn the shift hint.",
                keepPuzzleLayoutVisible: true,
                showEncodedWriting: true,
                showShiftPreview: true);
            PlayWordSearchRequiredDialogue();
            return;
        }

        if (CanRevealFinalMessage())
        {
            ApplyFinalRevealState(true);
            return;
        }

        solved = false;
        SyncAnswerSlotsFromHierarchy();
        SetAnswerSlotsRootVisible(true);
        SetAnswerSlotTextVisible(true);

        ApplyEncodedTextPresentation();
        SetEncodedWritingVisible(true);
        SetWheelVisualVisible(true);
        encodedText.text = GetDisplayedEncodedText();
        RefreshShiftPreview();

        if (partialDecodeComplete && !ShouldRevealMaskedPhrase())
        {
            feedbackText.text = "I have the right shift, but part of the message is still obscured.";
            SetAnswerSlotsInteractable(false);

            if (submitButton != null)
                submitButton.interactable = false;

            SetShiftControlsInteractable(false);
            return;
        }

        feedbackText.text = "Decode the message.";
        SetAnswerSlotsInteractable(true);

        if (submitButton != null)
            submitButton.interactable = true;

        SetShiftControlsInteractable(true);
    }

    private void ApplyFinalRevealState(bool triggerSolveEffects)
    {
        solved = true;

        if(decodeUIPanel != null) decodeUIPanel.SetActive(false);
        if (resultPaperPanel != null) resultPaperPanel.SetActive(true);
        SetAnswerSlotsRootVisible(false);
        SetEncodedWritingVisible(false);
        feedbackText.text = "The final message has been decoded.";

        if (triggerSolveEffects)
        {
            if (puzzleInteractable != null && puzzleInteractable.isPuzzleOpen)
            {
                puzzleInteractable.ClosePuzzle();
            }

            ShowConfiguredFinalTidbit();

            if (saveSystem != null)
            {
                if (puzzleData != null && !string.IsNullOrWhiteSpace(puzzleData.PuzzleKey))
                {
                    saveSystem.UnlockPuzzle(puzzleData.PuzzleKey);
                }

                if (!string.IsNullOrWhiteSpace(legacyPuzzleID))
                {
                    saveSystem.UnlockPuzzle(legacyPuzzleID);
                }
            }

            StartCoroutine(PlayFinalDialogueAfterTidbitCloses());
            ClearSavedProgress();
            onSolved?.Invoke();
        }

        wasSolved = true;

        if (submitButton != null)
            submitButton.interactable = false;

        SetShiftControlsInteractable(false);
        SetAnswerSlotsInteractable(false);
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
        SyncAnswerSlotsFromHierarchy();
        SetAnswerSlotsRootVisible(true);
        SetAnswerSlotTextVisible(true);

        ApplyEncodedTextPresentation();
        SetEncodedWritingVisible(true);
        SetWheelVisualVisible(true);
        encodedText.text = GetDisplayedEncodedText();
        RefreshShiftPreview();
        feedbackText.text = "Decoded!";

        SetAnswerFromString(BuildAnswerTarget());
        SetAnswerSlotsInteractable(false);

        if (submitButton != null)
            submitButton.interactable = false;

        SetShiftControlsInteractable(false);
        ClearSavedProgress();
    }

    private void SetBlockedState(
        string message,
        bool keepPuzzleLayoutVisible = false,
        bool showEncodedWriting = false,
        bool showShiftPreview = false)
    {
        SetEncodedWritingVisible(showEncodedWriting);
        SetWheelVisualVisible(showShiftPreview);
        if (encodedText != null)
            encodedText.text = showEncodedWriting ? GetDisplayedEncodedText() : string.Empty;

        if (mappingText != null)
            mappingText.text = showShiftPreview ? CaesarCipher.BuildAlphabetStrip(-currentPreviewShift) : string.Empty;

        if (shiftValueText != null)
            shiftValueText.text = showShiftPreview ? $"+{currentPreviewShift}" : string.Empty;

        if (shiftHeaderObject != null)
            shiftHeaderObject.SetActive(showShiftPreview);

        if (shiftBackwardButton != null)
            shiftBackwardButton.gameObject.SetActive(showShiftPreview);

        if (shiftForwardButton != null)
            shiftForwardButton.gameObject.SetActive(showShiftPreview);

        if (feedbackText != null)
            feedbackText.text = message;

        answerSlots.Clear();
        answerSlotTextComponents.Clear();
        answerSlotVisibleColors.Clear();
        wordLengths.Clear();

        if (answerInput != null)
        {
            answerInput.text = string.Empty;
            answerInput.interactable = false;
            answerInput.gameObject.SetActive(false);
        }

        if (submitButton != null)
            submitButton.interactable = false;

        SetAnswerSlotsRootVisible(keepPuzzleLayoutVisible);

        if (keepPuzzleLayoutVisible)
        {
            SyncAnswerSlotsFromHierarchy();
            SetAnswerSlotTextVisible(true);
            SetAnswerFromString(string.Empty);
            SetAnswerSlotsInteractable(false);
        }

        SetShiftControlsInteractable(false);
    }

    private string BuildMissingItemMessage(bool missingNote, bool missingWheel)
    {
        if (missingNote && missingWheel)
            return $"You need both the {GetItemLabel(requiredNoteItem, "cipher note")} and the {GetItemLabel(requiredWheelItem, "cipher wheel")}.";

        if (missingNote)
            return $"You need the {GetItemLabel(requiredNoteItem, "cipher note")}.";

        if (missingWheel)
            return $"You need the {GetItemLabel(requiredWheelItem, "cipher wheel")}.";

        return "You're missing something.";
    }

    private static string GetItemLabel(ItemData item, string fallback)
    {
        return item != null && !string.IsNullOrWhiteSpace(item.itemName)
            ? item.itemName
            : fallback;
    }

    private void Submit()
    {
        if (puzzleData == null || solved)
            return;

        if (currentPreviewShift != Mathf.Abs(puzzleData.Shift % 26))
        {
            feedbackText.text = "The shift still doesn't look right.";
            PersistProgress();
            return;
        }

        string typed = CaesarCipher.NormalizeForCompare(GetCurrentAnswerString());

        bool revealWords = ShouldRevealMaskedPhrase();

        if (!revealWords)
        {
            string partialExpected = "THEWILLREVEALTHETRUTH";
            string partialTyped = typed.Replace(" ", "");

            if (partialTyped == partialExpected)
            {
                partialDecodeComplete = true;
                feedbackText.text = "I can read part of it now, but some symbols still need another clue.";

                LibraryPuzzleStateBridge.Instance?.SetCipherHalfSolved();        // Connect painting puzzle part

                SetAnswerSlotsInteractable(false);

                if (submitButton != null)
                    submitButton.interactable = false;

                SetShiftControlsInteractable(false);

                TryPlayDialogue(onCorrectShiftDialogue, true);
                PersistProgress();
                return;
            }

            feedbackText.text = "Not quite... try again.";
            PersistProgress();
            return;
        }

        bool canFinalize = CanRevealFinalMessage();

        if (!canFinalize)
        {
            feedbackText.text = "I'm missing something... part of the message is still unclear. Maybe I should look around for clues.";
            return;
        }

        string expected = CaesarCipher.NormalizeForCompare("THE CASE FILE WILL REVEAL THE TRUTH");

        if (typed != expected)
        {
            feedbackText.text = "Not quite... try again.";
            PersistProgress();
            return;
        }

        // Queue the normal solve dialogue
        if (onFinalSolveDialogue != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(onFinalSolveDialogue);

        // Queue the clarity reaction dialogue
        if (cipherReactionDialogue != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(cipherReactionDialogue);


        ApplyFinalRevealState(!HasFinalRevealBeenCompleted());
    }

    private void PreviewPreviousShift()
    {
        currentPreviewShift = (currentPreviewShift + 25) % 26;
        RefreshShiftPreview();
        PersistProgress();
    }

    private void PreviewNextShift()
    {
        currentPreviewShift = (currentPreviewShift + 1) % 26;
        RefreshShiftPreview();
        PersistProgress();
    }

    private void RefreshShiftPreview()
    {
        if (mappingText != null)
            mappingText.text = keepAlphabetStripVisible
                ? CaesarCipher.BuildAlphabetStrip(-currentPreviewShift)
                : string.Empty;

        if (shiftValueText != null)
            shiftValueText.text = $"+{currentPreviewShift}";

        if (shiftHeaderObject != null && !shiftHeaderObject.activeSelf)
            shiftHeaderObject.SetActive(true);

        if (shiftBackwardButton != null && !shiftBackwardButton.gameObject.activeSelf)
            shiftBackwardButton.gameObject.SetActive(true);

        if (shiftForwardButton != null && !shiftForwardButton.gameObject.activeSelf)
            shiftForwardButton.gameObject.SetActive(true);

        UpdateWheelPointerVisual();
    }

    private void SetShiftControlsInteractable(bool interactable)
    {
        if (shiftBackwardButton != null)
            shiftBackwardButton.interactable = interactable;

        if (shiftForwardButton != null)
            shiftForwardButton.interactable = interactable;
    }

    private void ApplySavedProgress()
    {
        suppressProgressSave = true;
        currentPreviewShift = 0;
        partialDecodeComplete = false;
        savedAnswer = string.Empty;

        if (saveSystem == null || puzzleData == null || string.IsNullOrWhiteSpace(puzzleData.PuzzleKey))
        {
            suppressProgressSave = false;
            return;
        }

        List<string> savedValues = saveSystem.GetPuzzleProgress(puzzleData.PuzzleKey);
        for (int i = 0; i < savedValues.Count; i++)
        {
            string value = savedValues[i];
            if (string.IsNullOrEmpty(value))
                continue;

            if (value.StartsWith(SavedAnswerPrefix))
            {
                savedAnswer = value.Substring(SavedAnswerPrefix.Length);
                continue;
            }

            if (value.StartsWith(SavedShiftPrefix))
            {
                string shiftText = value.Substring(SavedShiftPrefix.Length);
                if (int.TryParse(shiftText, out int savedShift))
                    currentPreviewShift = Mathf.Abs(savedShift % 26);

                continue;
            }

            if (value.StartsWith(SavedPartialPrefix))
            {
                string partialText = value.Substring(SavedPartialPrefix.Length);
                if (bool.TryParse(partialText, out bool savedPartial))
                    partialDecodeComplete = savedPartial;
            }
        }

        suppressProgressSave = false;
    }

    private void PersistProgress()
    {
        if (suppressProgressSave || solved || saveSystem == null || puzzleData == null || string.IsNullOrWhiteSpace(puzzleData.PuzzleKey))
            return;

        saveSystem.SavePuzzleProgress(
            puzzleData.PuzzleKey,
            new[]
            {
                $"{SavedAnswerPrefix}{GetCurrentAnswerString()}",
                $"{SavedShiftPrefix}{currentPreviewShift}",
                $"{SavedPartialPrefix}{partialDecodeComplete}"
            });
    }

    private void ClearSavedProgress()
    {
        if (saveSystem == null || puzzleData == null || string.IsNullOrWhiteSpace(puzzleData.PuzzleKey))
            return;

        saveSystem.ClearPuzzleProgress(puzzleData.PuzzleKey);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private string GetVisiblePlaintext()
    {
        if (puzzleData == null)
            return string.Empty;

        string plaintext = puzzleData.Plaintext;
        if (ShouldRevealMaskedPhrase())
            return plaintext;

        return ReplaceIgnoreCase(plaintext, maskedDecodedPhrase, maskedPhrasePlaceholder);
    }

    private bool ShouldRevealMaskedPhrase()
    {
        if (!IsRequiredWordSearchSolved())
            return false;

        if (CanRevealFinalMessage())
            return true;

        if (saveSystem == null)
            return false;

        if (!string.IsNullOrWhiteSpace(revealMaskedPhraseAfterPuzzleKey) &&
            saveSystem.IsPuzzleSolved(revealMaskedPhraseAfterPuzzleKey))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(revealMaskedPhraseAfterLegacyPuzzleKey) &&
               saveSystem.IsPuzzleSolved(revealMaskedPhraseAfterLegacyPuzzleKey);
    }

    private void ResolvePuzzleInteractableReference()
    {
        if (puzzleInteractable != null)
            return;

        InteractableObject[] interactables = FindObjectsByType<InteractableObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null || !interactable.isPuzzle)
            {
                continue;
            }

            if (interactable.puzzleUI == decodeUIPanel ||
                (!string.IsNullOrEmpty(puzzleID) && interactable.puzzleID == puzzleID))
            {
                puzzleInteractable = interactable;
                break;
            }
        }
    }

    private bool HasFinalRevealBeenCompleted()
    {
        if (wasSolved)
            return true;

        if (saveSystem == null)
            return false;

        if (puzzleData != null &&
            !string.IsNullOrWhiteSpace(puzzleData.PuzzleKey) &&
            saveSystem.IsPuzzleSolved(puzzleData.PuzzleKey))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(legacyPuzzleID) &&
               saveSystem.IsPuzzleSolved(legacyPuzzleID);
    }

    private bool CanRevealFinalMessage()
    {
        if (!IsRequiredWordSearchSolved())
            return false;

        if (LibraryPuzzleStateBridge.Instance != null && LibraryPuzzleStateBridge.Instance.CanFinalize())
            return true;

        if (!partialDecodeComplete)
            return false;

        if (LibraryPuzzleStateBridge.Instance != null && LibraryPuzzleStateBridge.Instance.paintingSolved)
            return true;

        if (saveSystem != null && saveSystem.IsPuzzleSolved("PaintingPuzzle"))
            return true;

        if (saveSystem != null &&
            !string.IsNullOrWhiteSpace(revealMaskedPhraseAfterPuzzleKey) &&
            saveSystem.IsPuzzleSolved(revealMaskedPhraseAfterPuzzleKey))
        {
            return true;
        }

        return saveSystem != null &&
               !string.IsNullOrWhiteSpace(revealMaskedPhraseAfterLegacyPuzzleKey) &&
               saveSystem.IsPuzzleSolved(revealMaskedPhraseAfterLegacyPuzzleKey);
    }

    private bool IsRequiredWordSearchSolved()
    {
        return string.IsNullOrWhiteSpace(requiredWordSearchPuzzleKey) ||
               saveSystem == null ||
               saveSystem.IsPuzzleSolved(requiredWordSearchPuzzleKey);
    }

    private string BuildAnswerTarget()
    {
        return CaesarCipher.NormalizeForCompare(GetVisiblePlaintext());
    }

    private string BuildFormattedVisiblePlaintext()
    {
        List<List<AnswerTokenLayout>> rows = BuildAnswerTokenRows();
        var rowStrings = new List<string>(rows.Count);

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var tokenTexts = new List<string>(rows[rowIndex].Count);
            for (int tokenIndex = 0; tokenIndex < rows[rowIndex].Count; tokenIndex++)
                tokenTexts.Add(rows[rowIndex][tokenIndex].DisplayToken);

            rowStrings.Add(string.Join(" ", tokenTexts));
        }

        return string.Join("\n", rowStrings);
    }

    private string GetDisplayedEncodedText()
    {
        if (puzzleData == null)
            return string.Empty;

        if (solved)
            return GetVisiblePlaintext();

        if (!ShouldRevealMaskedPhrase() && !string.IsNullOrWhiteSpace(maskedEncodedTextOverride))
            return maskedEncodedTextOverride;

        return CaesarCipher.Shift(BuildFormattedVisiblePlaintext(), puzzleData.Shift);
    }

    private List<List<AnswerTokenLayout>> BuildAnswerTokenRows()
    {
        string visiblePlaintext = GetVisiblePlaintext();
        string[] rawTokens = visiblePlaintext.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<List<AnswerTokenLayout>>();

        if (rawTokens.Length == 0)
            return rows;

        if (TryBuildConfiguredRows(rawTokens, rows))
            return rows;

        var currentRow = new List<AnswerTokenLayout>();
        int currentRowDisplayLength = 0;

        for (int i = 0; i < rawTokens.Length; i++)
        {
            string displayToken = rawTokens[i];
            string normalizedToken = CaesarCipher.NormalizeForCompare(displayToken);
            int displayLength = displayToken.Length;
            int answerLength = normalizedToken.Replace(" ", string.Empty).Length;

            int additionalLength = displayLength + (currentRow.Count > 0 ? 1 : 0);
            if (currentRow.Count > 0 && currentRowDisplayLength + additionalLength > maxLettersPerRow)
            {
                rows.Add(currentRow);
                currentRow = new List<AnswerTokenLayout>();
                currentRowDisplayLength = 0;
            }

            currentRow.Add(new AnswerTokenLayout(displayToken, displayLength, answerLength));
            currentRowDisplayLength += displayLength + (currentRow.Count > 1 ? 1 : 0);
        }

        if (currentRow.Count > 0)
            rows.Add(currentRow);

        return rows;
    }

    private bool TryBuildConfiguredRows(string[] rawTokens, List<List<AnswerTokenLayout>> rows)
    {
        if (wordsPerRow == null || wordsPerRow.Length == 0)
            return false;

        int configuredWordTotal = 0;
        for (int i = 0; i < wordsPerRow.Length; i++)
            configuredWordTotal += Mathf.Max(0, wordsPerRow[i]);

        if (configuredWordTotal != rawTokens.Length)
            return false;

        int tokenIndex = 0;
        for (int rowIndex = 0; rowIndex < wordsPerRow.Length; rowIndex++)
        {
            int wordsInRow = Mathf.Max(0, wordsPerRow[rowIndex]);
            var row = new List<AnswerTokenLayout>(wordsInRow);

            for (int i = 0; i < wordsInRow; i++, tokenIndex++)
            {
                string displayToken = rawTokens[tokenIndex];
                string normalizedToken = CaesarCipher.NormalizeForCompare(displayToken);
                int displayLength = displayToken.Length;
                int answerLength = normalizedToken.Replace(" ", string.Empty).Length;
                row.Add(new AnswerTokenLayout(displayToken, displayLength, answerLength));
            }

            if (row.Count > 0)
                rows.Add(row);
        }

        return rows.Count > 0;
    }

    private void SyncAnswerSlotsFromHierarchy()
    {
        if (answerSlotsRoot == null)
            answerSlotsRoot = FindAnswerSlotsRoot();

        if (answerInput != null)
            answerInput.gameObject.SetActive(false);

        answerSlots.Clear();
        answerSlotTextComponents.Clear();
        answerSlotVisibleColors.Clear();
        wordLengths.Clear();

        if (answerSlotsRoot == null)
            return;

        for (int rowIndex = 0; rowIndex < answerSlotsRoot.childCount; rowIndex++)
        {
            Transform row = answerSlotsRoot.GetChild(rowIndex);
            for (int tokenIndex = 0; tokenIndex < row.childCount; tokenIndex++)
            {
                Transform token = row.GetChild(tokenIndex);
                TMP_InputField[] tokenSlots = token.GetComponentsInChildren<TMP_InputField>(true);
                if (tokenSlots == null || tokenSlots.Length == 0)
                {
                    ApplyStaticTokenPresentation(token);
                    continue;
                }

                wordLengths.Add(tokenSlots.Length);
                for (int slotIndex = 0; slotIndex < tokenSlots.Length; slotIndex++)
                {
                    TMP_InputField slot = tokenSlots[slotIndex];
                    ApplyAnswerSlotPresentation(slot);
                    slot.onValueChanged.RemoveAllListeners();
                    answerSlots.Add(slot);

                    if (slot.textComponent != null)
                    {
                        answerSlotTextComponents[slot] = slot.textComponent;
                        answerSlotVisibleColors[slot] = slot.textComponent.color;
                    }
                }
            }
        }

        for (int i = 0; i < answerSlots.Count; i++)
        {
            TMP_InputField slot = answerSlots[i];
            int slotIndex = i;
            slot.onValueChanged.AddListener(value => HandleSlotValueChanged(slotIndex, value));
        }

        SetAnswerFromString(savedAnswer);
    }

    private void ApplyStaticTokenPresentation(Transform token)
    {
        TMP_Text[] tokenTexts = token.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tokenTexts.Length; i++)
        {
            TMP_Text text = tokenTexts[i];
            if (text == null)
                continue;

            text.fontSize = staticTokenFontSize;
            text.color = staticTokenFontColor;
        }
    }

    private void ApplyAnswerSlotPresentation(TMP_InputField slot)
    {
        if (slot == null)
            return;

        slot.characterLimit = 1;
        slot.contentType = TMP_InputField.ContentType.Alphanumeric;
        slot.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        slot.lineType = TMP_InputField.LineType.SingleLine;

        if (slot.textComponent != null)
        {
            slot.textComponent.alignment = TextAlignmentOptions.Center;
            slot.textComponent.fontSize = 24f;
        }

        if (slot.placeholder is TMP_Text placeholderText)
            placeholderText.text = string.Empty;

        Image background = slot.GetComponent<Image>();
        if (background != null)
            background.color = slotBackgroundColor;
    }

    private RectTransform FindAnswerSlotsRoot()
    {
        if (answerSlotsRoot != null)
            return answerSlotsRoot;

        if (answerInput == null)
            return null;

        RectTransform templateRect = answerInput.transform as RectTransform;
        RectTransform parent = templateRect != null ? templateRect.parent as RectTransform : null;
        if (parent == null)
            return null;

        Transform existing = parent.Find("AnswerSlotsRoot");
        return existing as RectTransform;
    }

    private TMP_InputField CreateAnswerSlot(RectTransform parent, int wordIndex, int letterIndex)
    {
        GameObject slotObject = Instantiate(answerInput.gameObject, parent);
        slotObject.name = $"AnswerSlot_{wordIndex}_{letterIndex}";
        slotObject.SetActive(true);

        RectTransform rect = slotObject.transform as RectTransform;
        if (rect != null)
            rect.sizeDelta = slotSize;

        TMP_InputField slot = slotObject.GetComponent<TMP_InputField>();
        slot.text = string.Empty;
        slot.characterLimit = 1;
        slot.contentType = TMP_InputField.ContentType.Alphanumeric;
        slot.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        slot.lineType = TMP_InputField.LineType.SingleLine;
        slot.onValueChanged.RemoveAllListeners();
        slot.onEndEdit.RemoveAllListeners();
        slot.onSelect.RemoveAllListeners();

        if (slot.textComponent != null)
        {
            slot.textComponent.alignment = TextAlignmentOptions.Center;
            slot.textComponent.fontSize = 24f;
            slot.textComponent.text = string.Empty;
        }

        if (slot.placeholder is TMP_Text placeholderText)
            placeholderText.text = string.Empty;

        Image background = slotObject.GetComponent<Image>();
        if (background != null)
            background.color = slotBackgroundColor;

        int slotIndex = answerSlots.Count;
        slot.onValueChanged.AddListener(value => HandleSlotValueChanged(slotIndex, value));
        return slot;
    }

    private void ApplyEncodedTextPresentation()
    {
        if (encodedText == null)
            return;

        encodedText.lineSpacing = encodedLineSpacing;
    }

    private void HandleSlotValueChanged(int slotIndex, string value)
    {
        if (suppressSlotCallbacks || slotIndex < 0 || slotIndex >= answerSlots.Count)
            return;

        TMP_InputField slot = answerSlots[slotIndex];
        string sanitized = SanitizeSlotValue(value);
        if (!string.Equals(slot.text, sanitized, StringComparison.Ordinal))
        {
            suppressSlotCallbacks = true;
            slot.text = sanitized;
            suppressSlotCallbacks = false;
        }

        if (!string.IsNullOrEmpty(sanitized))
        {
            int nextIndex = slotIndex + 1;
            if (nextIndex < answerSlots.Count)
                answerSlots[nextIndex].ActivateInputField();
        }

        PersistProgress();
    }

    private static string SanitizeSlotValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                return char.ToUpperInvariant(c).ToString();
        }

        return string.Empty;
    }

    private void SetAnswerFromString(string answer)
    {
        suppressSlotCallbacks = true;

        string normalized = CaesarCipher.NormalizeForCompare(answer);
        string[] words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int slotIndex = 0;

        for (int wordIndex = 0; wordIndex < wordLengths.Count; wordIndex++)
        {
            string word = wordIndex < words.Length ? words[wordIndex] : string.Empty;
            for (int charIndex = 0; charIndex < wordLengths[wordIndex] && slotIndex < answerSlots.Count; charIndex++, slotIndex++)
            {
                answerSlots[slotIndex].text = charIndex < word.Length
                    ? char.ToUpperInvariant(word[charIndex]).ToString()
                    : string.Empty;
            }
        }

        while (slotIndex < answerSlots.Count)
        {
            answerSlots[slotIndex].text = string.Empty;
            slotIndex++;
        }

        suppressSlotCallbacks = false;
    }

    private string GetCurrentAnswerString()
    {
        if (answerSlots.Count == 0 || wordLengths.Count == 0)
            return string.Empty;

        var words = new List<string>(wordLengths.Count);
        int slotIndex = 0;

        for (int wordIndex = 0; wordIndex < wordLengths.Count; wordIndex++)
        {
            char[] chars = new char[wordLengths[wordIndex]];
            for (int charIndex = 0; charIndex < wordLengths[wordIndex] && slotIndex < answerSlots.Count; charIndex++, slotIndex++)
            {
                string value = answerSlots[slotIndex].text;
                chars[charIndex] = string.IsNullOrEmpty(value) ? ' ' : value[0];
            }

            words.Add(new string(chars).TrimEnd());
        }

        return string.Join(" ", words).Trim();
    }

    private void SetAnswerSlotsInteractable(bool interactable)
    {
        for (int i = 0; i < answerSlots.Count; i++)
            answerSlots[i].interactable = interactable;
    }

    private void SetAnswerSlotsRootVisible(bool visible)
    {
        if (answerSlotsRoot == null)
            answerSlotsRoot = FindAnswerSlotsRoot();

        if (answerSlotsRoot != null)
            answerSlotsRoot.gameObject.SetActive(visible);
    }

    private void SetEncodedWritingVisible(bool visible)
    {
        if (encodedText != null)
            encodedText.gameObject.SetActive(visible);
    }

    private void SetWheelVisualVisible(bool visible)
    {
        if (wheelVisualRoot != null)
            wheelVisualRoot.gameObject.SetActive(visible);
    }

    private void SetAnswerSlotTextVisible(bool visible)
    {
        for (int i = 0; i < answerSlots.Count; i++)
        {
            TMP_InputField slot = answerSlots[i];
            if (slot == null)
                continue;

            if (!answerSlotTextComponents.TryGetValue(slot, out TMP_Text text) || text == null)
                continue;

            Color visibleColor = answerSlotVisibleColors.TryGetValue(slot, out Color storedColor)
                ? storedColor
                : text.color;

            text.color = visible ? visibleColor : hiddenSlotTextColor;
        }
    }

    private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
            return source;

        int matchIndex = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
            return source;

        return source.Remove(matchIndex, oldValue.Length).Insert(matchIndex, newValue ?? string.Empty);
    }

    private void TryPlayDialogue(DialogueAsset dialogue, bool immediate)
    {
        if (dialogue == null || DialogueSystem.Instance == null)
            return;

        if (saveSystem != null &&
            !string.IsNullOrWhiteSpace(dialogue.dialogueID) &&
            saveSystem.HasViewedDialogue(dialogue.dialogueID))
        {
            return;
        }

        if (immediate)
            DialogueSystem.Instance.StartDialogue(dialogue);
        else
            DialogueSystem.Instance.QueueDialogue(dialogue);
    }

    private void PlayWordSearchRequiredDialogue()
    {
        if (onWordSearchRequiredDialogue == null || DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.StartDialogue(onWordSearchRequiredDialogue);
    }

    private void ShowConfiguredFinalTidbit()
    {
        UICluePopup popup = ResolveTidbitPopup();
        if (popup == null || puzzleInteractable == null)
            return;

        if (puzzleInteractable.informationalTidbit != null)
        {
            popup.ShowTidbit(puzzleInteractable.informationalTidbit);
            return;
        }

        if (!string.IsNullOrWhiteSpace(puzzleInteractable.tidbitMessage))
        {
            popup.ShowTidbitMessage(puzzleInteractable.tidbitMessage);
        }
    }

    private UICluePopup ResolveTidbitPopup()
    {
        UICluePopup popup = null;

        if (puzzleInteractable != null && puzzleInteractable.tidbitPopupCanvas != null)
        {
            popup = puzzleInteractable.tidbitPopupCanvas.GetComponent<UICluePopup>();

            if (popup == null)
            {
                popup = puzzleInteractable.tidbitPopupCanvas.GetComponentInChildren<UICluePopup>(true);
            }
        }

        if (popup == null)
        {
            popup = FindFirstObjectByType<UICluePopup>(FindObjectsInactive.Include);
        }

        if (popup == null)
        {
            GameObject popupPrefab = Resources.Load<GameObject>("PopupCanvas");
            if (popupPrefab != null)
            {
                GameObject popupInstance = Instantiate(popupPrefab);
                popup = popupInstance.GetComponent<UICluePopup>();

                if (popup == null)
                {
                    popup = popupInstance.GetComponentInChildren<UICluePopup>(true);
                }
            }
        }

        return popup;
    }

    private void ResolveWheelVisualReferences()
    {
        if (wheelVisualRoot == null)
        {
            Transform paperBackground = transform.Find("Paper Background");
            if (paperBackground != null)
                wheelVisualRoot = paperBackground as RectTransform;
        }

        if (wheelVisualRoot == null)
            return;

        if (rotatingInnerWheel == null)
        {
            Transform innerWheel = wheelVisualRoot.Find("Inner wheel");
            if (innerWheel == null)
                innerWheel = wheelVisualRoot.Find("Inner Wheel");

            if (innerWheel != null)
                rotatingInnerWheel = innerWheel as RectTransform;
        }

    }

    private void UpdateWheelPointerVisual()
    {
        float degreesPerShift = 360f / 26f;
        float zRotation = wheelAngleOffset - (currentPreviewShift * degreesPerShift);

        if (rotatingInnerWheel != null)
            rotatingInnerWheel.localEulerAngles = new Vector3(0f, 0f, zRotation);
    }

    private IEnumerator PlayFinalDialogueAfterTidbitCloses()
    {
        yield return null;

        UICluePopup popup = FindFirstObjectByType<UICluePopup>(FindObjectsInactive.Include);
        while (popup != null && popup.IsPopupOpen())
        {
            yield return null;
        }

        TryPlayDialogue(onFinalSolveDialogue, true);
    }

    private readonly struct AnswerTokenLayout
    {
        public AnswerTokenLayout(string displayToken, int displayLength, int answerLength)
        {
            DisplayToken = displayToken;
            DisplayLength = displayLength;
            AnswerLength = answerLength;
        }

        public string DisplayToken { get; }
        public int DisplayLength { get; }
        public int AnswerLength { get; }
    }
}
