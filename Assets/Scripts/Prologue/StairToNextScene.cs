using UnityEngine;
using UnityEngine.SceneManagement;

public class StairToNextScene : MonoBehaviour
{
    public CharMovement charMovement;
    public Transform player;
    public float walkSpeed = 2f;
    public string nextScene = "Chapter1_Home";
    public CanvasGroup fadeScreen;
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;
        if (!collision.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(DoStairTransition());
    }

    private System.Collections.IEnumerator DoStairTransition()
    {
        //SAVE BEFORE TRANSITION (player is still in safe position)
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("STAIR TRANSITION: Saved game before stairs");
        }

        //NOW BLOCK FURTHER SAVES
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetTransitioning(true);
            Debug.Log("STAIR TRANSITION: Blocking saves during transition");
        }

        if (charMovement != null)
            charMovement.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1f;
            player.position += Vector3.down * walkSpeed * Time.deltaTime;
            fadeScreen.alpha = t;
            yield return null;
        }

        SaveSystem.Instance.UnlockChapter(1);
        SceneManager.LoadScene(nextScene);
    }
}