using UnityEngine;
using System.Collections;
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

    [Header("Popup References (Optional)")]
    public GameObject newGameWarningPopup;

    void Start()
    {
        //Update continue button if it exists
        if (continueButton != null && SaveSystem.Instance != null)
        {
            bool hasSave = SaveSystem.Instance.HasSaveData();
            continueButton.interactable = hasSave;

            //Make button look disabled if no save
            if (!hasSave)
            {
                var colors = continueButton.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                continueButton.colors = colors;
            }
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
    void UpdateChapterButtons()
    {
        if (SaveSystem.Instance == null) return;

        //Chapter 0 is always unlocked
        if (chapter0Button != null)
        {
            chapter0Button.interactable = true;
        }

        //Check if Chapter 1 is unlocked
        if (chapter1Button != null)
        {
            bool chapter1Unlocked = SaveSystem.Instance.IsChapterUnlocked(1);
            chapter1Button.interactable = chapter1Unlocked;

            if (!chapter1Unlocked)
            {
                var colors = chapter1Button.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                chapter1Button.colors = colors;
            }
        }

        //Check if Chapter 2 is unlocked
        if (chapter2Button != null)
        {
            bool chapter2Unlocked = SaveSystem.Instance.IsChapterUnlocked(2);
            chapter2Button.interactable = chapter2Unlocked;

            if (!chapter2Unlocked)
            {
                var colors = chapter2Button.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                chapter2Button.colors = colors;
            }
        }
    }

    //Chapter button methods
    public void LoadChapter0()
    {
        if (SaveSystem.Instance == null || SaveSystem.Instance.GetSaveData().currentChapter <= 0)
        {
            LoadChapter("Chapter0_Prologue", 0);
        }
        else
        {
            Debug.Log("Cannot go back to previous chapters!");
        }
    }

    public void LoadChapter1()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(1))
        {
            //Allow if current chapter is 0 (moving forward) or 1 (replaying current)
            if (SaveSystem.Instance.GetSaveData().currentChapter <= 1)
            {
                LoadChapter("Chapter1_Home", 1);
            }
            else
            {
                Debug.Log("Cannot go back to previous chapters!");
            }
        }
    }

    public void LoadChapter2()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsChapterUnlocked(2))
        {
            //Allow if current chapter is 1 (moving forward) or 2 (replaying current)
            if (SaveSystem.Instance.GetSaveData().currentChapter <= 2)
            {
                LoadChapter("Chapter2_TBD", 2);
            }
            else
            {
                Debug.Log("Cannot go back to previous chapters!");
            }
        }
    }

    void LoadChapter(string sceneName, int chapterNumber)
    {
        //set current chapter in the save system
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.GetSaveData().currentChapter = chapterNumber;
        }

        //Load scene by name
        SceneManager.LoadScene(sceneName);
    }

    //For Play Game button
    public void PlayGame()
    {
        //Check if save exists and show warning
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            if (newGameWarningPopup != null)
            {
                newGameWarningPopup.SetActive(true);
                return; //Wait for user to confirm
            }
        }

        //No save exists just start new game
        StartNewGame();
    }

    //For Continue button 
    public void ContinueGame()
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            SaveSystem.Instance.LoadGame();
            SaveSystem.Instance.LoadSavedScene();
        }
        else
        {
            Debug.Log("No save data found!");
        }
    }

    //Called by "Yes" button on warning popup
    public void ConfirmNewGame()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSave();
        }

        if (newGameWarningPopup != null)
        {
            newGameWarningPopup.SetActive(false);
        }

        StartNewGame();
    }

    //Called by "Cancel" button on warning popup
    public void CancelNewGame()
    {
        if (newGameWarningPopup != null)
        {
            newGameWarningPopup.SetActive(false);
        }
    }

    void StartNewGame()
    {
        //Load the next scene (first chapter)
        SceneManager.LoadScene("Chapter0_Prologue");
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