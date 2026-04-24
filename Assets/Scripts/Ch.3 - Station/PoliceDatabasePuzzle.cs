using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class PoliceDatabasePuzzle : MonoBehaviour
{
    public TMP_InputField nameInput, yearInput;
    public TMP_Dropdown caseDropdown;
    public TextMeshProUGUI resultText;
    public GameObject databaseUI;

    public AlarmEventController alarmEvent;

    public string correctName = "Akila";
    public string correctYear = "2019";
    public int correctcaseIndex = 3;            // Dropdown option 3 = Homicide

    bool isProcessing = false;
    bool hasFocused = false;
    private bool hasPlayedFirstDialogue = false;

    [Header("Dialogue")]
    [Tooltip("Played on initial interact.")]
    public DialogueAsset interactDialogue;

    [Tooltip("Played when the player succeeds.")]
    public DialogueAsset alarmEventTriggeredDialogue;

    private void OnEnable()
    {
        hasFocused = false;
        StartCoroutine(BeginPuzzleFlow());
    }

    private IEnumerator BeginPuzzleFlow()
    {
        if (!hasPlayedFirstDialogue)
        {
            DialogueSystem.Instance.QueueDialogue(interactDialogue);

            while (GameInputState.DialogueActive)
            {
                yield return null;
            }

            hasPlayedFirstDialogue = true;
        }
        InputLock.Instance.CanToggleInventory = false;
        StartCoroutine(FocusNameField());
    }

    IEnumerator FocusNameField()
    {
        if (hasFocused) yield break;
        hasFocused = true;

        yield return null;

        EventSystem.current.SetSelectedGameObject(null);
        nameInput.Select();
        nameInput.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(nameInput.gameObject);
    }

    public void OnSubmit()
    {
        if (isProcessing) return;
        StartCoroutine(ProcessQuery());
    }

    IEnumerator ProcessQuery()
    {
        InputLock.Instance.InteractEnabled = false;
        isProcessing = true;
        nameInput.interactable = false;
        yearInput.interactable = false;
        caseDropdown.interactable = false;

        string baseText = "Searching database";
        for (int i = 0; i < 6; i++)
        {
            resultText.text = baseText + new string('.', i % 4);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        bool correct = nameInput.text.Trim().ToLower() == correctName.ToLower() && yearInput.text.Trim().ToLower() == correctYear.ToLower() && caseDropdown.value == correctcaseIndex;
        if (correct)
        {
            resultText.text = "ACCESSING RESTRICTED FILE...";
            yield return new WaitForSecondsRealtime(2f);

            resultText.text = "NO AUTHORIZATION CONFIRMATION PROVIDED. UNAUTHORIZED ACCESS DETECTED.";
            yield return new WaitForSecondsRealtime(0.5f);
            if (alarmEvent != null)
            {
                Time.timeScale = 1f;            // Re-enable time for alarm animation to appear properly
                alarmEvent.TriggerAlarmEvent();
                yield return new WaitForSecondsRealtime(1.5f);
                Close();
            }
        }
        else
        {
            resultText.text = "NO MATCH FOUND.";
        }

        isProcessing = false;

        if (!correct)           // Re-enable to allow more attempts
        {
            nameInput.interactable = true;
            yearInput.interactable = true;
            caseDropdown.interactable = true;
        }
        InputLock.Instance.InteractEnabled = true;
    }

    public void Close()
    {
        databaseUI.SetActive(false);
        if (InputLock.Instance != null)
        {
            InputLock.Instance.GameplayInputEnabled = true;
            InputLock.Instance.CanToggleInventory = true;
        }
        Time.timeScale = 1f;
        DialogueSystem.Instance.QueueDialogue(alarmEventTriggeredDialogue);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))              // Allow player to use tab to go from name input field to year input field
        {
            if(nameInput.isFocused)
                EventSystem.current.SetSelectedGameObject(yearInput.gameObject);
        }
    }
}
