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

    [Header("Continue Hint")]
    [SerializeField] private TextMeshProUGUI continueHintText;
    [SerializeField] private string showHintForDialogueID = "Chapter0_awakening"; // set to your spawn dialogue ID
    [SerializeField] private string continueHintSaveKey = "UI_ContinueHintShown";

    [Header("Continue Hint Polish")]
    [SerializeField] private CanvasGroup continueHintCanvas;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float pulseMinAlpha = 0.45f;
    [SerializeField] private float pulseMaxAlpha = 0.65f;
    [SerializeField] private float pulseSpeed = 1.5f;

    private Coroutine pulseRoutine;
    private Coroutine fadeRoutine;

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

        if (continueHintText != null)
            continueHintText.gameObject.SetActive(false);
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
        // If hint is showing, stop it and mark it as shown
        if (continueHintText != null && continueHintText.gameObject.activeSelf)
        {
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.MarkDialogueViewed(continueHintSaveKey);
        }

        StopContinueHint();

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

        // Show continueHint only for the spawn dialogue
        if (continueHintText != null && asset != null && asset.dialogueID == showHintForDialogueID)
        {
            bool alreadyShown = (SaveSystem.Instance != null && SaveSystem.Instance.HasViewedDialogue(continueHintSaveKey));
            if (!alreadyShown)
            {
                continueHintText.gameObject.SetActive(true);
                StartContinueHint();
            }
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
        StopContinueHint();

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

    void StartContinueHint()
    {
        if (continueHintText != null)
            continueHintText.gameObject.SetActive(true);

        // If no CanvasGroup is assigned, no fade/pulse
        if (continueHintCanvas == null)
            return;

        continueHintCanvas.alpha = 0f;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        fadeRoutine = StartCoroutine(FadeInContinueHint());
    }

    IEnumerator FadeInContinueHint()
    {
        float t = 0f;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            continueHintCanvas.alpha = Mathf.Lerp(0f, pulseMaxAlpha, t / fadeInDuration);
            yield return null;
        }

        fadeRoutine = null;
        pulseRoutine = StartCoroutine(PulseContinueHint());
    }

    IEnumerator PulseContinueHint()
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            continueHintCanvas.alpha =
                Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(t) + 1f) * 0.5f);
            yield return null;
        }
    }

    void StopContinueHint()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (continueHintCanvas != null)
            continueHintCanvas.alpha = 0f;

        if (continueHintText != null)
            continueHintText.gameObject.SetActive(false);
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
