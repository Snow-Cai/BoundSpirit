using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("Audio")]
    public AudioClip pauseSound;
    public AudioClip resumeSound;

    private bool isPaused = false;

    void Start()
    {
        //make sure pause menu is hidden at start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        //toggle pause with escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        Time.timeScale = 0f; //freeze game
        isPaused = true;

        //pause music
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PauseMusic();
        }

        //play pause sound
        if (pauseSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(pauseSound);
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        Time.timeScale = 1f; //unfreeze game
        isPaused = false;

        //resume music
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ResumeMusic();
        }

        //play resume sound
        if (resumeSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(resumeSound);
        }
    }

    public void QuitToMainMenu()
    {
        //unfreeze time before changing scenes
        Time.timeScale = 1f;
        isPaused = false;

        //play UI sound
        if (UIAudioManager.Instance != null && UIAudioManager.Instance.audioSource != null)
        {
            //get the click sound from a button if available
            UIButtonSound buttonSound = FindFirstObjectByType<UIButtonSound>();
            if (buttonSound != null && buttonSound.clickSound != null)
            {
                UIAudioManager.Instance.PlayOneShot(buttonSound.clickSound);
            }
        }

        //load main menu scene
        SceneManager.LoadScene("MenuScene");
    }

    public void QuitGame()
    {
        //play UI sound
        if (UIAudioManager.Instance != null && UIAudioManager.Instance.audioSource != null)
        {
            UIButtonSound buttonSound = FindFirstObjectByType<UIButtonSound>();
            if (buttonSound != null && buttonSound.clickSound != null)
            {
                UIAudioManager.Instance.PlayOneShot(buttonSound.clickSound);
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}