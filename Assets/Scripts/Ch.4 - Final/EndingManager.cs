using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the Chapter 4 hill choice flow and its end card sequence.
public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("Choice Entry")]
    [Tooltip("Dialogue played when the player interacts with the hill gate.")]
    public DialogueAsset finalChoiceDialogue;

    [Header("Ending Dialogue Assets")]
    public DialogueAsset revengeEndingDialogue;
    public DialogueAsset forgiveEndingDialogue;
    public DialogueAsset secretEndingDialogue;

    [Header("End Screen")]
    public CanvasGroup endScreenCanvas;
    public float fadeInDuration = 2f;
    public float endScreenHoldDuration = 5f;
    public string returnToScene = "MenuScene";

    [Header("End Screen Text")]
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingSubtitleText;

    [Header("Secret Ending Debug")]
    [Tooltip("Use the inspector flags below instead of save data when testing Chapter 4 in the editor.")]
    public bool overrideSecretFlagsInInspector = false;

    [Tooltip("Debug-only stand in for SaveSystem.foundMenuSecret.")]
    public bool inspectorFoundMenuSecret = false;

    [Tooltip("Debug-only stand in for SaveSystem.foundHiddenTombstone.")]
    public bool inspectorFoundHiddenTombstone = false;

    private bool gateSequenceActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (endScreenCanvas != null)
        {
            endScreenCanvas.alpha = 0f;
            endScreenCanvas.gameObject.SetActive(false);
        }
    }

    public void TriggerEnding()
    {
        if (gateSequenceActive)
            return;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogWarning("EndingManager: DialogueSystem missing.");
            return;
        }

        StartCoroutine(RunEndingSequence());
    }

    public bool HasMenuSecretForEnding()
    {
        if (overrideSecretFlagsInInspector)
            return inspectorFoundMenuSecret;

        return SaveSystem.Instance != null && SaveSystem.Instance.FoundMenuSecret();
    }

    public bool HasHiddenTombstoneForEnding()
    {
        if (overrideSecretFlagsInInspector)
            return inspectorFoundHiddenTombstone;

        return SaveSystem.Instance != null && SaveSystem.Instance.FoundHiddenTombstone();
    }

    public bool HasSecretEndingRequirements()
    {
        return HasMenuSecretForEnding() && HasHiddenTombstoneForEnding();
    }

    private IEnumerator RunEndingSequence()
    {
        gateSequenceActive = true;

        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = false;

        string choiceDialogueId = finalChoiceDialogue != null ? finalChoiceDialogue.dialogueID : string.Empty;
        int previousChoice = -1;
        if (SaveSystem.Instance != null && !string.IsNullOrEmpty(choiceDialogueId))
            previousChoice = SaveSystem.Instance.GetDialogueChoice(choiceDialogueId);

        DialogueAsset choiceDialogue = finalChoiceDialogue != null
            ? finalChoiceDialogue
            : forgiveEndingDialogue;

        Debug.Log("EndingManager: Starting final choice dialogue.");
        DialogueSystem.Instance.StartDialogue(choiceDialogue);

        yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());

        EndingType ending = ResolveSelectedEnding(choiceDialogueId, previousChoice);
        Debug.Log("EndingManager: Selected ending -> " + ending);

        DialogueAsset endingDialogue = GetDialogueForEnding(ending);
        if (endingDialogue != null)
        {
            DialogueSystem.Instance.StartDialogue(endingDialogue);
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        ApplyEndingPersistence(ending);

        string titleText;
        string subtitleText;
        GetEndingCardText(ending, out titleText, out subtitleText);
        yield return StartCoroutine(ShowEndScreen(titleText, subtitleText));

        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = true;

        gateSequenceActive = false;
    }

    private EndingType ResolveSelectedEnding(string choiceDialogueId, int previousChoice)
    {
        if (SaveSystem.Instance == null || string.IsNullOrEmpty(choiceDialogueId))
            return EndingType.Forgive;

        int selectedChoice = SaveSystem.Instance.GetDialogueChoice(choiceDialogueId);
        if (selectedChoice < 0 || selectedChoice == previousChoice)
            return EndingType.Forgive;

        switch (selectedChoice)
        {
            case 1:
                return EndingType.Revenge;
            case 2:
                return EndingType.Secret;
            default:
                return EndingType.Forgive;
        }
    }

    private DialogueAsset GetDialogueForEnding(EndingType ending)
    {
        switch (ending)
        {
            case EndingType.Revenge:
                return revengeEndingDialogue;
            case EndingType.Secret:
                return secretEndingDialogue;
            default:
                return forgiveEndingDialogue;
        }
    }

    private void ApplyEndingPersistence(EndingType ending)
    {
        if (SaveSystem.Instance == null)
            return;

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null)
            return;

        data.currentChapter = Mathf.Max(data.currentChapter, 4);
        data.currentScene = SceneManager.GetActiveScene().name;

        if (ending == EndingType.Forgive)
        {
            data.truthRevealed = true;
            data.knowsPlayerIsDead = true;
        }
        else if (ending == EndingType.Revenge)
        {
            data.edenRevealed = true;
        }
        else if (ending == EndingType.Secret)
        {
            data.truthRevealed = true;
            data.edenRevealed = true;
            data.knowsPlayerIsDead = true;
        }

        SaveSystem.Instance.SaveGame();
    }

    private void GetEndingCardText(EndingType ending, out string title, out string subtitle)
    {
        switch (ending)
        {
            case EndingType.Revenge:
                title = "BOUND SPIRIT";
                subtitle = "Akila never moved on.\n\nSome nights, people in Hillside say they see something near the old cemetery - a faint light, moving between the headstones. It's always alone.";
                break;

            case EndingType.Secret:
                title = "FOUND";
                subtitle = "Some things can only be forgiven between the people involved.\n\nNeither of them was just a villain. Neither of them was just a victim. They were two people who didn't know how to say the hard things. They learned, eventually. Some things take longer than a lifetime.";
                break;

            default:
                title = "FOUND SPIRIT";
                subtitle = "Eden Reyes graduated in 2019. She studied psychology. She wanted to help people who feel like they have no other way out.\n\nSome people still visit a grave with oranges and apples, because those were her favorites.\n\nAkila *last name*. 2001-2019. Loving daughter.";
                break;
        }
    }

    private IEnumerator ShowEndScreen(string title, string subtitle)
    {
        if (endScreenCanvas == null)
        {
            SceneManager.LoadScene(returnToScene);
            yield break;
        }

        if (endingTitleText != null)
            endingTitleText.text = title;
        if (endingSubtitleText != null)
            endingSubtitleText.text = subtitle;

        endScreenCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            endScreenCanvas.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        endScreenCanvas.alpha = 1f;
        yield return new WaitForSecondsRealtime(endScreenHoldDuration);

        Time.timeScale = 1f;
        SceneManager.LoadScene(returnToScene);
    }

    private enum EndingType
    {
        Revenge,
        Forgive,
        Secret
    }
}
