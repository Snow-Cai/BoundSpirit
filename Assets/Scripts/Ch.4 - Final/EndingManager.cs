using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Runs the Chapter 4 hill choice flow and its end card sequence.
public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("Choice Entry")]
    [Tooltip("Assign the Chapter4_finalChoice DialogueAsset here.")]
    public DialogueAsset finalChoiceDialogue;

    [Header("Ending Dialogue Assets")]
    public DialogueAsset revengeEndingDialogue;
    public DialogueAsset forgiveEndingDialogue;
    public DialogueAsset secretEndingDialogue;
    public DialogueAsset walkingDialogue;

    [Header("End Screen")]
    public CanvasGroup endScreenCanvas;
    public float fadeInDuration = 2f;
    public float endScreenHoldDuration = 5f;
    public string returnToScene = "MenuScene";

    [Header("End Screen Text")]
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingSubtitleText;

    [Header("Cutscene References")]
    public Transform player, hillStartPoint, treePoint;
    public float walkSpeed = 0.5f;
    public CanvasGroup fadeCanvas;
    public float zoomOutSize = 10f;
    public float zoomDuration = 8f;
    public float panUpAmount = 1f;
    public Camera mainCamera;
    public CameraFollow camFollow;
    public PixelPerfectCamera ppc;
    public SpriteRenderer gateSR;

    public AudioSource musicAudio;

    [Header("Secret Ending Debug")]
    [Tooltip("Use the inspector flags below instead of save data when testing Chapter 4 in the editor.")]
    public bool overrideSecretFlagsInInspector = false;

    [Tooltip("Debug-only stand in for SaveSystem.foundMenuSecret.")]
    public bool inspectorFoundMenuSecret = false;

    [Tooltip("Debug-only stand in for SaveSystem.foundHiddenTombstone.")]
    public bool inspectorFoundHiddenTombstone = false;

    [Tooltip("To test without automatic cutscene playing.")]
    public bool autoStartOnSceneLoad = true;

    private bool gateSequenceActive;
    public bool IsEndingPresentationActive => gateSequenceActive;

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

    private void Start()
    {
        if (autoStartOnSceneLoad) StartCoroutine(AutoStartEnding());
    }

    private IEnumerator AutoStartEnding()
    {
        yield return new WaitForSeconds(0.5f);
        TriggerEnding();
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

    //secret flag helpers

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

    //main sequence

    private IEnumerator RunEndingSequence()
    {
        gateSequenceActive = true;

        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = false;

        //play the walking monologue
        if (walkingDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(walkingDialogue);
        }

        //Step 2: walk the player to the tree
        if (ppc != null) ppc.enabled = false;
        yield return StartCoroutine(FadeFromBlack());

        DialogueSystem.Instance.AutoAdvance = true;
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(WalkToPoint(treePoint));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(ZoomOutCamera());

        // Wait for the walking dialogue to finish before showing the choice
        yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        DialogueSystem.Instance.AutoAdvance = false;

        //step 3: present the branching final choice
        if (finalChoiceDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(finalChoiceDialogue);

            // Wait until the player has picked a choice and dialogue is fully done
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());

            // One extra frame so ClarityChoiceHandler UnityEvents have written their flags
            yield return null;
        }

        //Step 4: resolve which ending was earned
        EndingType ending = ResolveSelectedEnding();
        Debug.Log("EndingManager: Resolved ending -> " + ending);

        if (musicAudio != null) musicAudio.Play();

        //Step 5: play the ending-specific cutscene 
        switch (ending)
        {
            case EndingType.Forgive:
                yield return StartCoroutine(ForgiveCutscene());
                break;
            case EndingType.Revenge:
                yield return StartCoroutine(RevengeCutscene());
                break;
            case EndingType.Secret:
                yield return StartCoroutine(SecretCutscene());
                break;
        }

        //step 6: persist ending data then show end card
        ApplyEndingPersistence(ending);

        string titleText, subtitleText;
        GetEndingCardText(ending, out titleText, out subtitleText);

        if (ending == EndingType.Forgive || ending == EndingType.Secret)
        {
            yield return StartCoroutine(FadeInGate());
            yield return new WaitForSecondsRealtime(2f);
        }

        yield return StartCoroutine(ShowEndScreen(titleText, subtitleText));

        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = true;

        gateSequenceActive = false;
    }

    //ending resolution

    /// <summary>
    /// Reads the story flags written by ClarityChoiceHandler to decide the ending.
    /// Secret always wins if both secret flags AND the puzzle key are present.
    /// Forgive requires TruthRevealed = true AND clarityScore >= threshold.
    /// Revenge is the fallback.
    /// </summary>
    private EndingType ResolveSelectedEnding()
    {
        // Secret ending takes priority when all requirements are met
        if (HasSecretEndingRequirements() &&
            SaveSystem.Instance != null &&
            SaveSystem.Instance.IsPuzzleSolved("secret_ending_unlocked"))
        {
            return EndingType.Secret;
        }

        if (SaveSystem.Instance == null)
            return EndingType.Revenge;

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null)
            return EndingType.Revenge;

        // Forgive: player accepted the full truth AND accumulated enough clarity
        if (data.truthRevealed && ClaritySystem.CanSeeForgiveEnding())
            return EndingType.Forgive;

        // Revenge: default when truth hasn't been accepted or clarity is too low
        return EndingType.Revenge;
    }

    //Ending cutscenes

    private IEnumerator ForgiveCutscene()
    {
        if (forgiveEndingDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(forgiveEndingDialogue);
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        yield return new WaitForSeconds(2f);
        DialogueSystem.Instance.AutoAdvance = false;
    }

    private IEnumerator RevengeCutscene()
    {
        if (revengeEndingDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(revengeEndingDialogue);
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        yield return new WaitForSeconds(2f);
        DialogueSystem.Instance.AutoAdvance = false;
    }

    private IEnumerator SecretCutscene()
    {
        if (secretEndingDialogue != null && DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(secretEndingDialogue);
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        yield return new WaitForSeconds(2f);
        DialogueSystem.Instance.AutoAdvance = false;
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

            default: // Forgive
                title = "FOUND SPIRIT";
                subtitle = "Eden Reyes graduated in 2019. She studied psychology. She wanted to help people who feel like they have no other way out.\n\nSome people still visit Akila's grave with oranges and apples, because those were her favorites.\n\nAkila. 2001-2019. Loving daughter.";
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
        if (camFollow != null) camFollow.followEnabled = true;
        if (ppc != null) ppc.enabled = true;
        SceneManager.LoadScene(returnToScene);
    }

    private IEnumerator WalkToPoint(Transform target)
    {
        while (Vector3.Distance(player.position, target.position) > 7f)
        {
            player.position = Vector3.MoveTowards(player.position, target.position, walkSpeed * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(6f);
        while (Vector3.Distance(player.position, target.position) > 0.1f)
        {
            player.position = Vector3.MoveTowards(player.position, target.position, walkSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator FadeFromBlack()
    {
        fadeCanvas.gameObject.SetActive(true);
        fadeCanvas.alpha = 1f;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = 1f - (t / fadeInDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    private IEnumerator FadeInGate()
    {
        if (gateSR == null) yield break;
        float t = 0f;

        Color startColor = gateSR.color;
        startColor.a = 0f;
        Color targetColor = gateSR.color;
        targetColor.a = 1f;

        gateSR.color = startColor;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            gateSR.color = Color.Lerp(startColor, targetColor, t / fadeInDuration);
            yield return null;
        }
        gateSR.color = targetColor;
    }

    private IEnumerator ZoomOutCamera()
    {
        if (camFollow != null) camFollow.followEnabled = false;

        yield return null;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 targetPos = startPos + new Vector3(0f, panUpAmount, 0f);

        float startSize = mainCamera.orthographicSize;
        float targetSize = zoomOutSize;
        float t = 0f;

        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            float normalized = t / zoomDuration;
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, eased);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, eased);
            pos.x = Mathf.Round(pos.x * 32f) / 32f;
            pos.y = Mathf.Round(pos.y * 32f) / 32f;
            mainCamera.transform.position = pos;
            yield return null;
        }

        mainCamera.orthographicSize = targetSize;
        mainCamera.transform.position = targetPos;
    }

    private enum EndingType
    {
        Revenge,
        Forgive,
        Secret
    }
}