using System.Collections;
using UnityEngine;
using UnityEngine.Playables; //Timeline support

public class CutsceneController : MonoBehaviour
{
    public CameraFollow camFollow; //CameraFollow script
    public CharMovement charMovement;   //CharMovement script
    public float CutsceneLength = 7f;

    [Header("Timeline")]
    public PlayableDirector cutsceneTimeline; //Reference to the Timeline

    [Header("Cutscene ID")]
    public string cutsceneID = "chapter0_intro"; //Unique ID for this cutscene
    public bool IsCutsceneActive { get; private set; }

    [Header("Cutscene Elements to Hide")]
    public GameObject[] cutsceneObjects; //Canvas, text, images that should hide after cutscene

    private void Start()
    {
        //Only play cutscene if not viewed before OR if no save system exists
        if (SaveSystem.Instance == null || !SaveSystem.Instance.HasViewedDialogue(cutsceneID))
        {
            StartOpeningCutscene();

            //Mark as viewed
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkDialogueViewed(cutsceneID);
            }
        }
        else
        {
            //Cutscene already seen, skip it and hide all cutscene elements
            Debug.Log("Cutscene already viewed, skipping...");

            //Hide all cutscene objects immediately
            HideCutsceneElements();

            //Make sure player can move
            if (camFollow != null)
                camFollow.enabled = true;
            if (charMovement != null)
                charMovement.enabled = true;

            IsCutsceneActive = false;
        }
    }

    void StartOpeningCutscene()
    {
        IsCutsceneActive = true;

        if (charMovement != null)       //disable player movement during cutscene
            charMovement.enabled = false;
        if (camFollow != null)       //disable CameraFollow during cutscene
            camFollow.enabled = false;

        //Play the Timeline if it exists
        if (cutsceneTimeline != null)
        {
            cutsceneTimeline.Play();
        }

        StartCoroutine(RunOpeningCutscene());
    }

    IEnumerator RunOpeningCutscene()
    {
        yield return new WaitForSeconds(CutsceneLength);    //cutscene duration

        //Hide cutscene elements after it finishes
        HideCutsceneElements();

        if (camFollow != null)
            camFollow.enabled = true;   //re-enable camera follow
        if (charMovement != null)
            charMovement.enabled = true;   //Re-enable player movement

        IsCutsceneActive = false;
    }

    void HideCutsceneElements()
    {
        //Hide all assigned cutscene objects (text, canvas, etc.)
        if (cutsceneObjects != null)
        {
            foreach (GameObject obj in cutsceneObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}
