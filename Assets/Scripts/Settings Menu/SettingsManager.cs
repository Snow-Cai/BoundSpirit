using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public SettingsData settingsData;

    [Header("Panels")]
    public GameObject mainSettingsPanel;
    public GameObject audioSettingsPanel;
    public GameObject graphicsSettingsPanel;

    [Header("Gameplay Settings")]
    [SerializeField] private Toggle informationalTidbitsToggle;

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

        EnsureInformationalTidbitsToggleReference();
    }

    void Start()
    {
        if (settingsData != null)
        {
            settingsData.Load();
        }

        BindInformationalTidbitsToggle();
    }

    void OnEnable()
    {
        SyncInformationalTidbitsToggle();
    }

    void OnDestroy()
    {
        if (informationalTidbitsToggle != null)
        {
            informationalTidbitsToggle.onValueChanged.RemoveListener(SetInformationalTidbitsEnabled);
        }
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

    private void EnsureInformationalTidbitsToggleReference()
    {
        if (informationalTidbitsToggle != null)
            return;

        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            if (toggle != null && toggle.name == "InformationalTidbitsToggle")
            {
                informationalTidbitsToggle = toggle;
                break;
            }
        }
    }

    private void BindInformationalTidbitsToggle()
    {
        EnsureInformationalTidbitsToggleReference();

        if (informationalTidbitsToggle == null)
            return;

        informationalTidbitsToggle.onValueChanged.RemoveListener(SetInformationalTidbitsEnabled);
        informationalTidbitsToggle.onValueChanged.AddListener(SetInformationalTidbitsEnabled);
        SyncInformationalTidbitsToggle();
    }

    private void SyncInformationalTidbitsToggle()
    {
        EnsureInformationalTidbitsToggleReference();

        if (informationalTidbitsToggle == null)
            return;

        bool defaultValue = settingsData == null || settingsData.informationalTidbitsEnabled;
        bool isEnabled = SettingsData.GetInformationalTidbitsEnabled(defaultValue);
        informationalTidbitsToggle.SetIsOnWithoutNotify(isEnabled);
    }

    public void SetInformationalTidbitsEnabled(bool isEnabled)
    {
        SettingsData.SetInformationalTidbitsEnabled(isEnabled);

        if (settingsData != null)
        {
            settingsData.informationalTidbitsEnabled = isEnabled;
            settingsData.Save();
        }
    }
}
