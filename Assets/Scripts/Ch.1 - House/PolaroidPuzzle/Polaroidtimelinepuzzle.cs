using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//manages the polaroid timeline puzzle in Chapter 1 (Akila's bedroom)
//player drags polaroid cards into chronological order

public class PolaroidTimelinePuzzle : MonoBehaviour
{
    [Header("Clarity Choice")]
    public DialogueAsset polaroidReactionDialogue;

    [Header("Puzzle Identity")]
    public string puzzleID = "Chapter1_polaroid_timeline";

    [Header("Polaroid Data (assign in order: polaroid 1 to 6)")]
    public PolaroidData[] allPolaroids;

    [Header("Timeline Slots (the answer row, left to right = slot 0 to 5)")]
    public PolaroidSlotUI[] timelineSlots;

    [Header("Hand Slots (starting area where polaroids begin)")]
    public PolaroidSlotUI[] handSlots;

    [Header("Puzzle Panel")]
    public GameObject puzzlePanel;

    [Header("Inspect Popup")]
    public GameObject inspectPopup;
    public Image inspectImage;
    public TextMeshProUGUI inspectYearText;
    public TextMeshProUGUI inspectDescriptionText;
    public Button inspectCloseButton;

    [Header("Confirm Button")]
    [Tooltip("Player presses this to check their answer")]
    public Button confirmButton;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public float feedbackDisplayTime = 2f;

    [Header("Dialogue")]
    public DialogueAsset onSolveDialogue;       //plays when puzzle is solved
    public DialogueAsset onWrongDialogue;       //plays when player confirms wrong order

    [Header("Objective")]
    public string solveObjectiveMessage = "You remember the library... Eden.";

    [Header("Solve Bridge")]
    [SerializeField] private InteractableObject puzzleInteractable;

    private bool puzzleSolved = false;
    private Coroutine feedbackCoroutine;

    public bool IsSolved => puzzleSolved || (SaveSystem.Instance != null && SaveSystem.Instance.IsPuzzleSolved(puzzleID));

    //lifecycle

    private void Start()
    {
        if (puzzleInteractable == null)
        {
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

                if (interactable.puzzleUI == puzzlePanel ||
                    (!string.IsNullOrEmpty(puzzleID) && interactable.puzzleID == puzzleID))
                {
                    puzzleInteractable = interactable;
                    break;
                }
            }
        }

        if (inspectPopup != null)
            inspectPopup.SetActive(false);

        if (inspectCloseButton != null)
            inspectCloseButton.onClick.AddListener(CloseInspectPopup);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPressed);

        if (feedbackText != null)
            feedbackText.text = "";

        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);

        //check if already solved from save
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsPuzzleSolved(puzzleID))
        {
            puzzleSolved = true;
            PopulateSolvedState();
            return;
        }

        ShuffleAndDealPolaroids();
    }

    //open/close

    public void OpenPuzzle()
    {
        //force stop player movement immediately
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        if (puzzlePanel != null)
        {
            puzzlePanel.SetActive(true);
            Canvas canvas = puzzlePanel.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = true;
        }

        InputLock.Instance.GameplayInputEnabled = false;
    }

    public void ClosePuzzle()
    {
        if (puzzleInteractable != null && puzzleInteractable.isPuzzleOpen)
        {
            puzzleInteractable.ClosePuzzle();
            return;
        }

        if (puzzlePanel != null)
        {
            Canvas canvas = puzzlePanel.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false;
            puzzlePanel.SetActive(false);
        }

        if (inspectPopup != null)
            inspectPopup.SetActive(false);

        //force stop player so they don't lurch on resume
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        InputLock.Instance.GameplayInputEnabled = true;
    }

    //setup

    private void ShuffleAndDealPolaroids()
    {
        if (allPolaroids == null || allPolaroids.Length == 0)
        {
            Debug.LogError("PolaroidTimelinePuzzle: No PolaroidData assigned!");
            return;
        }

        //Clear all slots first
        foreach (var slot in timelineSlots)
            slot.SetPolaroid(null);

        //Shuffle polaroids
        List<PolaroidData> shuffled = new List<PolaroidData>(allPolaroids);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            PolaroidData temp = shuffled[i];
            shuffled[i] = shuffled[rand];
            shuffled[rand] = temp;
        }

        //Deal into hand slots
        for (int i = 0; i < handSlots.Length && i < shuffled.Count; i++)
        {
            handSlots[i].SetPolaroid(shuffled[i]);
        }
    }

    //solution check
    //Called automatically after every drag-drop to give live feedback
    //Does not trigger win player must press confirm
    public void CheckSolution()
    {
        //highlight correct placements without revealing answer
        int correct = 0;
        for (int i = 0; i < timelineSlots.Length; i++)
        {
            if (timelineSlots[i].currentPolaroid != null &&
                timelineSlots[i].currentPolaroid.correctOrder == i)
            {
                correct++;
            }
        }

        if (feedbackText != null)
        {
            if (correct == timelineSlots.Length)
                feedbackText.text = "This feels right... press Confirm.";
            else if (correct == 0)
                feedbackText.text = "";
            else
                feedbackText.text = correct + " in the right place.";
        }
    }

    //called when player presses the Confirm button
    public void OnConfirmPressed()
    {
        //all timeline slots must be filled
        foreach (var slot in timelineSlots)
        {
            if (slot.currentPolaroid == null)
            {
                ShowFeedback("Place all polaroids in the timeline first.");
                return;
            }
        }

        if (IsSolutionCorrect())
            HandleSuccess();
        else
            HandleFailure();
    }

    private bool IsSolutionCorrect()
    {
        for (int i = 0; i < timelineSlots.Length; i++)
        {
            if (timelineSlots[i].currentPolaroid == null)
                return false;
            if (timelineSlots[i].currentPolaroid.correctOrder != i)
                return false;
        }
        return true;
    }

    //win/fail

    private void HandleSuccess()
    {
        puzzleSolved = true;
        PopulateSolvedState();
        ClosePuzzle();

        if (puzzleInteractable != null && !string.IsNullOrEmpty(puzzleInteractable.puzzleID))
        {
            puzzleInteractable.OnPuzzleSolved();
        }
        else if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(puzzleID);
        }

        if (onSolveDialogue != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(onSolveDialogue);

        // Queue the polaroid reaction clarity choice after the solve dialogue
        if (polaroidReactionDialogue != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(polaroidReactionDialogue);

        Debug.Log("POLAROID PUZZLE: Solved!");
    }

    private void PopulateSolvedState()
    {
        if (timelineSlots == null || allPolaroids == null)
        {
            return;
        }

        for (int i = 0; i < timelineSlots.Length; i++)
        {
            PolaroidSlotUI slot = timelineSlots[i];
            if (slot == null)
            {
                continue;
            }

            PolaroidData correctPolaroid = null;
            for (int p = 0; p < allPolaroids.Length; p++)
            {
                if (allPolaroids[p] != null && allPolaroids[p].correctOrder == i)
                {
                    correctPolaroid = allPolaroids[p];
                    break;
                }
            }

            slot.SetPolaroid(correctPolaroid);
        }

        if (handSlots != null)
        {
            for (int i = 0; i < handSlots.Length; i++)
            {
                if (handSlots[i] != null)
                {
                    handSlots[i].SetPolaroid(null);
                }
            }
        }
    }

    private void HandleFailure()
    {
        ShowFeedback("Something doesn't feel right...");

        if (onWrongDialogue != null && DialogueSystem.Instance != null)
        {
            //temporarily reenable dialogue input for the dialogue system
            Time.timeScale = 1f;
            DialogueSystem.Instance.StartDialogue(onWrongDialogue);
        }
    }

    //inspect popup

    public void ShowInspectPopup(PolaroidData data)
    {
        if (inspectPopup == null || data == null) return;

        if (inspectImage != null)
            inspectImage.sprite = data.polaroidImage;
        if (inspectYearText != null)
            inspectYearText.text = data.year;
        if (inspectDescriptionText != null)
            inspectDescriptionText.text = data.inspectDescription;

        inspectPopup.SetActive(true);
    }

    public void CloseInspectPopup()
    {
        if (inspectPopup != null)
            inspectPopup.SetActive(false);
    }

    //feedback

    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FeedbackRoutine(message));
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.text = message;
        yield return new WaitForSecondsRealtime(feedbackDisplayTime);
        feedbackText.text = "";
        feedbackCoroutine = null;
    }
}
