using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject choiceButtonContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Audio")]
    [SerializeField] private bool playVoiceClips = true;

    private DialogueSystem dialogueSystem;

    private Coroutine typingCoroutine;
    private bool isTyping;
    private string currentFullText;
    private DialogueLine currentLine;

    private readonly List<Button> activeChoiceButtons = new List<Button>();

    private void Awake()
    {
        Initialize();
        HideAll();
    }

    private void OnDestroy()
    {
        if (dialogueSystem == null)
        {
            return;
        }

        dialogueSystem.OnDialogueStarted -= HandleDialogueStarted;
        dialogueSystem.OnLineStarted -= HandleLineStarted;
        dialogueSystem.OnChoicesOffered -= HandleChoicesOffered;
        dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void Initialize()
    {
        if (dialogueSystem != null)
        {
            return;
        }

        // Primary approach: singleton instance
        dialogueSystem = DialogueSystem.Instance;

        // Fallback: find system in scene (Unity 2023+ safe API)
        if (dialogueSystem == null)
        {
            dialogueSystem = FindFirstObjectByType<DialogueSystem>();
        }

        if (dialogueSystem == null)
        {
            Debug.LogError("DialogueUI could not find DialogueSystem in scene.");
            enabled = false;
            return;
        }

        dialogueSystem.OnDialogueStarted += HandleDialogueStarted;
        dialogueSystem.OnLineStarted += HandleLineStarted;
        dialogueSystem.OnChoicesOffered += HandleChoicesOffered;
        dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
    }

    private void Update()
    {
        if (dialogueSystem == null || !dialogueSystem.IsDialogueActive())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleAdvanceInput();
        }
    }

    private void HandleAdvanceInput()
    {
        if (isTyping)
        {
            CompleteTextInstantly();
        }
        else if (dialogueSystem.State == DialogueState.WaitingForAdvance)
        {
            dialogueSystem.AdvanceLine();
        }
    }

    private void HandleDialogueStarted(DialogueAsset asset)
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        ClearChoices();

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }
    }

    private void HandleLineStarted(DialogueLine line)
    {
        currentLine = line;
        currentFullText = line.dialogueText;

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (playVoiceClips && line.voiceClip != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(line.voiceClip);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLineCoroutine(currentFullText));
    }

    private void HandleChoicesOffered(List<DialogueChoice> choices)
    {
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogWarning("DialogueUI: Choices offered but UI is not wired.");
            return;
        }

        ClearChoices();
        choiceButtonContainer.SetActive(true);

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            int index = i;

            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);
            activeChoiceButtons.Add(button);

            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = choice.choiceText;
            }

            button.onClick.AddListener(() =>
            {
                dialogueSystem.SelectChoice(choice, index);
            });
        }
    }

    private void HandleDialogueEnded(DialogueAsset asset)
    {
        ClearChoices();

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }
    }

    private IEnumerator TypeLineCoroutine(string fullText)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        float cps = Mathf.Max(1f, dialogueSystem.TypingSpeed);
        float delay = 1f / cps;

        foreach (char c in fullText)
        {
            if (!isTyping)
            {
                break;
            }

            if (dialogueText != null)
            {
                dialogueText.text += c;
            }

            yield return new WaitForSecondsRealtime(delay);
        }

        if (dialogueText != null)
        {
            dialogueText.text = fullText;
        }

        isTyping = false;
        typingCoroutine = null;

        dialogueSystem.NotifyLineFinishedTyping();
    }

    private void CompleteTextInstantly()
    {
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null)
        {
            dialogueText.text = currentFullText;
        }

        dialogueSystem.NotifyLineFinishedTyping();
    }

    private void ClearChoices()
    {
        foreach (Button btn in activeChoiceButtons)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }

        activeChoiceButtons.Clear();
    }

    private void HideAll()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }
    }
}
