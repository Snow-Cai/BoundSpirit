using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    private const float ChoiceButtonHeight = 50f;
    private const float ChoiceButtonSpacing = 10f;
    private const float ChoiceContainerHorizontalPadding = 8f;
    private const float ChoiceContainerTopPadding = 12f;

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

    [Header("Continue Hint: Show If Player Doesn't React")]
    [SerializeField] private float continueHintAppearDelay = 0.6f;
    [SerializeField] private string continueHintSaveKey = "UI_ContinueHintShown";
    [SerializeField] private bool showHintOnlyOnceEver = false;

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

    private Coroutine delayedHintCoroutine;

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

        if (Input.GetKeyDown(KeyCode.Space) && !DialogueSystem.Instance.AutoAdvance)
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

        CancelDelayedHint();
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

        CancelDelayedHint();
        StopContinueHint();
    }

    private void HandleLineStarted(DialogueLine line)
    {
        currentLine = line;
        currentFullText = line.dialogueText;

        if (speakerNameText != null)
        {
            speakerNameText.text = PlayerIdentityPresentation.GetDisplayedSpeakerName(
                line.speakerName,
                dialogueSystem != null ? dialogueSystem.ActiveDialogueId : null);
        }

        if (playVoiceClips && line.voiceClip != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(line.voiceClip);
        }

        CancelDelayedHint();
        StopContinueHint();

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

        CancelDelayedHint();
        StopContinueHint();

        ClearChoices();
        choiceButtonContainer.SetActive(true);

        int availableChoiceCount = 0;
        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            if (!IsChoiceAvailable(choice))
                continue;

            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);
            activeChoiceButtons.Add(button);

            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = choice.choiceText;
                tmpText.enableAutoSizing = true;
                tmpText.fontSizeMin = 20f;
                tmpText.fontSizeMax = 28f;
                tmpText.alignment = TextAlignmentOptions.Center;
            }

            ConfigureChoiceButtonLayout(button, availableChoiceCount);

            int index = i;
            button.onClick.AddListener(() =>
            {
                dialogueSystem.SelectChoice(choice, index);
            });

            availableChoiceCount++;
        }

        if (availableChoiceCount == 0)
        {
            choiceButtonContainer.SetActive(false);
        }
    }

    private void ConfigureChoiceButtonLayout(Button button, int visibleChoiceIndex)
    {
        if (button == null || choiceButtonContainer == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        RectTransform containerRect = choiceButtonContainer.GetComponent<RectTransform>();
        if (buttonRect == null || containerRect == null)
            return;

        float width = containerRect.rect.width - (ChoiceContainerHorizontalPadding * 2f);
        if (width <= 0f)
            width = 320f;

        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.sizeDelta = new Vector2(width, ChoiceButtonHeight);
        buttonRect.anchoredPosition = new Vector2(
            0f,
            -ChoiceContainerTopPadding - visibleChoiceIndex * (ChoiceButtonHeight + ChoiceButtonSpacing));
    }

    private bool IsChoiceAvailable(DialogueChoice choice)
    {
        if (choice == null)
            return false;

        if (!AreFlagsSatisfied(choice.requiredFlags, requirePresence: true))
            return false;

        if (!AreFlagsSatisfied(choice.forbiddenFlags, requirePresence: false))
            return false;

        return true;
    }

    private bool AreFlagsSatisfied(List<string> flags, bool requirePresence)
    {
        if (flags == null || flags.Count == 0)
            return true;

        for (int i = 0; i < flags.Count; i++)
        {
            bool isSet = ResolveFlag(flags[i]);
            if (requirePresence && !isSet)
                return false;
            if (!requirePresence && isSet)
                return false;
        }

        return true;
    }

    private bool ResolveFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName))
            return false;

        if (string.Equals(flagName, "foundHiddenTombstone", System.StringComparison.OrdinalIgnoreCase))
        {
            if (EndingManager.Instance != null)
                return EndingManager.Instance.HasHiddenTombstoneForEnding();

            if (SaveSystem.Instance == null)
                return false;

            return SaveSystem.Instance.FoundHiddenTombstone();
        }

        if (string.Equals(flagName, "foundMenuSecret", System.StringComparison.OrdinalIgnoreCase))
        {
            if (EndingManager.Instance != null)
                return EndingManager.Instance.HasMenuSecretForEnding();

            if (SaveSystem.Instance == null)
                return false;

            return SaveSystem.Instance.FoundMenuSecret();
        }

        if (string.Equals(flagName, "clarityForForgive", System.StringComparison.OrdinalIgnoreCase))
        {
            return ClaritySystem.CanSeeForgiveEnding();
        }

        if (SaveSystem.Instance == null)
            return false;

        if (System.Enum.TryParse(flagName, true, out StoryFlags.Flag storyFlag))
            return StoryFlags.IsSet(storyFlag);

        return SaveSystem.Instance.IsPuzzleSolved(flagName) ||
               SaveSystem.Instance.HasViewedDialogue(flagName);
    }

    private void HandleDialogueEnded(DialogueAsset asset)
    {
        CancelDelayedHint();
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

        // hint after a delay ONLY if player hasn't advanced
        ScheduleDelayedHintIfNeeded();
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
        ScheduleDelayedHintIfNeeded();
    }

    private void ScheduleDelayedHintIfNeeded()
    {
        // If once-ever across the whole game
        if (showHintOnlyOnceEver)
        {
            bool alreadyShown = (SaveSystem.Instance != null && SaveSystem.Instance.HasViewedDialogue(continueHintSaveKey));
            if (alreadyShown)
                return;
        }

        // Only if waiting for player advance (not choices)
        if (dialogueSystem.State != DialogueState.WaitingForAdvance)
            return;

        CancelDelayedHint();
        delayedHintCoroutine = StartCoroutine(DelayedShowContinueHint());
    }

    private IEnumerator DelayedShowContinueHint()
    {
        yield return new WaitForSecondsRealtime(continueHintAppearDelay);

        // Don't show if dialogue is advancing automatically
        if (DialogueSystem.Instance.AutoAdvance == true)
            yield break;

        // Player might have advanced / dialogue ended / choices appeared
        if (dialogueSystem == null || !dialogueSystem.IsDialogueActive())
            yield break;

        if (dialogueSystem.State != DialogueState.WaitingForAdvance)
            yield break;

        // If once-ever, re-check (in case something marked it)
        if (showHintOnlyOnceEver)
        {
            bool alreadyShown = (SaveSystem.Instance != null && SaveSystem.Instance.HasViewedDialogue(continueHintSaveKey));
            if (alreadyShown)
                yield break;
        }

        StartContinueHint();
    }

    private void CancelDelayedHint()
    {
        if (delayedHintCoroutine != null)
        {
            StopCoroutine(delayedHintCoroutine);
            delayedHintCoroutine = null;
        }
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
