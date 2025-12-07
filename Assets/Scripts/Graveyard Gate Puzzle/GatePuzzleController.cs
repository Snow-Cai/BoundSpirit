using UnityEngine;
using TMPro;

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
    [SerializeField] private bool showWrongDialogueOncePerOpen = true;
    private bool wrongDialogueShownThisOpen;
    [SerializeField] private DialogueAsset puzzleCompleteDialogue;

    private int[] currentPattern;

    private void Awake()
    {
        if (runeTexts == null || runeTexts.Length == 0)
        {
            //Debug.LogWarning("GatePuzzleController: No runeTexts assigned.");
            return;
        }

        if (correctPattern == null || correctPattern.Length != runeTexts.Length)
        {
            Debug.LogWarning("GatePuzzleController: correctPattern must match runeTexts length.");
        }

        currentPattern = new int[runeTexts.Length];

        // Initialize all runes to first symbol.
        for (int i = 0; i < currentPattern.Length; i++)
        {
            currentPattern[i] = 0;
        }

        RefreshRunes();
    }

    private void OnEnable()
    {
        // Reset pattern whenever the puzzle UI is opened.
        if (currentPattern == null || runeTexts == null)
        {
            return;
        }

        for (int i = 0; i < currentPattern.Length; i++)
        {
            currentPattern[i] = 0;
        }

        RefreshRunes();
        wrongDialogueShownThisOpen = false;
    }

    public void CycleRune(int index)
    {
        if (runeTexts == null ||
            symbolOptions == null ||
            symbolOptions.Length == 0)
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
            symbolOptions.Length == 0)
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
        if (wrongPatternDialogue != null &&
        DialogueSystem.Instance != null)
        {
            if (!showWrongDialogueOncePerOpen || !wrongDialogueShownThisOpen)
            {
                DialogueSystem.Instance.StartDialogue(wrongPatternDialogue);
                wrongDialogueShownThisOpen = true;
            }
        }
    }

    private void HandleCorrectPattern()
    {
        if (puzzleCompleteDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(puzzleCompleteDialogue);
        }

        if (gateController != null)
        {
            gateController.OnGatePuzzleSolved();
        }
        else
        {
            gameObject.SetActive(false);
            GameInputState.DialogueActive = false;
        }
    }
}
