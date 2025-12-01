using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneInitializer : MonoBehaviour
{
    [Header("Scene Type")]
    [Tooltip("What type of scene is this?")]
    public SceneType sceneType = SceneType.Gameplay;

    [Header("Custom Music (Optional)")]
    [Tooltip("Leave empty to use default music for scene type")]
    public AudioClip customMusic;

    [Header("Fade Settings")]
    public bool fadeInMusic = true;

    [Header("Player Spawn")]
    public bool shouldLoadPlayerPosition = true;
    public Transform defaultSpawnPoint;

    public enum SceneType
    {
        MainMenu,
        Cutscene,
        Gameplay,
        Safe,
        Custom
    }

    void Start()
    {
        //Delay to ensure player is spawned first
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        InitializeScene();
    }

    void InitializeScene()
    {
        //Handle Music
        PlaySceneMusic();

        //Handle Player Position (for gameplay scenes)
        if (shouldLoadPlayerPosition && sceneType == SceneType.Gameplay || sceneType == SceneType.Safe)
        {
            LoadPlayerPosition();
        }

        //Auto-save when entering a new scene (optional)
        if (SaveSystem.Instance != null && sceneType != SceneType.MainMenu)
        {
            SaveSystem.Instance.SaveGame();
        }
    }

    void PlaySceneMusic()
    {
        if (MusicManager.Instance == null) return;

        AudioClip musicToPlay = null;

        //Determine which music to play
        if (customMusic != null)
        {
            //Use custom music if assigned
            musicToPlay = customMusic;
        }
        else
        {
            //Use default music based on scene type
            switch (sceneType)
            {
                case SceneType.MainMenu:
                    musicToPlay = MusicManager.Instance.mainMenuMusic;
                    break;

                case SceneType.Cutscene:
                    musicToPlay = MusicManager.Instance.cutsceneMusic;
                    break;

                case SceneType.Gameplay:
                    musicToPlay = MusicManager.Instance.gameplayMusic;
                    break;

                case SceneType.Safe:
                    //Safe scenes can use gameplay music or custom
                    musicToPlay = MusicManager.Instance.gameplayMusic;
                    break;

                case SceneType.Custom:
                    //Don't play anything unless customMusic is assigned
                    break;
            }
        }

        //Play music
        if (musicToPlay != null)
        {
            MusicManager.Instance.PlayMusic(musicToPlay, fadeInMusic);
        }
    }

    void LoadPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("No player found with 'Player' tag!");
            return;
        }

        //Check if we have saved position data
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            SaveData saveData = SaveSystem.Instance.GetSaveData();

            //Only load position if the saved scene matches current scene
            string currentScene = SceneManager.GetActiveScene().name;
            if (saveData.currentScene == currentScene)
            {
                Vector3 savedPosition = SaveSystem.Instance.GetPlayerPosition();

                //Check if position is not zero (default value)
                if (savedPosition != Vector3.zero)
                {
                    player.transform.position = savedPosition;
                    Debug.Log("Loaded player position: " + savedPosition);
                    return;
                }
            }
        }

        //If no save data or different scene, use default spawn point
        if (defaultSpawnPoint != null)
        {
            player.transform.position = defaultSpawnPoint.position;
            player.transform.rotation = defaultSpawnPoint.rotation;
            Debug.Log("Using default spawn point");
        }
    }

    //Optional:Save player position when leaving scene
    void OnDestroy()
    {
        if (sceneType == SceneType.Gameplay || sceneType == SceneType.Safe)
        {
            SavePlayerPosition();
        }
    }

    void SavePlayerPosition()
    {
        if (SaveSystem.Instance == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SaveSystem.Instance.SetPlayerPosition(player.transform.position);
            SaveSystem.Instance.SaveGame();
        }
    }
}
