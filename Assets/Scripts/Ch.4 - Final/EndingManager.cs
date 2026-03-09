using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

//Determines and triggers one of three endings based on story flags in SaveSystem
///Attach to a persistent GameObject or the ending scene
//ENDINGS:
//Revenge  - edenRevealed = true, truthRevealed = false  -> cannot ascend, stays as ghost
//Forgive  - truthRevealed = true, knowsPlayerIsDead = true -> ascends peacefully
//Secret   - all three flags true + solvedPuzzles contains "secret_ending_unlocked"
//call TriggerEnding() from AfterlifeGateEnding.cs when player interacts with gate
public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("Ending Dialogue Assets")]
    [Tooltip("Akila refuses to forgive Eden. Cannot pass through the gate.")]
    public DialogueAsset revengeEndingDialogue;

    [Tooltip("Akila forgives Eden and herself. Ascends peacefully.")]
    public DialogueAsset forgiveEndingDialogue;

    [Tooltip("Secret ending - Akila realises she was the cause of everything.")]
    public DialogueAsset secretEndingDialogue;

    [Header("End Screen")]
    [Tooltip("Canvas Group covering the whole screen for the end card.")]
    public CanvasGroup endScreenCanvas;

    [Tooltip("How long to fade into the end screen after dialogue finishes.")]
    public float fadeInDuration = 2f;

    [Tooltip("How long the end screen stays visible before returning to menu.")]
    public float endScreenHoldDuration = 5f;

    [Tooltip("Scene name to load after end screen. Usually your main menu.")]
    public string returnToScene = "MenuScene";

    [Header("End Screen Text (optional)")]
    public TMPro.TextMeshProUGUI endingTitleText;
    public TMPro.TextMeshProUGUI endingSubtitleText;

    private bool endingTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //Hide end screen at start
        if (endScreenCanvas != null)
        {
            endScreenCanvas.alpha = 0f;
            endScreenCanvas.gameObject.SetActive(false);
        }
    }

    //Call when the player interacts with the afterlife gate at the end
    //reads story flags from SaveSystem and picks the correct ending
    public void TriggerEnding()
    {
        if (endingTriggered) return;
        endingTriggered = true;

        EndingType ending = DetermineEnding();
        Debug.Log("ENDING: Triggering ending -> " + ending);

        StartCoroutine(PlayEnding(ending));
    }

    private EndingType DetermineEnding()
    {
        if (SaveSystem.Instance == null) return EndingType.Forgive;

        SaveData data = SaveSystem.Instance.GetSaveData();

        //Secret ending: knows everything AND explicitly unlocked
        bool secretUnlocked = SaveSystem.Instance.IsPuzzleSolved("secret_ending_unlocked");
        if (data.truthRevealed && data.edenRevealed && data.knowsPlayerIsDead && secretUnlocked)
            return EndingType.Secret;

        //Forgive ending: Akila has accepted the full truth
        if (data.truthRevealed && data.knowsPlayerIsDead)
            return EndingType.Forgive;

        //Revenge ending: knows Eden killed her but hasn't accepted truth
        if (data.edenRevealed && !data.truthRevealed)
            return EndingType.Revenge;

        //forgive if flags are incomplete (failsafe)
        return EndingType.Forgive;
    }

    private IEnumerator PlayEnding(EndingType ending)
    {
        //block player input during ending
        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = false;

        //pick the right dialogue asset
        DialogueAsset chosenDialogue = null;
        string titleText = "";
        string subtitleText = "";

        switch (ending)
        {
            case EndingType.Revenge:
                chosenDialogue = revengeEndingDialogue;
                titleText = "BOUND";
                subtitleText = "Some souls are too heavy to let go.";
                break;

            case EndingType.Forgive:
                chosenDialogue = forgiveEndingDialogue;
                titleText = "AT PEACE";
                subtitleText = "She finally let herself rest.";
                break;

            case EndingType.Secret:
                chosenDialogue = secretEndingDialogue;
                titleText = "THE TRUTH";
                subtitleText = "The only person who could have saved her... was herself.";
                break;
        }

        //play ending dialogue then wait for it to finish
        if (DialogueSystem.Instance != null && chosenDialogue != null)
        {
            DialogueSystem.Instance.StartDialogue(chosenDialogue);

            //wait until dialogue is done
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsDialogueActive());
        }

        //small beat before the end screen
        yield return new WaitForSeconds(1f);

        //show end screen
        yield return StartCoroutine(ShowEndScreen(titleText, subtitleText));
    }

    private IEnumerator ShowEndScreen(string title, string subtitle)
    {
        if (endScreenCanvas == null) yield break;

        //set text before fading in
        if (endingTitleText != null) endingTitleText.text = title;
        if (endingSubtitleText != null) endingSubtitleText.text = subtitle;

        endScreenCanvas.gameObject.SetActive(true);

        //Fade in
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            endScreenCanvas.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        endScreenCanvas.alpha = 1f;

        //Hold
        yield return new WaitForSeconds(endScreenHoldDuration);

        //Return to menu
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