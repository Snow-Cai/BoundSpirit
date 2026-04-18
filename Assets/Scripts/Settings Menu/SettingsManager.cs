using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public SettingsData settingsData;

    [Header("Panels")]
    public GameObject mainSettingsPanel;
    public GameObject audioSettingsPanel;
    public GameObject graphicsSettingsPanel;

    void Awake()
    {
        // Must run in Awake, not Start: when the settings root is first activated (e.g. pause settings),
        // Start runs later in the frame *after* PauseManager calls ShowMainSettings(), which would
        // incorrectly hide all panels again.
        if (mainSettingsPanel != null)
            mainSettingsPanel.SetActive(false);
        if (audioSettingsPanel != null)
            audioSettingsPanel.SetActive(false);
        if (graphicsSettingsPanel != null)
            graphicsSettingsPanel.SetActive(false);
    }

    void Start()
    {
        settingsData.Load();
    }


    public void ShowMainSettings()
    {
        SetPanelState(true, false, false);
    }

    public void ShowAudioSettings()
    {
        SetPanelState(false, true, false);
    }

    public void ShowGraphicsSettings()
    {
        SetPanelState(false, false, true);
    }

    private void SetPanelState(bool main, bool audio, bool graphics)
    {
        settingsData.openMain = main;
        settingsData.openAudio = audio;
        settingsData.openGraphics = graphics;
        settingsData.Save();

        mainSettingsPanel.SetActive(main);
        audioSettingsPanel.SetActive(audio);
        graphicsSettingsPanel.SetActive(graphics);
    }

    public void CloseSettings()
    {
        // Hide the entire settings UI
        gameObject.SetActive(false);
        Time.timeScale = 1f; 
    }
}
