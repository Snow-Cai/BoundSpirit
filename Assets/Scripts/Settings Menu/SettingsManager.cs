using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public SettingsData settingsData;

    [Header("Panels")]
    public GameObject mainSettingsPanel;
    public GameObject audioSettingsPanel;
    public GameObject graphicsSettingsPanel;

    private void Start()
    {
        settingsData.Load();

        // Hide all panels at start
        mainSettingsPanel.SetActive(false);
        audioSettingsPanel.SetActive(false);
        graphicsSettingsPanel.SetActive(false);
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
