using UnityEngine;
using System.Collections;

public class FloorTransition : MonoBehaviour
{
    [Header("Floors")]
    public GameObject firstFloor;
    public GameObject secondFloor;

    [Header("Player")]
    public Transform player;

    [Header("Positions")]
    public Vector3 upstairsPosition;
    public Vector3 downstairsPosition;

    [Header("Fade Settings")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 0.5f;

    private bool onSecondFloor = false;

    void Start()
    {
        //Load which floor player should be on when scene starts
        LoadFloorState();
    }

    public void TriggerTransition()
    {
        StartCoroutine(SwitchFloor());
    }

    private IEnumerator SwitchFloor()
    {
        //Block saving during floor transition
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetTransitioning(true);
            Debug.Log("FLOOR TRANSITION: Started - blocking saves");
        }

        float t = 0f;
        Vector3 startPos = player.position;
        Vector3 targetPos = onSecondFloor ? downstairsPosition : upstairsPosition;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            player.position = Vector3.Lerp(startPos, targetPos, t / fadeDuration);
            yield return null;
        }

        player.position = targetPos;

        if (!onSecondFloor)
        {
            firstFloor.SetActive(false);
            secondFloor.SetActive(true);
            onSecondFloor = true;
        }
        else
        {
            firstFloor.SetActive(true);
            secondFloor.SetActive(false);
            onSecondFloor = false;
        }

        //SAVE THE FLOOR STATE
        SaveFloorState();

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = 0;

        //Re-enable saving after transition completes
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetTransitioning(false);
            Debug.Log("FLOOR TRANSITION: Completed - re-enabling saves");
        }
    }

    void SaveFloorState()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.GetSaveData().onSecondFloor = onSecondFloor;
            SaveSystem.Instance.SaveGame();
            Debug.Log("FLOOR STATE: Saved - On second floor: " + onSecondFloor);
        }
    }

    public bool LoadFloorState()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            onSecondFloor = SaveSystem.Instance.GetSaveData().onSecondFloor;

            //Set the correct floor active based on saved state
            if (onSecondFloor)
            {
                firstFloor.SetActive(false);
                secondFloor.SetActive(true);
                Debug.Log("FLOOR STATE: Loaded - Player on second floor");
                return true;    //return true if on the second floor
            }
            else
            {
                firstFloor.SetActive(true);
                secondFloor.SetActive(false);
                Debug.Log("FLOOR STATE: Loaded - Player on first floor");
                return false;   //return false if on the first floor
            }
        }
        else
        {
            //Default: start on first floor
            firstFloor.SetActive(true);
            secondFloor.SetActive(false);
            onSecondFloor = false;
            Debug.Log("FLOOR STATE: No save data - defaulting to first floor");
            return false;
        }
    }
}