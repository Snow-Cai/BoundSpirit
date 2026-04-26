using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mainMenu : MonoBehaviour
{
    [Header("Button References (Optional)")]
    public Button continueButton;

    [Header("Chapter Selection")]
    public Button chapter0Button;
    public Button chapter1Button;
    public Button chapter2Button;
    public Button chapter3Button;
    public Button chapter4Button;

    [Header("Chapter Scene Names")]
    public string chapter0Scene = "Chapter0_Prologue";
    public string chapter1Scene = "Chapter1_Home";
    public string chapter2Scene = "Chapter2_TBD";
    public string chapter3Scene = "Chapter3_TBD";
    public string chapter4Scene = "Chapter4_TBD";

    [Header("Popup References (Optional)")]
    public GameObject newGameWarningPopup;

    private static readonly Color LockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private static readonly Color UnlockedColor = Color.white;

    void Start()
    {
        if (continueButton != null && SaveSystem.Instance != null)
        {
            bool hasSave = SaveSystem.Instance.HasPlayableSaveData();
            continueButton.interactable = hasSave;
            if (!hasSave)
                SetButtonColor(continueButton, LockedColor);
        }

        UpdateChapterButtons();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Delete))
        {
            SaveSystem.Instance?.DeleteSave();
            Debug.Log("Save wiped");
            Start();
        }
#endif
    }

    public void UpdateChapterButtons()
    {
        if (SaveSystem.Instance == null) return;

        //Chapter 0 is always unlock
        SetChapterButton(chapter0Button, unlocked: true);

        //chapters 1-4 unlock once the player has completed the previous chapter
        SetChapterButton(chapter1Button, SaveSystem.Instance.IsChapterUnlocked(1));
        SetChapterButton(chapter2Button, SaveSystem.Instance.IsChapterUnlocked(2));
        SetChapterButton(chapter3Button, SaveSystem.Instance.IsChapterUnlocked(3));
        SetChapterButton(chapter4Button, SaveSystem.Instance.IsChapterUnlocked(4));
    }

    private void SetChapterButton(Button btn, bool unlocked)
    {
        if (btn == null) return;
        btn.interactable = unlocked;
        SetButtonColor(btn, unlocked ? UnlockedColor : LockedColor);
    }

    private static void SetButtonColor(Button btn, Color c)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = c;
        colors.disabledColor = c;
        btn.colors = colors;
    }

    private void LoadChapter(string sceneName, int chapterNumber)
    {
        if (SaveSystem.Instance != null)
        {
            SaveData data = SaveSystem.Instance.GetSaveData();

            //check whether the player is returning to the chapter they were just in (same scene name already saved) or jumping to a different one
            bool returningToSameChapter =
                !string.IsNullOrEmpty(data.currentScene) &&
                string.Equals(data.currentScene, sceneName, System.StringComparison.Ordinal);

            //update chapter tracker so progression logic stays correct
            data.currentChapter = chapterNumber;

            if (returningToSameChapter)
            {
                Debug.Log("Chapter select: returning to same chapter — keeping saved position.");
            }
            else
            {
                data.playerPosX = 0f;
                data.playerPosY = 0f;
                data.playerPosZ = 0f;
                data.currentScene = string.Empty; //force defaultSpawnPoint
                data.onSecondFloor = false;         //start on floor 1
                Debug.Log("Chapter select: switching chapter — using default spawn.");
            }

            TravelState.NextSpawnPointId = null;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadChapter0()
    {
        LoadChapter(chapter0Scene, 0);
    }

    public void LoadChapter1()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(1))
            LoadChapter(chapter1Scene, 1);
    }

    public void LoadChapter2()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(2))
            LoadChapter(chapter2Scene, 2);
    }

    public void LoadChapter3()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(3))
            LoadChapter(chapter3Scene, 3);
    }

    public void LoadChapter4()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(4))
            LoadChapter(chapter4Scene, 4);
    }

    public void PlayGame()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasPlayableSaveData())
        {
            if (newGameWarningPopup != null)
            {
                newGameWarningPopup.SetActive(true);
                return;
            }
        }

        StartNewGame();
    }

    public void ContinueGame()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasPlayableSaveData())
        {
            SaveSystem.Instance.LoadGame();
            SaveSystem.Instance.LoadSavedScene();
        }
        else
        {
            Debug.Log("No save data found!");
        }
    }

    public void ConfirmNewGame()
    {
        SaveSystem.Instance?.DeleteSave();

        if (newGameWarningPopup != null)
            newGameWarningPopup.SetActive(false);

        StartNewGame();
    }

    public void CancelNewGame()
    {
        if (newGameWarningPopup != null)
            newGameWarningPopup.SetActive(false);
    }

    void StartNewGame()
    {
        SaveSystem.Instance?.DeleteSave();
        SceneManager.LoadScene(chapter0Scene);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}