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
    [Tooltip("Dialogue played when the player interacts with the hill gate.")]
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
    public float zoomOutScale = 10f;
    public float zoomDuration = 4f;
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

    private void Start()
    {
        if(autoStartOnSceneLoad) StartCoroutine(AutoStartEnding());
    }

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

        EndingType ending = ResolveSelectedEnding();
        Debug.Log("EndingManager: Playing the ending for: -> " + ending);

        yield return StartCoroutine(PlayEndingCutscene(ending));

        ApplyEndingPersistence(ending);

        string titleText;
        string subtitleText;
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

    private EndingType ResolveSelectedEnding()                  // TEMPORARY: UPDATE WHEN SAVE SYSTEM IMPLEMENTS THE CHOICE FLAGS i.e. read from SaveSystem flags instead of dialogue system
    {
        return EndingType.Forgive;

        //int resulting = 
        //if (resultingChoice < 0)
        //    return EndingType.Forgive;

        //switch (resultingChoice)
        //{
        //    case 1:
        //        return EndingType.Revenge;
        //    case 2:
        //        return EndingType.Secret;
        //    default:
        //        return EndingType.Forgive;
        //}
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
                subtitle = "Eden Reyes graduated in 2019. She studied psychology. She wanted to help people who feel like they have no other way out.\n\nSome people still visit a grave with oranges and apples, because those were her favorites.\n\nAkila. 2001-2019. Loving daughter.";
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

    private IEnumerator PlayEndingCutscene(EndingType ending)
    {
        switch (ending)
        {
            case EndingType.Forgive:
                yield return StartCoroutine(ForgiveCutscene());
                break;
            case EndingType.Revenge:
                //yield return StartCoroutine(RevengeCutscene());
                break;
            case EndingType.Secret:
                //yield return StartCoroutine(SecretCutscene());
                break;
        }
    }

    private IEnumerator ForgiveCutscene()
    {
        yield return StartCoroutine(FadeFromBlack());
        if (walkingDialogue != null)
        {
            DialogueSystem.Instance.QueueDialogue(walkingDialogue);
        }
        DialogueSystem.Instance.AutoAdvance = true;
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(WalkToPoint(treePoint));

        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(ZoomOutCamera());

        if(musicAudio != null) musicAudio.Play();
        if(forgiveEndingDialogue != null)
        {
            DialogueSystem.Instance.QueueDialogue(forgiveEndingDialogue);
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        yield return new WaitForSeconds(2f);
        DialogueSystem.Instance.AutoAdvance = false;
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
        while(t < fadeInDuration)
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

        while(t < fadeInDuration)
        {
            t += Time.deltaTime;
            gateSR.color = Color.Lerp(startColor, targetColor, t / fadeInDuration);
            yield return null;
        }
        gateSR.color = targetColor;
    }

    private IEnumerator ZoomOutCamera()
    {
        if (camFollow != null) camFollow.followEnabled = false;         // Allow zoom out
        if (ppc != null) ppc.enabled = false;

        yield return null;

        float startSize = mainCamera.orthographicSize;
        float targetSize = zoomOutScale;

        float t = 0f;

        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t / zoomDuration);
            yield return null;
        }

        mainCamera.orthographicSize = targetSize;
    }

    private enum EndingType
    {
        Revenge,
        Forgive,
        Secret
    }
}
