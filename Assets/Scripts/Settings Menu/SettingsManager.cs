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
        SettingsData.InformationalTidbitsEnabledChanged += HandleInformationalTidbitsEnabledChanged;
        SyncInformationalTidbitsToggle();
    }

    void OnDisable()
    {
        SettingsData.InformationalTidbitsEnabledChanged -= HandleInformationalTidbitsEnabledChanged;
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

        informationalTidbitsToggle = FindInformationalTidbitsToggle(mainSettingsPanel);

        if (informationalTidbitsToggle == null)
            informationalTidbitsToggle = FindInformationalTidbitsToggle(audioSettingsPanel);

        if (informationalTidbitsToggle == null)
            informationalTidbitsToggle = FindInformationalTidbitsToggle(graphicsSettingsPanel);

        if (informationalTidbitsToggle == null)
        {
            Toggle[] toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Toggle toggle in toggles)
            {
                if (toggle != null && toggle.name == "InformationalTidbitsToggle")
                {
                    informationalTidbitsToggle = toggle;
                    break;
                }
            }
        }
    }

    private static Toggle FindInformationalTidbitsToggle(GameObject root)
    {
        if (root == null)
            return null;

        Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            if (toggle != null && toggle.name == "InformationalTidbitsToggle")
                return toggle;
        }

        return null;
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

    private void HandleInformationalTidbitsEnabledChanged(bool isEnabled)
    {
        if (informationalTidbitsToggle == null)
            return;

        informationalTidbitsToggle.SetIsOnWithoutNotify(isEnabled);

        if (settingsData != null)
        {
            settingsData.informationalTidbitsEnabled = isEnabled;
        }
    }
}
