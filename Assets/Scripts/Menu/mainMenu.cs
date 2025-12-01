using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mainMenu : MonoBehaviour
{
    [Header("Button References (Optional)")]
    public Button continueButton;

    [Header("Popup References (Optional)")]
    public GameObject newGameWarningPopup;

    void Start()
    {
        // Update continue button if it exists
        if (continueButton != null && SaveSystem.Instance != null)
        {
            bool hasSave = SaveSystem.Instance.HasSaveData();
            continueButton.interactable = hasSave;

            // Make button look disabled if no save
            if (!hasSave)
            {
                var colors = continueButton.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                continueButton.colors = colors;
            }
        }
    }

    // For "New Game" or "Play Game" button
    public void PlayGame()
    {
        // Check if save exists and show warning
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            if (newGameWarningPopup != null)
            {
                newGameWarningPopup.SetActive(true);
                return; // Wait for user to confirm
            }
        }

        // No save exists, just start new game
        StartNewGame();
    }

    // For "Continue" button (if you add one)
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

    // Called by "Yes" button on warning popup
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

    // Called by "Cancel" button on warning popup
    public void CancelNewGame()
    {
        if (newGameWarningPopup != null)
        {
            newGameWarningPopup.SetActive(false);
        }
    }

    void StartNewGame()
    {
        // Load the next scene (cutscene or first chapter)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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