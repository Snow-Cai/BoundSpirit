using TMPro;
using UnityEngine;

public class GatePuzzleController : MonoBehaviour
{
    [Header("Gate Link")]
    [SerializeField] private GraveyardGateController gateController;

    [Header("Rune Slots")]
    [SerializeField] private TextMeshProUGUI[] runeTexts;

    [Header("Symbol Options")]
    [SerializeField] private string[] symbolOptions = { "●", "▲", "■" }; // Circle, Triangle, Square

    [Header("Correct Pattern (Single Stage)")]
    [Tooltip("Indices into symbolOptions, length must match runeTexts.")]
    [SerializeField] private int[] correctPattern = { 0, 1, 2 }; // ● ▲ ■

    [Header("Feedback")]
    [SerializeField] private DialogueAsset wrongPatternDialogue;
    [SerializeField] private DialogueAsset puzzleCompleteDialogue;

    private int[] currentPattern;
    private bool waitingForCompletionDialogue;

    private void Awake()
    {
        if (runeTexts == null || runeTexts.Length == 0)
        {
            return;
        }

        if (correctPattern == null || correctPattern.Length != runeTexts.Length)
        {
            Debug.LogWarning("GatePuzzleController: correctPattern must match runeTexts length.");
        }

        currentPattern = new int[runeTexts.Length];

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

        if (runeTexts == null ||
            symbolOptions == null ||
            symbolOptions.Length == 0 ||
            currentPattern == null)
        {
            return;
        }

        if (index < 0 || index >= currentPattern.Length)
        {
            Debug.LogWarning("GatePuzzleController: CycleRune index out of range.");
            return;
        }

        currentPattern[index] = (currentPattern[index] + 1) % symbolOptions.Length;
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
        if (runeTexts == null ||
            symbolOptions == null ||
            symbolOptions.Length == 0 ||
            currentPattern == null)
        {
            return;
        }

        for (int i = 0; i < runeTexts.Length; i++)
        {
            int symbolIndex = Mathf.Clamp(currentPattern[i], 0, symbolOptions.Length - 1);
            runeTexts[i].text = symbolOptions[symbolIndex];
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
