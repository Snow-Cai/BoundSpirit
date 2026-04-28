using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GatePuzzleController : MonoBehaviour
{
    [Header("Gate Link")]
    [SerializeField] private GraveyardGateController gateController;

    [Header("Rune Slots (Legacy Text)")]
    [SerializeField] private TextMeshProUGUI[] runeTexts;

    [Header("Rune Images")]
    [SerializeField] private Image[] runeImages;

    [Header("Symbol Options")]
    [SerializeField] private string[] symbolOptions = { "Circle", "Triangle", "Square" };
    [SerializeField] private Sprite[] symbolSprites;

    [Header("Correct Pattern (Single Stage)")]
    [Tooltip("Indices into the symbol options, length must match the rune slot count.")]
    [SerializeField] private int[] correctPattern = { 0, 1, 2 }; // circle, triangle, square

    [Header("Feedback")]
    [SerializeField] private DialogueAsset wrongPatternDialogue;
    [SerializeField] private DialogueAsset puzzleCompleteDialogue;

    private int[] currentPattern;
    private bool waitingForCompletionDialogue;

    private void Awake()
    {
        ResolveRuneImages();

        int runeCount = GetRuneSlotCount();
        if (runeCount == 0)
        {
            return;
        }

        if (correctPattern == null || correctPattern.Length != runeCount)
        {
            Debug.LogWarning("GatePuzzleController: correctPattern must match rune slot count.");
        }

        currentPattern = new int[runeCount];

        for (int i = 0; i < currentPattern.Length; i++)
        {
            currentPattern[i] = 0;
        }

        RefreshRunes();
    }

    private void OnDisable()
    {
        UnsubscribeFromDialogueEnded();
        waitingForCompletionDialogue = false;
    }

    private void OnDestroy()
    {
        UnsubscribeFromDialogueEnded();
    }

    public void CycleRune(int index)
    {
        if (gateController != null && !gateController.CanUseGatePuzzleRunesAndSubmit())
        {
            return;
        }

        gateController?.OnGatePuzzleInputStarted();

        int optionCount = GetOptionCount();
        if (GetRuneSlotCount() == 0 ||
            optionCount == 0 ||
            currentPattern == null)
        {
            return;
        }

        if (index < 0 || index >= currentPattern.Length)
        {
            Debug.LogWarning("GatePuzzleController: CycleRune index out of range.");
            return;
        }

        currentPattern[index] = (currentPattern[index] + 1) % optionCount;
        RefreshRunes();
    }

    public void Confirm()
    {
        if (gateController != null && !gateController.CanUseGatePuzzleRunesAndSubmit())
        {
            return;
        }

        gateController?.OnGatePuzzleInputStarted();

        if (!IsPatternConfigured())
        {
            Debug.LogWarning("GatePuzzleController: Pattern not configured correctly.");
            return;
        }

        if (!IsPatternCorrect())
        {
            HandleIncorrectPattern();
            return;
        }

        HandleCorrectPattern();
    }

    private void RefreshRunes()
    {
        bool usingSprites = symbolSprites != null && symbolSprites.Length > 0;
        bool usingText = symbolOptions != null && symbolOptions.Length > 0;

        if ((!usingSprites && !usingText) || currentPattern == null)
        {
            return;
        }

        if (usingSprites && runeImages != null)
        {
            for (int i = 0; i < runeImages.Length; i++)
            {
                if (runeImages[i] == null)
                    continue;

                int symbolIndex = Mathf.Clamp(currentPattern[i], 0, symbolSprites.Length - 1);
                runeImages[i].sprite = symbolSprites[symbolIndex];
                runeImages[i].color = Color.white;
                runeImages[i].preserveAspect = true;
            }
        }

        if (runeTexts == null)
            return;

        for (int i = 0; i < runeTexts.Length; i++)
        {
            if (runeTexts[i] == null)
                continue;

            if (usingText)
            {
                int symbolIndex = Mathf.Clamp(currentPattern[i], 0, symbolOptions.Length - 1);
                runeTexts[i].text = symbolOptions[symbolIndex];
            }

            runeTexts[i].enabled = !usingSprites;
        }
    }

    private int GetRuneSlotCount()
    {
        if (runeImages != null && runeImages.Length > 0)
            return runeImages.Length;

        return runeTexts != null ? runeTexts.Length : 0;
    }

    private int GetOptionCount()
    {
        if (symbolSprites != null && symbolSprites.Length > 0)
            return symbolSprites.Length;

        return symbolOptions != null ? symbolOptions.Length : 0;
    }

    private void ResolveRuneImages()
    {
        if (runeImages != null && runeImages.Length > 0)
            return;

        if (runeTexts == null || runeTexts.Length == 0)
            return;

        runeImages = new Image[runeTexts.Length];

        for (int i = 0; i < runeTexts.Length; i++)
        {
            if (runeTexts[i] == null)
                continue;

            runeImages[i] = runeTexts[i].GetComponentInParent<Image>();
        }
    }

    private bool IsPatternConfigured()
    {
        return correctPattern != null &&
               currentPattern != null &&
               correctPattern.Length == currentPattern.Length;
    }

    private bool IsPatternCorrect()
    {
        if (!IsPatternConfigured())
        {
            return false;
        }

        for (int i = 0; i < correctPattern.Length; i++)
        {
            if (currentPattern[i] != correctPattern[i])
            {
                return false;
            }
        }

        return true;
    }

    private void HandleIncorrectPattern()
    {
        if (wrongPatternDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(wrongPatternDialogue);
        }
    }

    private void HandleCorrectPattern()
    {
        if (puzzleCompleteDialogue != null && DialogueSystem.Instance != null)
        {
            waitingForCompletionDialogue = true;
            DialogueSystem.Instance.OnDialogueEnded -= HandlePuzzleCompleteDialogueEnded;
            DialogueSystem.Instance.OnDialogueEnded += HandlePuzzleCompleteDialogueEnded;
            DialogueSystem.Instance.StartDialogue(puzzleCompleteDialogue);
            return;
        }

        CompleteGateUnlock();
    }

    private void HandlePuzzleCompleteDialogueEnded(DialogueAsset finishedDialogue)
    {
        if (!waitingForCompletionDialogue || !DialogueMatches(puzzleCompleteDialogue, finishedDialogue))
            return;

        waitingForCompletionDialogue = false;
        UnsubscribeFromDialogueEnded();
        CompleteGateUnlock();
    }

    private void CompleteGateUnlock()
    {
        if (gateController != null)
        {
            gateController.OnGatePuzzleSolved();
            return;
        }

        gameObject.SetActive(false);
        GameInputState.DialogueActive = false;
    }

    private void UnsubscribeFromDialogueEnded()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded -= HandlePuzzleCompleteDialogueEnded;
        }
    }

    private static bool DialogueMatches(DialogueAsset expected, DialogueAsset finished)
    {
        if (expected == null || finished == null)
            return false;

        if (!string.IsNullOrEmpty(expected.dialogueID) && !string.IsNullOrEmpty(finished.dialogueID))
            return expected.dialogueID == finished.dialogueID;

        return ReferenceEquals(expected, finished);
    }
}
