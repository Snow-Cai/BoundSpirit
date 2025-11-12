using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloorTransition : MonoBehaviour
{
    [Header("Floors")]
    public GameObject firstFloor;
    public GameObject secondFloor;

    [Header("Player")]
    public Transform player;

    [Header("Positions")]       //where the player will be before and after switching floors
    public Vector3 upstairsPosition;
    public Vector3 downstairsPosition;

    [Header("Fade Settings")]       //adjust fade
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 0.5f;

    private bool onSecondFloor = false;

    public void TriggerTransition()     //call when player enters stairs
    {
        StartCoroutine(SwitchFloor());
    }
    private IEnumerator SwitchFloor()
    {
        float t = 0f;
        while(t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 1;
    }
}
