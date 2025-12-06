using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SaveData
{
    //Player Info
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    //progress
    public int currentChapter;
    public string currentScene;

    //Puzzle Progress
    public bool safeUnlocked;
    public bool keyCollected;
    public bool laptopUnlocked;
    public List<string> collectedItems = new List<string>();
    public List<string> solvedPuzzles = new List<string>();

    //Dialogue Progress
    public List<string> viewedDialogues = new List<string>();
    public Dictionary<string, int> dialogueChoices = new Dictionary<string, int>();

    //Story Flags
    public bool knowsPlayerIsDead;
    public bool knowsNameIsAkila;
    public bool edenRevealed;
    public bool truthRevealed;

    //Timestamps
    public string lastSaveTime;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private SaveData currentSave;
    private const string SAVE_KEY = "GameSave";

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

    public void SaveGame()
    {
        if (currentSave == null)
        {
            currentSave = new SaveData();
        }

        //Save player position if player exists
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentSave.playerPosX = player.transform.position.x;
            currentSave.playerPosY = player.transform.position.y;
            currentSave.playerPosZ = player.transform.position.z;
        }

        //Save current scene
        currentSave.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        //Save timestamp
        currentSave.lastSaveTime = DateTime.Now.ToString();

        //Convert to JSON and save
        string json = JsonUtility.ToJson(currentSave);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("Game Saved!");
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            currentSave = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game Loaded!");
        }
        else
        {
            //Create new save if none exists
            currentSave = new SaveData();
            Debug.Log("No save found. Creating new save.");
        }
    }

    public void LoadSavedScene()
    {
        if (currentSave != null && !string.IsNullOrEmpty(currentSave.currentScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSave.currentScene);
        }
        else
        {
            //Default to first gameplay scene (skip cutscene)
            UnityEngine.SceneManagement.SceneManager.LoadScene(2); //may need to be adjusted
        }
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        currentSave = new SaveData();
        Debug.Log("Save deleted!");
    }

    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    //Getters and Setters for easy access
    public SaveData GetSaveData()
    {
        return currentSave;
    }

    public void SetPlayerPosition(Vector3 position)
    {
        currentSave.playerPosX = position.x;
        currentSave.playerPosY = position.y;
        currentSave.playerPosZ = position.z;
    }

    public Vector3 GetPlayerPosition()
    {
        return new Vector3(currentSave.playerPosX, currentSave.playerPosY, currentSave.playerPosZ);
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
        currentSave.dialogueChoices[dialogueID] = choiceIndex;
        SaveGame();
    }

    public int GetDialogueChoice(string dialogueID)
    {
        if (currentSave.dialogueChoices.ContainsKey(dialogueID))
        {
            return currentSave.dialogueChoices[dialogueID];
        }
        return -1;
    }
}