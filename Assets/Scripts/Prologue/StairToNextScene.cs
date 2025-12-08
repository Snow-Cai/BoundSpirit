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
        if (!collision.CompareTag("Player")) return;        //check to ensure Player is the collider
        triggered = true;
        StartCoroutine(DoStairTransition());
    }

    private System.Collections.IEnumerator DoStairTransition()
    {
        if (charMovement != null)       //disable player movement
            charMovement.enabled = false;
        float t = 0f;
        while (t < 1f)      //fade screen to black as player is moved down stairs
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
