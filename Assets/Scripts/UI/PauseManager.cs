using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseMenuUI;

    /// <summary>Parent object for in-canvas pause settings (audio/graphics). Inactive until opened.</summary>
    [SerializeField] GameObject pauseSettingsRoot;

    [Header("Audio")]
    public AudioClip pauseSound;
    public AudioClip resumeSound;

    private bool isPaused = false;
    private bool pauseSettingsOpen = false;

    /// <summary>
    /// True when this component is the PauseCanvas instance (has menu + settings). Scenes may still contain an
    /// empty legacy PauseManager used only as an old button target; that object must not handle input or own state.
    /// </summary>
    bool IsPauseCanvasController => pauseMenuUI != null && pauseSettingsRoot != null;

    /// <summary>
    /// When the settings root is a child of the pause menu panel, we must not deactivate the whole pause panel
    /// to show settings — only hide the other pause UI children.
    /// </summary>
    bool SettingsRootIsUnderPauseMenu =>
        pauseMenuUI != null && pauseSettingsRoot != null &&
        pauseSettingsRoot.transform.IsChildOf(pauseMenuUI.transform);

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (IsPauseCanvasController)
            Instance = this;

        //make sure pause menu is hidden at start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (pauseSettingsRoot != null)
            pauseSettingsRoot.SetActive(false);
    }

    void Update()
    {
        if (Instance != null && Instance != this)
            return;

        //toggle pause with escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //DON'T ALLOW PAUSING DURING TRANSITIONS
            if (SaveSystem.Instance != null && SaveSystem.Instance.IsTransitioning())
            {
                Debug.Log("Cannot pause during scene transition");
                return;
            }

            if (isPaused && pauseSettingsOpen)
            {
                ClosePauseSettings();
                return;
            }

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
        pauseSettingsOpen = false;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            if (pauseSettingsRoot != null)
            {
                pauseSettingsRoot.SetActive(false);
                if (SettingsRootIsUnderPauseMenu)
                {
                    foreach (Transform child in pauseMenuUI.transform)
                    {
                        if (child.gameObject == pauseSettingsRoot)
                            continue;
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
        Time.timeScale = 0f; //freeze game
        isPaused = true;

        //SAVE THE GAME WHEN PAUSING
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("Game saved on pause");
        }

        //play pause sound
        if (pauseSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(pauseSound);
        }
    }

    public void Resume()
    {
        if (Instance != null && Instance != this)
        {
            Instance.Resume();
            return;
        }

        ClosePauseSettings();

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f; //unfreeze game
        isPaused = false;
        //play resume sound
        if (resumeSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(resumeSound);
        }
    }

    /// <summary>
    /// Opens pause settings (audio/graphics) from the same canvas as the pause menu.
    /// </summary>
    public void OpenPauseSettings()
    {
        if (Instance != null && Instance != this)
        {
            Instance.OpenPauseSettings();
            return;
        }

        if (!isPaused || pauseMenuUI == null || pauseSettingsOpen)
            return;

        if (pauseSettingsRoot == null)
        {
            Debug.LogError("PauseManager: Assign pauseSettingsRoot (settings parent on PauseCanvas).");
            return;
        }

        pauseSettingsOpen = true;

        if (SettingsRootIsUnderPauseMenu)
        {
            foreach (Transform child in pauseMenuUI.transform)
            {
                if (child.gameObject == pauseSettingsRoot)
                    continue;
                child.gameObject.SetActive(false);
            }
            pauseSettingsRoot.SetActive(true);
            pauseSettingsRoot.transform.SetAsLastSibling();
        }
        else
        {
            pauseMenuUI.SetActive(false);
            pauseSettingsRoot.SetActive(true);
            pauseSettingsRoot.transform.SetAsLastSibling();
        }

        var settingsManager = pauseSettingsRoot.GetComponentInChildren<SettingsManager>(true);
        settingsManager?.ShowMainSettings();
    }

    /// <summary>Leave settings and return to the pause menu (Resume / Quit / Esc / Back).</summary>
    public void ClosePauseSettings()
    {
        if (Instance != null && Instance != this)
        {
            Instance.ClosePauseSettings();
            return;
        }

        if (!pauseSettingsOpen)
            return;

        pauseSettingsOpen = false;
        if (pauseSettingsRoot != null)
            pauseSettingsRoot.SetActive(false);

        if (pauseMenuUI == null)
            return;

        if (SettingsRootIsUnderPauseMenu)
        {
            foreach (Transform child in pauseMenuUI.transform)
            {
                if (child.gameObject == pauseSettingsRoot)
                    continue;
                child.gameObject.SetActive(true);
            }
        }
        else
        {
            pauseMenuUI.SetActive(true);
        }
    }

    public void QuitToMainMenu()
    {
        if (Instance != null && Instance != this)
        {
            Instance.QuitToMainMenu();
            return;
        }

        if (pauseSettingsOpen)
            ClosePauseSettings();

        //unfreeze time BEFORE saving so physics/grounded state is valid
        Time.timeScale = 1f;
        isPaused = false;

        //SAVE BEFORE QUITTING (only if not transitioning)
        if (SaveSystem.Instance != null && !SaveSystem.Instance.IsTransitioning())
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("Game saved before returning to menu");
        }
        //play UI sound
        if (UIAudioManager.Instance != null && UIAudioManager.Instance.audioSource != null)
        {
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
        if (Instance != null && Instance != this)
        {
            Instance.QuitGame();
            return;
        }

        if (pauseSettingsOpen)
            ClosePauseSettings();

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