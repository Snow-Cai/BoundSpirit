using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class PoliceDatabasePuzzle : MonoBehaviour
{
    public TMP_InputField nameInput, yearInput;
    public TMP_Dropdown caseDropdown;
    public TextMeshProUGUI resultText;

    public AlarmEventController alarmEvent;

    public string correctName = "Akila";
    public string correctYear = "2019";
    public int correctcaseIndex = 3;            // Dropdown option 3 = Homicide

    bool isProcessing = false;
    bool hasFocused = false;

    private void OnEnable()
    {
        hasFocused = false;
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
                alarmEvent.TriggerAlarmEvent();
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
    }

    private void Update()
    {
        if (isProcessing)
            return;
        if (Input.GetKeyDown(KeyCode.Tab))              // Allow player to use tab to go from name input field to year input field
        {
            if(nameInput.isFocused)
                EventSystem.current.SetSelectedGameObject(yearInput.gameObject);
        }
    }
}
