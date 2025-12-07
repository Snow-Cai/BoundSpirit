using UnityEngine;

public class PlayerPositionLoader : MonoBehaviour
{
    void Start()
    {
        Debug.Log("LOAD: PlayerPositionLoader started in scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Invoke("LoadPlayerPosition", 0.2f);
    }

    void LoadPlayerPosition()
    {
        Debug.Log("LOAD: Attempting to load player position...");

        if (SaveSystem.Instance == null)
        {
            Debug.LogError("LOAD: SaveSystem.Instance is NULL!");
            return;
        }

        if (!SaveSystem.Instance.HasSaveData())
        {
            Debug.Log("LOAD: No save data found");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("LOAD: No player found with 'Player' tag!");
            return;
        }

        Vector3 savedPosition = SaveSystem.Instance.GetPlayerPosition();
        Debug.Log("LOAD: Retrieved saved position: " + savedPosition);

        //SAFETY CHECK: Don't load if position is (0,0,0) likely bad save
        if (savedPosition == Vector3.zero)
        {
            Debug.LogWarning("LOAD: Saved position is (0,0,0) - not loading position");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log("LOAD: Disabled CharacterController");
        }

        //add small upward offset to prevent floor clipping
        savedPosition.y += 0.1f;

        player.transform.position = savedPosition;
        Debug.Log("LOAD: Player moved to: " + player.transform.position);

        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log("LOAD: Re-enabled CharacterController");
        }
    }
}