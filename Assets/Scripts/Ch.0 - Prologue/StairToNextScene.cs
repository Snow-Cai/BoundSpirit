using UnityEngine;
using UnityEngine.SceneManagement;

public class StairToNextScene : MonoBehaviour
{
    public CharMovement charMovement;
    public Transform player;
    public float walkSpeed = 2f;
    public CanvasGroup fadeScreen;
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;
        if (!collision.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(HandleTransition());
    }

    private System.Collections.IEnumerator HandleTransition()
    {
        //SAVE BEFORE TRANSITION (player is still in safe position)
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("TRANSITION: Saved game before transition");
        }

        //NOW BLOCK FURTHER SAVES
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetTransitioning(true);
            Debug.Log("TRANSITION: Blocking saves during transition");
        }

        if (charMovement != null)
            charMovement.enabled = false;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1f;
            if(currentIndex == 1)                       //If in graveyard scene go down stairs
                player.position += Vector3.down * walkSpeed * Time.deltaTime;
            fadeScreen.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene(nextIndex);
    }
}
