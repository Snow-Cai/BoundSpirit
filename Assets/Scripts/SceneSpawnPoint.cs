using System.Collections;
using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId;
    [SerializeField] private bool isDefaultSpawn;

    private static bool hasPlacedPlayerThisScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasPlacedPlayerThisScene = false;
        TravelState.NextSpawnPointId = null;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (hasPlacedPlayerThisScene) yield break;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        bool useThisSpawn = false;

        if (!string.IsNullOrWhiteSpace(TravelState.NextSpawnPointId))
        {
            useThisSpawn = TravelState.NextSpawnPointId == spawnPointId;
        }
        else
        {
            useThisSpawn = isDefaultSpawn;
        }

        if (!useThisSpawn) yield break;

        player.transform.position = transform.position;
        hasPlacedPlayerThisScene = true;
        TravelState.NextSpawnPointId = null;
    }
}