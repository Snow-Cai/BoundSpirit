using System.Collections;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    public CameraFollow camFollow; //CameraFollow script
    public CharMovement charMovement;   //CharMovement script
    public float CutsceneLength = 7f;

    private void Start()
    {
        StartOpeningCutscene();
    }

    void StartOpeningCutscene()
    {
        if (charMovement != null)       //disable player movement during cutscene
            charMovement.enabled = false;
        if (camFollow != null)       //disable CameraFollow during cutscene
               camFollow.enabled = false;
        StartCoroutine(RunOpeningCutscene());
    }

    IEnumerator RunOpeningCutscene()
    {
        yield return new WaitForSeconds(CutsceneLength);    //cutscene duration
        if(camFollow != null )
            camFollow.enabled = true;   //re-enable camera follow
        if (charMovement != null)       
            charMovement.enabled = true;   //re-enable player movement
    }
}
