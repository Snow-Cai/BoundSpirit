using System.Collections;
using UnityEngine;

public class SpawnDialogueAfterCutscene : MonoBehaviour
{
    [Header("References")]
    public CutsceneController cutsceneController;
    public DialogueAsset introDialogue;

    [Header("Timing")]
    public float delayAfterCutscene = 0.25f;

    [Header("Optional")]
    public bool alsoPlayIfNoCutscene = true;

    private bool played;

    void Start()
    {
        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<CutsceneController>();

        StartCoroutine(WaitThenPlay());
    }

    IEnumerator WaitThenPlay()
    {
        // Wait 1 frame so CutsceneController.Start() has a chance to run
        yield return null;

        if (cutsceneController == null)
        {
            if (alsoPlayIfNoCutscene)
                TryPlay();
            yield break;
        }

        // Wait until the cutscene finishes
        while (cutsceneController.IsCutsceneActive)
            yield return null;

        yield return new WaitForSeconds(delayAfterCutscene);
        TryPlay();
    }

    void TryPlay()
    {
        if (played) return;
        if (DialogueSystem.Instance == null || introDialogue == null) return;

        //If already viewed, don't play again
        if (SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(introDialogue.dialogueID) &&
            SaveSystem.Instance.HasViewedDialogue(introDialogue.dialogueID))
            return;

        played = true;
        StartCoroutine(PlayAfterDelay());
    }

    IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterCutscene);
        GameInputState.DialogueActive = true;

        DialogueSystem.Instance.StartDialogue(introDialogue);

        if (SaveSystem.Instance != null && !string.IsNullOrEmpty(introDialogue.dialogueID))
            SaveSystem.Instance.MarkDialogueViewed(introDialogue.dialogueID);
    }
}
