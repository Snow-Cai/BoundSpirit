using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LibraryWordSearchPanel : MonoBehaviour
{
    private static LibraryWordSearchPanel instance;

    public static bool IsPanelActuallyOpen =>
        instance != null &&
        instance.isActiveAndEnabled &&
        instance.gameObject.activeInHierarchy;

    [Header("Puzzle Identity")]
    [SerializeField] private string puzzleKey = "Library_WordSearch_13";

    [Header("Runtime Dependencies")]
    [SerializeField] private SaveSystem saveSystem;

    [Header("Board Layout")]
    [SerializeField] private string[] boardRows =
    {
        "QWERTYUIOPASDFG",
        "LKJHGFDSAZXCVBN",
        "MNBVCXLIBRARYRY",
        "QWESASBUIOPLCJC",
        "TYUHOHZNMASDLGL",
        "ASDIGIOMQWERUYU",
        "POIFYFKLKJHGEDE",
        "ZXCTBTCIPHERYRM",
        "QAZWSXEDCRFVGAB",
        "MNBBCBZLKJHGHQH",
        "PLMOKOIJBUHVODO",
        "TREOQOKJHGFDSOS",
        "YUIKPLSPIRITTWT",
        "CVBSMSSDFGHJKLZ",
        "XCVBNMQWERTYUIO"
    };

    [Header("Word Paths")]
    [SerializeField] private List<FixedWordPlacement> placements = new()
    {
        new FixedWordPlacement("BOOKS", 9, 3, 1, 0),
        new FixedWordPlacement("SHIFT", 3, 3, 1, 0),
        new FixedWordPlacement("LIBRARY", 2, 6, 0, 1),
        new FixedWordPlacement("CLUE", 3, 12, 1, 0),
        new FixedWordPlacement("CIPHER", 7, 6, 0, 1),
        new FixedWordPlacement("GHOST", 8, 12, 1, 0),
        new FixedWordPlacement("SPIRIT", 12, 6, 0, 1)
    };

    [Header("Optional UI References")]
    [SerializeField] private RectTransform boardContainer;

    [Header("Scene Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text wordsText;

    [Header("Fallback Placeholder Containers")]
    [SerializeField] private RectTransform titlePlaceholder;
    [SerializeField] private RectTransform statusPlaceholder;
    [SerializeField] private RectTransform wordsPlaceholder;

    private WordSearchBoard board;
    private readonly HashSet<string> foundWords = new();
    private readonly Dictionary<Vector2Int, WordSearchCellUI> cellViews = new();
    private readonly List<Color> solvedWordColors = new()
    {
        new(0.96f, 0.61f, 0.61f, 0.92f),
        new(0.98f, 0.79f, 0.49f, 0.92f),
        new(0.95f, 0.91f, 0.52f, 0.92f),
        new(0.63f, 0.85f, 0.60f, 0.92f),
        new(0.55f, 0.80f, 0.90f, 0.92f),
        new(0.67f, 0.67f, 0.93f, 0.92f),
        new(0.84f, 0.63f, 0.89f, 0.92f)
    };

    private GridLayoutGroup gridLayout;
    private bool isDraggingSelection;
    private Vector2Int dragStart;
    private Vector2Int dragEnd;

    private static readonly Color DefaultCellColor = new(1f, 1f, 1f, 0.92f);
    private static readonly Color PreviewCellColor = new(0.98f, 0.88f, 0.54f, 0.9f);
    private static readonly Color InkColor = new(0.22f, 0.16f, 0.08f, 1f);

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        ResolveDependencies();
        EnsureRuntimeUi();
        BuildBoard();
        ApplySavedState();
        RefreshTexts();
        RefreshBoardVisuals();
    }

    private void OnDisable()
    {
        PersistProgress();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void OnWordSearchCellPointerDown(int row, int column)
    {
        if (IsSolved())
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        isDraggingSelection = true;
        dragStart = new Vector2Int(row, column);
        dragEnd = dragStart;
        RefreshBoardVisuals();
    }

    public void OnWordSearchCellPointerEnter(int row, int column)
    {
        if (!isDraggingSelection)
            return;

        dragEnd = new Vector2Int(row, column);
        RefreshBoardVisuals();
    }

    public void OnWordSearchCellPointerUp(int row, int column)
    {
        if (!isDraggingSelection)
            return;

        TryCommitSelection();
        isDraggingSelection = false;
        RefreshBoardVisuals();
    }

    private void Close()
    {
        PersistProgress();
        gameObject.SetActive(false);
    }

    private void ResolveDependencies()
    {
        if (saveSystem == null)
            saveSystem = FindFirstObjectByType<SaveSystem>();
    }

    private void EnsureRuntimeUi()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        TMP_FontAsset font = FindAnyObjectByType<TMP_Text>()?.font;
        if (font == null)
            font = TMP_Settings.defaultFontAsset;

        if (boardContainer == null)
            boardContainer = CreatePanel("Board", root, new Vector2(460f, 460f), new Vector2(-145f, 8f));

        if (titlePlaceholder == null)
            titlePlaceholder = CreatePlaceholder("TitlePlaceholder", root, new Vector2(230f, 60f), new Vector2(222f, 206f));

        if (statusPlaceholder == null)
            statusPlaceholder = CreatePlaceholder("StatusPlaceholder", root, new Vector2(230f, 60f), new Vector2(222f, 132f));

        if (wordsPlaceholder == null)
            wordsPlaceholder = CreatePlaceholder("WordsPlaceholder", root, new Vector2(230f, 130f), new Vector2(222f, 36f));

        if (gridLayout == null)
        {
            gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
                gridLayout = boardContainer.gameObject.AddComponent<GridLayoutGroup>();

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = boardRows.Length;
            gridLayout.spacing = new Vector2(3f, 3f);
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
        }

        titleText = ResolveTextReference(titleText, titlePlaceholder, "Title", font, 28f, TextAlignmentOptions.TopLeft);
        statusText = ResolveTextReference(statusText, statusPlaceholder, "Status", font, 18f, TextAlignmentOptions.TopLeft);
        wordsText = ResolveTextReference(wordsText, wordsPlaceholder, "Words", font, 18f, TextAlignmentOptions.TopLeft);

        if (wordsText != null)
        {
            wordsText.enableWordWrapping = true;
            wordsText.richText = true;
        }
    }

    private void BuildBoard()
    {
        var boardPlacements = new List<WordSearchPlacement>(placements.Count);
        for (int i = 0; i < placements.Count; i++)
        {
            FixedWordPlacement placement = placements[i];
            boardPlacements.Add(new WordSearchPlacement(
                placement.Word,
                new Vector2Int(placement.StartRow, placement.StartColumn),
                new Vector2Int(placement.RowDirection, placement.ColumnDirection)));
        }

        board = WordSearchBoard.CreateFixed(boardRows, boardPlacements);
        gridLayout.constraintCount = board.Size;
        RebuildCells();
    }

    private void RebuildCells()
    {
        cellViews.Clear();

        for (int i = boardContainer.childCount - 1; i >= 0; i--)
            Destroy(boardContainer.GetChild(i).gameObject);

        float cellSize = Mathf.Floor((boardContainer.rect.width - ((board.Size - 1) * gridLayout.spacing.x)) / board.Size);
        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        TMP_FontAsset font = titleText != null ? titleText.font : FindAnyObjectByType<TMP_Text>()?.font;

        for (int row = 0; row < board.Size; row++)
        {
            for (int column = 0; column < board.Size; column++)
            {
                GameObject cell = new($"Cell_{row}_{column}", typeof(RectTransform), typeof(Image), typeof(WordSearchCellUI));
                cell.transform.SetParent(boardContainer, false);

                Image background = cell.GetComponent<Image>();
                background.color = DefaultCellColor;

                GameObject letter = new("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
                letter.transform.SetParent(cell.transform, false);

                RectTransform letterRect = letter.GetComponent<RectTransform>();
                letterRect.anchorMin = Vector2.zero;
                letterRect.anchorMax = Vector2.one;
                letterRect.offsetMin = Vector2.zero;
                letterRect.offsetMax = Vector2.zero;

                TMP_Text label = letter.GetComponent<TMP_Text>();
                label.font = font;
                label.fontSize = 24f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = InkColor;
                label.text = board.GetLetter(row, column).ToString();
                label.raycastTarget = false;

                WordSearchCellUI cellUi = cell.GetComponent<WordSearchCellUI>();
                cellUi.Initialize(
                    row,
                    column,
                    label,
                    background,
                    OnWordSearchCellPointerDown,
                    OnWordSearchCellPointerEnter,
                    OnWordSearchCellPointerUp);

                cellViews[new Vector2Int(row, column)] = cellUi;
            }
        }
    }

    private void ApplySavedState()
    {
        foundWords.Clear();

        if (IsSolved())
        {
            foreach (WordSearchPlacement placement in board.Placements)
                foundWords.Add(placement.Word);

            return;
        }

        if (saveSystem == null)
            return;

        List<string> savedWords = saveSystem.GetPuzzleProgress(puzzleKey);
        for (int i = 0; i < savedWords.Count; i++)
        {
            string savedWord = savedWords[i];
            if (string.IsNullOrWhiteSpace(savedWord))
                continue;

            for (int placementIndex = 0; placementIndex < board.Placements.Count; placementIndex++)
            {
                if (board.Placements[placementIndex].Word == savedWord)
                {
                    foundWords.Add(savedWord);
                    break;
                }
            }
        }
    }

    private void TryCommitSelection()
    {
        if (!board.TryGetMatchedWord(
                dragStart.x,
                dragStart.y,
                dragEnd.x,
                dragEnd.y,
                foundWords,
                out string matchedWord,
                out _))
        {
            statusText.text = "That line is not one of the 7 words.";
            return;
        }

        foundWords.Add(matchedWord);
        PersistProgress();

        if (foundWords.Count >= board.Placements.Count)
        {
            CompletePuzzle();
            return;
        }

        RefreshTexts();
    }

    private void CompletePuzzle()
    {
        bool firstSolve = !IsSolved();

        if (!firstSolve)
        {
            RefreshTexts();
            return;
        }

        InteractableObject puzzleSource = PuzzleBridge.currentPuzzleSource;
        if (puzzleSource != null)
        {
            if (saveSystem != null)
                saveSystem.ClearPuzzleProgress(puzzleKey);

            puzzleSource.OnPuzzleSolved();
            RefreshTexts();
            return;
        }

        if (saveSystem != null)
        {
            saveSystem.ClearPuzzleProgress(puzzleKey);
            saveSystem.UnlockPuzzle(puzzleKey);
        }

        RefreshTexts();
    }

    private void PersistProgress()
    {
        if (saveSystem == null || IsSolved())
            return;

        saveSystem.SavePuzzleProgress(puzzleKey, foundWords);
    }

    private void RefreshTexts()
    {
        if (wordsText != null)
            wordsText.text = BuildWordListText();

        if (IsSolved())
        {
            if (statusText != null)
                statusText.text = "All 7 words found.";
        }
        else
        {
            if (statusText != null)
                statusText.text = $"Find all 7 words. {foundWords.Count}/{board.Placements.Count} found.";
        }
    }

    private string BuildWordListText()
    {
        const int firstLineCount = 6;
        var firstLine = new List<string>();
        var secondLine = new List<string>();

        for (int i = 0; i < board.Placements.Count; i++)
        {
            string label = FormatWordLabel(board.Placements[i].Word);
            if (i < firstLineCount)
                firstLine.Add(label);
            else
                secondLine.Add(label);
        }

        if (secondLine.Count == 0)
            return string.Join("    ", firstLine);

        return string.Join("    ", firstLine) + "\n" + string.Join("    ", secondLine);
    }

    private string FormatWordLabel(string word)
    {
        if (!foundWords.Contains(word))
            return word;

        return $"<s><color=#6E6254>{word}</color></s>";
    }

    private void RefreshBoardVisuals()
    {
        foreach (KeyValuePair<Vector2Int, WordSearchCellUI> pair in cellViews)
            pair.Value.Background.color = DefaultCellColor;

        int colorIndex = 0;
        foreach (WordSearchPlacement placement in board.Placements)
        {
            if (!foundWords.Contains(placement.Word))
                continue;

            List<Vector2Int> lockedPath = board.BuildPath(placement);
            Color solvedColor = solvedWordColors[colorIndex % solvedWordColors.Count];
            colorIndex++;

            for (int i = 0; i < lockedPath.Count; i++)
            {
                if (cellViews.TryGetValue(lockedPath[i], out WordSearchCellUI cell))
                    cell.Background.color = solvedColor;
            }
        }

        if (!isDraggingSelection || IsSolved())
            return;

        if (!TryGetPreviewPath(dragStart, dragEnd, out List<Vector2Int> previewPath))
            return;

        for (int i = 0; i < previewPath.Count; i++)
        {
            if (cellViews.TryGetValue(previewPath[i], out WordSearchCellUI cell) &&
                cell.Background.color == DefaultCellColor)
            {
                cell.Background.color = PreviewCellColor;
            }
        }
    }

    private bool IsSolved()
    {
        return saveSystem != null && saveSystem.IsPuzzleSolved(puzzleKey);
    }

    private static bool TryGetPreviewPath(Vector2Int start, Vector2Int end, out List<Vector2Int> path)
    {
        path = null;

        int rowDelta = end.x - start.x;
        int columnDelta = end.y - start.y;

        int stepRow = rowDelta == 0 ? 0 : rowDelta / Mathf.Abs(rowDelta);
        int stepColumn = columnDelta == 0 ? 0 : columnDelta / Mathf.Abs(columnDelta);

        bool isHorizontal = rowDelta == 0 && columnDelta != 0;
        bool isVertical = columnDelta == 0 && rowDelta != 0;
        bool isDiagonal = Mathf.Abs(rowDelta) == Mathf.Abs(columnDelta) && rowDelta != 0;
        bool isSingleCell = rowDelta == 0 && columnDelta == 0;

        if (!isHorizontal && !isVertical && !isDiagonal && !isSingleCell)
            return false;

        int steps = Mathf.Max(Mathf.Abs(rowDelta), Mathf.Abs(columnDelta));
        path = new List<Vector2Int>(steps + 1);

        for (int i = 0; i <= steps; i++)
            path.Add(new Vector2Int(start.x + (stepRow * i), start.y + (stepColumn * i)));

        return true;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.88f, 0.80f, 0.68f, 0.65f);

        return rect;
    }

    private static RectTransform CreatePlaceholder(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject placeholder = new(name, typeof(RectTransform));
        placeholder.transform.SetParent(parent, false);

        RectTransform rect = placeholder.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        return rect;
    }

    private static TMP_Text ResolveTextReference(
        TMP_Text existingText,
        RectTransform placeholder,
        string childName,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (existingText != null)
        {
            PrepareExistingText(existingText, font);
            return existingText;
        }

        return EnsurePlaceholderText(placeholder, childName, font, fontSize, alignment);
    }

    private static TMP_Text EnsurePlaceholderText(
        RectTransform placeholder,
        string childName,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (placeholder == null)
            return null;

        if (!placeholder.gameObject.activeSelf)
            placeholder.gameObject.SetActive(true);

        TMP_Text existing = placeholder.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
        {
            PrepareExistingText(existing, font);
            return existing;
        }

        GameObject textObject = new(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(placeholder, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        ApplyTextStyle(text, font, fontSize, alignment);
        text.text = string.Empty;

        return text;
    }

    private static void ApplyTextStyle(
        TMP_Text text,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.font = font;
        text.fontSize = fontSize;
        text.color = InkColor;
        text.alignment = alignment;
        text.raycastTarget = false;
    }

    private static void PrepareExistingText(TMP_Text text, TMP_FontAsset fallbackFont)
    {
        if (text == null)
            return;

        if (text.font == null)
            text.font = fallbackFont;

        text.raycastTarget = false;
    }

    [System.Serializable]
    private sealed class FixedWordPlacement
    {
        public FixedWordPlacement(string word, int startRow, int startColumn, int rowDirection, int columnDirection)
        {
            Word = word;
            StartRow = startRow;
            StartColumn = startColumn;
            RowDirection = rowDirection;
            ColumnDirection = columnDirection;
        }

        public string Word;
        public int StartRow;
        public int StartColumn;
        public int RowDirection;
        public int ColumnDirection;
    }
}
