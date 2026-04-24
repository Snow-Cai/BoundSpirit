using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SaveData;

[System.Serializable]
public class SaveData
{
    //Player Info
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public bool onSecondFloor = false;  


    //progress
    public int currentChapter;
    public string currentScene;

    //chapter unlocked
    public int highestChapterUnlocked = 0;

    //Puzzle Progress
    public bool safeUnlocked;
    public bool keyCollected;
    public bool laptopUnlocked;
    public List<string> collectedItems = new List<string>();
    public List<string> solvedPuzzles = new List<string>();

    //Dialogue Progress
    public List<string> viewedDialogues = new List<string>();
    [System.Serializable]
    public class DialogueChoiceEntry
    {
        public string dialogueID;
        public int choiceIndex;
    }
    public List<DialogueChoiceEntry> dialogueChoices = new List<DialogueChoiceEntry>();
    //public Dictionary<string, int> dialogueChoices = new Dictionary<string, int>();

    //Story Flags
    public bool knowsPlayerIsDead;
    public bool knowsNameIsAkila;
    public bool edenRevealed;
    public bool truthRevealed;
    public bool foundMenuSecret;
    public bool foundHiddenTombstone;

    //Timestamps
    public string lastSaveTime;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private SaveData currentSave;
    private const string SAVE_KEY = "GameSave";
    private const string MENU_SECRET_KEY = "FoundMenuSecret";
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Try to load existing save
        LoadGame();
    }

    public void SetTransitioning(bool transitioning)
    {
        isTransitioning = transitioning;
        Debug.Log("SAVE: Transition state: " + transitioning);
    }
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public void SaveGame()
    {
        //Don't save during scene transitions
        if (isTransitioning)
        {
            Debug.LogWarning("SAVE: Blocked - currently transitioning scenes");
            return;
        }

        if (currentSave == null)
        {
            currentSave = new SaveData();
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(activeSceneName, "MenuScene", StringComparison.Ordinal))
        {
            Debug.Log("SAVE: Skipping save in MenuScene.");
            return;
        }

        //Save player position if player exists
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;

            //SAFETY CHECK 1: Make sure player is grounded
            CharacterController controller = player.GetComponent<CharacterController>();
            Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();

            bool isGrounded = false;

            if (controller != null)
            {
                isGrounded = controller.isGrounded;
            }
            else if (rb2d != null)
            {
                //cause 2d check if player is on ground using raycast
                RaycastHit2D hit = Physics2D.Raycast(
                    player.transform.position - new Vector3(0, 0.4f, 0),
                    Vector2.down,
                    0.3f
                );
                isGrounded = hit.collider != null;
                Debug.Log("SAVE: 2D Grounded check: " + isGrounded);
            }
            else
            {
                //No controller or rb2d, assume grounded
                isGrounded = true;
            }

            //SAFETY CHECK 2: Don't save if position seems invalid (falling into void)
            bool validPosition = pos.y > -50f; // Adjust based on map bounds

            if (isGrounded && validPosition)
            {
                currentSave.playerPosX = pos.x;
                currentSave.playerPosY = pos.y;
                currentSave.playerPosZ = pos.z;
                Debug.Log("SAVE: Safe position saved: " + pos);
            }
            else
            {
                Debug.LogWarning("SAVE: Unsafe position detected - keeping previous save. Current: " + pos + " Grounded: " + isGrounded + " Valid: " + validPosition);
            }
        }
        else
        {
            Debug.LogError("SAVE: No player found!");
        }

        //Save player's inventory (only if player exists)
        if (player != null)
        {
            PlayerInventory inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                currentSave.collectedItems = inv.GetInventoryItemIDs();
                Debug.Log("SAVE: Inventory has been saved (" + currentSave.collectedItems.Count + " items)");
            }
            else
            {
                Debug.LogWarning("SAVE: Player has no PlayerInventory component!");
            }
        }

        //Save current scene
        currentSave.currentScene = activeSceneName;
        Debug.Log("SAVE: Scene saved: " + currentSave.currentScene);

        //Save timestamp
        currentSave.lastSaveTime = DateTime.Now.ToString();

        //Convert to JSON and save
        string json = JsonUtility.ToJson(currentSave);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("SAVE: Complete! JSON: " + json);
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            currentSave = JsonUtility.FromJson<SaveData>(json);
            currentSave.foundMenuSecret = currentSave.foundMenuSecret || PlayerPrefs.GetInt(MENU_SECRET_KEY, 0) == 1;
            Debug.Log("Game Loaded!");
        }
        else
        {
            //Create new save if none exists
            currentSave = new SaveData();
            currentSave.foundMenuSecret = PlayerPrefs.GetInt(MENU_SECRET_KEY, 0) == 1;
            Debug.Log("No save found. Creating new save.");
        }
    }

    public void LoadSavedScene()
    {
        if (HasPlayableSaveData())
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSave.currentScene);
            //after scene loads, restore game state
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.LogWarning("SAVE: No playable save scene found. Starting a new game instead.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Chapter0_Prologue");
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        //unsubscribe so this only runs once
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        // Reset transition flag after scene loads
        isTransitioning = false;

        //restore collected items
        RestoreCollectedItems();

        //restore puzzle states
        RestorePuzzleStates();
    }

    private void RestoreCollectedItems()
    {
        if (currentSave == null) return;
        //Deactivate items already collected from the world
        GameObject mapGo = GameObject.Find("Map");
        if (mapGo == null)
        {
            Debug.LogWarning("SAVE: Map GameObject not found — skipped hiding world collectibles.");
        }
        else
        {
            Transform map = mapGo.transform;
        foreach (Transform floor in map)
        {
            Transform itemsParent = floor.Find("CollectibleItemsParent");
            if (itemsParent == null) continue;
            Transform[] items = itemsParent.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in items)
            {
                CollectibleObject co = item.GetComponent<CollectibleObject>();
                string id = co != null ? co.item.itemID : item.name;
                if (currentSave.collectedItems.Contains(id))
                {
                    if (co != null && co.disappearOnPickup)
                    {
                        item.gameObject.SetActive(false);
                    }

                    Debug.Log("Restored collected item: " + id);
                }
            }
        }
        }

        //Restore player's inventory
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerInventory inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.LoadInventoryFromIDs(currentSave.collectedItems);
                Debug.Log("RESTORE: Player inventory restored! " + currentSave.collectedItems.Count + " items restored.");
            }
            else
            {
                Debug.LogWarning("RESTORE: Player object not found!");
            }
        }
    }

    /// <summary>
    /// Syncs graveyard gate visuals with save after runtime save edits (e.g. dev progress shortcuts).
    /// Does not hide world collectibles or puzzle objects — use a scene reload for full restore.
    /// </summary>
    public void ApplySaveToLoadedScene()
    {
        if (currentSave == null)
        {
            return;
        }

        GraveyardGateController[] gates = UnityEngine.Object.FindObjectsByType<GraveyardGateController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] != null)
            {
                gates[i].SyncUnlockedStateWithSave();
            }
        }
    }

    private void RestorePuzzleStates()
    {
        if (currentSave == null) return;

        InteractableObject[] interactables = FindObjectsOfType<InteractableObject>(true);
        foreach (var obj in interactables)
        {
            if (!string.IsNullOrEmpty(obj.puzzleID) && IsPuzzleSolved(obj.puzzleID))
            {
                if (obj.allowSolvedPuzzleReopen)
                {
                    continue;
                }

                obj.gameObject.SetActive(false);
                Debug.Log("Restored solved puzzle: " + obj.puzzleID);
            }
        }
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(MENU_SECRET_KEY);
        PlayerPrefs.Save();
        currentSave = new SaveData();
        Debug.Log("Save deleted!");
    }

    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    public bool HasPlayableSaveData()
    {
        if (!HasSaveData())
            return false;

        if (currentSave == null)
            LoadGame();

        if (currentSave == null || string.IsNullOrWhiteSpace(currentSave.currentScene))
            return false;

        if (string.Equals(currentSave.currentScene, "MenuScene", StringComparison.Ordinal))
            return false;

        return Application.CanStreamedLevelBeLoaded(currentSave.currentScene);
    }

    //Getters and Setters for easy access
    public SaveData GetSaveData()
    {
        return currentSave;
    }

    public Vector3 GetPlayerPosition()
    {
        return new Vector3(currentSave.playerPosX, currentSave.playerPosY, currentSave.playerPosZ);
    }

    public void SetPlayerPosition(Vector3 position)
    {
        if (currentSave != null)
        {
            currentSave.playerPosX = position.x;
            currentSave.playerPosY = position.y;
            currentSave.playerPosZ = position.z;
        }
    }

    public void SetCurrentChapter(int chapter)
    {
        currentSave.currentChapter = chapter;
        SaveGame();
    }

    public void UnlockPuzzle(string puzzleName)
    {
        if (!currentSave.solvedPuzzles.Contains(puzzleName))
        {
            currentSave.solvedPuzzles.Add(puzzleName);
            SaveGame();
        }
    }

    public bool IsPuzzleSolved(string puzzleName)
    {
        return currentSave.solvedPuzzles.Contains(puzzleName);
    }

    public void CollectItem(string itemName)
    {
        if (!currentSave.collectedItems.Contains(itemName))
        {
            currentSave.collectedItems.Add(itemName);
            SaveGame();
        }
    }

    public bool HasItem(string itemName)
    {
        return currentSave.collectedItems.Contains(itemName);
    }

    public void MarkDialogueViewed(string dialogueID)
    {
        if (!currentSave.viewedDialogues.Contains(dialogueID))
        {
            currentSave.viewedDialogues.Add(dialogueID);
            SaveGame();
        }
    }

    public bool HasViewedDialogue(string dialogueID)
    {
        return currentSave.viewedDialogues.Contains(dialogueID);
    }

    public void SaveDialogueChoice(string dialogueID, int choiceIndex)
    {
        var existing = currentSave.dialogueChoices.Find(e => e.dialogueID == dialogueID);
        if (existing != null)
            existing.choiceIndex = choiceIndex;
        else
            currentSave.dialogueChoices.Add(new DialogueChoiceEntry { dialogueID = dialogueID, choiceIndex = choiceIndex });
        SaveGame();
    }

    public int GetDialogueChoice(string dialogueID)
    {
        var existing = currentSave.dialogueChoices.Find(e => e.dialogueID == dialogueID);
        return existing != null ? existing.choiceIndex : -1;
    }

    public bool IsChapterUnlocked(int chapterNumber)
    {
        if (currentSave == null)
        {
            return chapterNumber == 0; //only chapter 0 if no save
        }

        //a chapter is unlocked if it's <= the highest unlocked chapter
        return chapterNumber <= currentSave.highestChapterUnlocked;
    }

    public void UnlockChapter(int chapterNumber)
    {
        if (currentSave == null)
        {
            currentSave = new SaveData();
        }

        //only update if this chapter is higher than what's currently unlocked
        if (chapterNumber > currentSave.highestChapterUnlocked)
        {
            currentSave.highestChapterUnlocked = chapterNumber;
            SaveGame(); //save immediately when unlocking
            Debug.Log("Chapter " + chapterNumber + " unlocked!");
        }
    }

    public int GetHighestUnlockedChapter()
    {
        if (currentSave == null)
        {
            return 0;
        }
        return currentSave.highestChapterUnlocked;
    }

    void OnApplicationQuit()
    {
        if (CanSaveCurrentSceneOnQuit())
        {
            SaveGame();
            Debug.Log("SAVE: Game saved on application quit!");
        }
        else
        {
            Debug.Log("SAVE: Skipped save on application quit.");
        }
    }

    private bool CanSaveCurrentSceneOnQuit()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return false;

        return !string.Equals(activeScene.name, "MenuScene", StringComparison.Ordinal);
    }

    public bool KnowsNameIsAkila()
    {
        return currentSave != null && currentSave.knowsNameIsAkila;
    }

    public void SetKnowsNameIsAkila(bool value = true)
    {
        if (currentSave == null) currentSave = new SaveData();
        currentSave.knowsNameIsAkila = value;
        SaveGame();
    }

    public bool FoundHiddenTombstone()
    {
        return currentSave != null && currentSave.foundHiddenTombstone;
    }

    public bool FoundMenuSecret()
    {
        return (currentSave != null && currentSave.foundMenuSecret) ||
               PlayerPrefs.GetInt(MENU_SECRET_KEY, 0) == 1;
    }

    public void SetFoundMenuSecret(bool value = true)
    {
        if (currentSave == null) currentSave = new SaveData();

        bool persistedValue = PlayerPrefs.GetInt(MENU_SECRET_KEY, 0) == 1;
        if (currentSave.foundMenuSecret == value && persistedValue == value)
            return;

        currentSave.foundMenuSecret = value;
        PlayerPrefs.SetInt(MENU_SECRET_KEY, value ? 1 : 0);
        PlayerPrefs.Save();
        SaveGame();
    }

    public void SetFoundHiddenTombstone(bool value = true)
    {
        if (currentSave == null) currentSave = new SaveData();

        if (currentSave.foundHiddenTombstone == value)
            return;

        currentSave.foundHiddenTombstone = value;
        SaveGame();
    }

}
