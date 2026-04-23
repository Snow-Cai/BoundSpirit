using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneInitializer : MonoBehaviour
{
    [Header("Scene Type")]
    [Tooltip("What type of scene is this?")]
    public SceneType sceneType = SceneType.Gameplay;

    [Header("Audio (optional)")]
    [Tooltip("Assign the mixer's SFX group so ambient/world audio respects the SFX slider when the main menu (UIAudioManager) was never loaded.")]
    public AudioMixerGroup defaultSfxMixerGroup;

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

    void Awake()
    {
        if (defaultSfxMixerGroup != null && UIAudioManager.SharedSfxGroup == null)
            UIAudioManager.RegisterSharedSfxGroup(defaultSfxMixerGroup);
    }

    void Start()
    {
        //Reset transition flag when scene starts
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetTransitioning(false);
        }

        //Delay to ensure player is spawned first
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        InitializeScene();
    }

    void InitializeScene()
    {
        //Handle Music
        PlaySceneMusic();

        //Handle Player Position (for gameplay scenes)
        if (shouldLoadPlayerPosition && (sceneType == SceneType.Gameplay || sceneType == SceneType.Safe))
        {
            LoadPlayerPosition();
        }

        //Load player inventory
        LoadPlayerInventory();

        //REMOVED AUTO-SAVE We'll save manually at safe times only
        //Don't auto-save when entering scenes: this was causing the void spawning issue
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
            Debug.Log("Current scene: " + currentScene + " | Saved scene: " + saveData.currentScene);

            if (saveData.currentScene == currentScene)
            {
                Vector3 savedPosition = SaveSystem.Instance.GetPlayerPosition();

                //Check if position is not zero (default value) AND is valid (not in void)
                if (savedPosition != Vector3.zero && savedPosition.y > -50f)
                {
                    // Disable controller before moving
                    CharacterController controller = player.GetComponent<CharacterController>();
                    Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();

                    if (controller != null)
                    {
                        controller.enabled = false;
                    }
                    if (rb2d != null)
                    {
                        rb2d.linearVelocity = Vector2.zero; // Stop any movement
                    }

                    player.transform.position = savedPosition;
                    Debug.Log("Loaded player position: " + savedPosition);

                    // Re-enable controller
                    if (controller != null)
                    {
                        controller.enabled = true;
                    }
                    return;
                }
                else
                {
                    Debug.LogWarning("Saved position invalid: " + savedPosition + " - using default spawn");
                }
            }
            else
            {
                Debug.Log("Different scene detected - using default spawn point");
            }
        }

        //If no save data or different scene, use default spawn point
        if (defaultSpawnPoint != null)
        {
            player.transform.position = defaultSpawnPoint.position;
            player.transform.rotation = defaultSpawnPoint.rotation;
            Debug.Log("Using default spawn point at: " + defaultSpawnPoint.position);
        }
        else
        {
            Debug.LogError("No default spawn point set in SceneInitializer!");
        }
    }

    void LoadPlayerInventory()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No player found with 'Player' tag!");
            return;
        }

        if (SaveSystem.Instance == null || !SaveSystem.Instance.HasSaveData())
        {
            Debug.Log("No save data found. Inventory will be empty.");
            return;
        }

        SaveData saveData = SaveSystem.Instance.GetSaveData();
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Player is missing a PlayerInventory component!");
            return;
        }
        inventory.inventory.Clear();
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("ItemDatabase.Instance is null in scene: " +
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                " - inventory will not load!");
        }
        else
        {
            foreach (string itemID in saveData.collectedItems)
            {
                ItemData item = ItemDatabase.Instance.GetItemByID(itemID);
                if (item != null)
                    inventory.inventory.Add(item);
                else
                    Debug.LogWarning("Saved item ID not found in ItemDatabase: " + itemID);
            }
        }
        InventoryUI ui = FindFirstObjectByType<InventoryUI>();
        if (ui != null)
            ui.RefreshUI();
    }
}