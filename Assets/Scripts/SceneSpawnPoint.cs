using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId;
    [SerializeField] private bool isDefaultSpawn;

    private static bool hasPlacedPlayerThisScene;

    public static bool HasPlacedPlayerThisScene => hasPlacedPlayerThisScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasPlacedPlayerThisScene = false;
        TravelState.NextSpawnPointId = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasPlacedPlayerThisScene = false;
    }

    public static bool TryPlacePlayerAtPendingSpawn(GameObject player)
    {
        if (player == null || string.IsNullOrWhiteSpace(TravelState.NextSpawnPointId))
            return false;

        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint == null || TravelState.NextSpawnPointId != spawnPoint.spawnPointId)
                continue;

            spawnPoint.PlacePlayer(player);
            TravelState.NextSpawnPointId = null;
            return true;
        }

        Debug.LogWarning("Requested spawn point was not found: " + TravelState.NextSpawnPointId);
        TravelState.NextSpawnPointId = null;
        return false;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (hasPlacedPlayerThisScene) yield break;
        if (string.IsNullOrWhiteSpace(TravelState.NextSpawnPointId)) yield break;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        if (TravelState.NextSpawnPointId != spawnPointId) yield break;

        PlacePlayer(player);
        TravelState.NextSpawnPointId = null;
    }

    private void PlacePlayer(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();

        if (controller != null)
            controller.enabled = false;

        if (rb2d != null)
            rb2d.linearVelocity = Vector2.zero;

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;

        if (controller != null)
            controller.enabled = true;

        hasPlacedPlayerThisScene = true;
        Debug.Log("Placed player at scene spawn point: " + spawnPointId);
    }
}
