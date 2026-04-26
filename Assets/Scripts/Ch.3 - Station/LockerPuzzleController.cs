using TMPro;
using UnityEngine;

public class LockerPuzzleController : MonoBehaviour
{
    [Header("UI")]
    public GameObject lockerUI;
    public TextMeshProUGUI[] digitTexts;

    [Header("Code")]
    public int[] correctCode = new int[] { 2, 9, 5, 3, 7, 4 };

    private int[] currentCode = new int[6];

    [Header("Completion Dialogue")]
    [Tooltip("Played when the player enters the code successfully.")]
    public DialogueAsset policeFileObtainedDialogue;

    [Header("Save Progress")]
    [SerializeField] private string puzzleID = "StationLocker";
    [SerializeField] private InteractableObject puzzleInteractable;

    private void Awake()
    {
        ResolvePuzzleInteractable();
    }

    public void Open()
    {
        lockerUI.SetActive(true);

        for (int i = 0; i < currentCode.Length; i++)
            currentCode[i] = 0;
        RefreshUI();
    }

    public void Close()
    {
        lockerUI.SetActive(false);
        if (InputLock.Instance != null)
        {
            InputLock.Instance.GameplayInputEnabled = true;
        }
        Time.timeScale = 1f;
        DialogueSystem.Instance.QueueDialogue(policeFileObtainedDialogue);
    }

    public void CycleDigit(int index)
    {
        currentCode[index] = (currentCode[index] + 1) % 10;
        RefreshUI();
    }

    public void Submit()
    {
        for(int i = 0; i < 6; i++)
        {
            if (currentCode[i] != correctCode[i])
            {
                Debug.Log("Wrong Code!");
                return;
            }
        }
        Unlock();
    }

    void Unlock()
    {
        ResolvePuzzleInteractable();

        if (puzzleInteractable != null && !string.IsNullOrWhiteSpace(puzzleInteractable.puzzleID))
        {
            puzzleInteractable.OnPuzzleSolved();
        }
        else if (SaveSystem.Instance != null && !string.IsNullOrWhiteSpace(puzzleID))
        {
            SaveSystem.Instance.UnlockPuzzle(puzzleID);
        }

        Debug.Log("LOCKER UNLOCKED: Police file obtained!");
        Close();
    }

    void RefreshUI()
    {
        for(int i = 0; i <digitTexts.Length; i++)
        {
            digitTexts[i].text = currentCode[i].ToString();
        }
    }

    private void ResolvePuzzleInteractable()
    {
        if (puzzleInteractable != null)
        {
            return;
        }

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

            if (interactable.puzzleUI == lockerUI ||
                (!string.IsNullOrEmpty(puzzleID) && interactable.puzzleID == puzzleID))
            {
                puzzleInteractable = interactable;
                break;
            }
        }
    }
}
