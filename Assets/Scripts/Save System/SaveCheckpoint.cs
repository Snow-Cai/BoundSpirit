using UnityEngine;

public class SaveCheckpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Save automatically when player enters this area")]
    public bool autoSave = true;

    [Header("Visual Feedback (Optional)")]
    public GameObject saveIndicator;

    private bool hasTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && autoSave && !hasTriggered)
        {
            SaveGame();
            hasTriggered = true;

            //Show visual feedback
            if (saveIndicator != null)
            {
                saveIndicator.SetActive(true);
                Invoke("HideIndicator", 2f);
            }
        }
    }

    void SaveGame()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("CHECKPOINT: Game saved at " + transform.position);
        }
    }

    void HideIndicator()
    {
        if (saveIndicator != null)
        {
            saveIndicator.SetActive(false);
        }
    }

    //Draw gizmo in editor to see checkpoint location
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}