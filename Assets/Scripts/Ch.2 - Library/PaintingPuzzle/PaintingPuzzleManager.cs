using UnityEngine;
using TMPro;

public class PaintingPuzzleManager : MonoBehaviour
{
    public static PaintingPuzzleManager Instance;

    [Header("Target Solution")]
    private string target = "CASEFILE";

    [Header("Progress")]
    private char[] current;
    private int index = 0;

    [Header("UI")]
    public TextMeshProUGUI displayText;

    [Header("Completion")]
    public DialogueAsset dialogueOnPaintingSolve;

    [HideInInspector] public bool puzzleReady;
    [SerializeField] private string puzzleID = "PaintingPuzzle";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        current = new char[target.Length];
        UpdateUI();
    }

    public bool CanAcceptLetters()
    {
        if (!puzzleReady) return false;
        return index < target.Length;
    }

    public void SetLetter(int slot, char c)
    {
        if(!CanAcceptLetters()) return;
        if (current[slot] != '\0') return;

        current[slot] = c;
        index++;

        UpdateUI();

        if (index >= target.Length) CompletePuzzle();
    }

    private void UpdateUI()
    {
        string display = "";
        for (int i = 0; i < target.Length; i++)
        {
            if (i == target.Length / 2)
                display += " ";
            if (current[i] != '\0')
                display += current[i] + " ";
            else
                display += "_ ";
        }
        displayText.text = display.Trim();
    }

    private void CompletePuzzle()
    {
        LibraryPuzzleStateBridge.Instance.SetPaintingSolved();
        DialogueSystem.Instance.StartDialogue(dialogueOnPaintingSolve);
        if (SaveSystem.Instance != null)
        {
            if (puzzleID != null && !string.IsNullOrWhiteSpace(puzzleID))
            {
                SaveSystem.Instance.UnlockPuzzle(puzzleID);
            }
        }
        Debug.Log("Found missing words!");
    }
}
