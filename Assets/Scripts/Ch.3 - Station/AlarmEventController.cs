using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class AlarmEventController : MonoBehaviour
{
    [Header("Computer Screens")]
    public StationComputerScreenController[] computers;

    [Header("Alarm")]
    public Image redOverlay;
    public float overlayPulseSpeed = 0.1f;
    public float overlayMaxAlpha = 0.1f;
    public float overlayMinAlpha = 0f;

    [Header("Audio")]
    public AudioSource alarmSiren;

    [Header("Dialogue")]
    public GameObject dialogueTrigger;

    private Coroutine overlayCoroutine;

    public float autoStopDelay = 30f;
    public DialogueAsset postAlarmDialogue;
    private Coroutine autoStopRoutine;

    private void Awake()
    {
        if (redOverlay != null)
            redOverlay.color = new Color(1f, 0f, 0f, 0f);
    }

    public void TriggerAlarmEvent()
    {
        foreach(var computer in computers)          // activate computer screens
        {
            if (computer != null)
                computer.ActivateScreen();
        }
        
        if(redOverlay != null)                      // activate red overlay
        {
            if(overlayCoroutine != null)
                StopCoroutine(overlayCoroutine);
            overlayCoroutine = StartCoroutine(AnimateRedOverlay());
        }

        if (alarmSiren != null)                     // play alarm sound
            alarmSiren.Play();

        if (dialogueTrigger != null)                // activate response dialogue
            dialogueTrigger.SetActive(true);

        if (autoStopRoutine != null)
            StopCoroutine(autoStopRoutine);

        autoStopRoutine = StartCoroutine(AutoStopAlarm());
    }

    private IEnumerator AutoStopAlarm()
    {
        yield return new WaitForSeconds(autoStopDelay);
        StopAlarm();
        if(postAlarmDialogue != null && DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive())
        {
            DialogueSystem.Instance.QueueDialogue(postAlarmDialogue);
        }
    }

    private IEnumerator AnimateRedOverlay()
    {
        RectTransform overlayRect = redOverlay.rectTransform;
        Vector3 originalPos = overlayRect.localPosition;

        while (true)
        {
            float alpha = Mathf.PingPong(Time.time * overlayPulseSpeed, overlayMaxAlpha -  overlayMinAlpha);
            redOverlay.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }
    }

    public void StopAlarm()
    {
        if(overlayCoroutine != null)
        {
            StopCoroutine(overlayCoroutine);
            overlayCoroutine = null;
        }
        if (redOverlay != null)
            redOverlay.color = new Color(1f, 0f, 0f, 0f);
        if (alarmSiren != null)
            alarmSiren.Stop();
    }
}
