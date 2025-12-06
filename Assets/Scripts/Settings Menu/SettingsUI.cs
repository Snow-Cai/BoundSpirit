using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Control Sliders")]
    public Slider sensitivitySlider;
    public Toggle invertYToggle;

    [Header("Graphics UI")]
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Toggle fullscreenToggle;

    [Header("Accessibility")]
    public Toggle colorblindToggle;
    public Toggle subtitlesToggle;
    public Slider subtitleSizeSlider;

    void Start()
    {
        LoadUI();
    }

    public void LoadUI()
    {
        masterSlider.value = SettingsManager.Instance.settings.masterVolume;
        musicSlider.value = SettingsManager.Instance.settings.musicVolume;
        sfxSlider.value = SettingsManager.Instance.settings.sfxVolume;

        sensitivitySlider.value = SettingsManager.Instance.settings.moveSensitivity;
        invertYToggle.isOn = SettingsManager.Instance.settings.invertYAxis;

        resolutionDropdown.value = SettingsManager.Instance.settings.resolutionIndex;
        qualityDropdown.value = SettingsManager.Instance.settings.qualityIndex;
        fullscreenToggle.isOn = SettingsManager.Instance.settings.fullscreen;

        colorblindToggle.isOn = SettingsManager.Instance.settings.colorblindMode;
        subtitlesToggle.isOn = SettingsManager.Instance.settings.subtitlesEnabled;
        subtitleSizeSlider.value = SettingsManager.Instance.settings.subtitleSize;
    }

    public void OnMasterVolume() => SettingsManager.Instance.SetMasterVolume(masterSlider.value);
    public void OnMusicVolume() => SettingsManager.Instance.SetMusicVolume(musicSlider.value);
    public void OnSFXVolume() => SettingsManager.Instance.SetSFXVolume(sfxSlider.value);

    public void OnSensitivity() => SettingsManager.Instance.SetSensitivity(sensitivitySlider.value);
    public void OnInvertY() => SettingsManager.Instance.SetInvertY(invertYToggle.isOn);

    public void OnResolution() => SettingsManager.Instance.SetResolution(resolutionDropdown.value);
    public void OnQuality() => SettingsManager.Instance.SetQuality(qualityDropdown.value);
    public void OnFullscreen() => SettingsManager.Instance.SetFullscreen(fullscreenToggle.isOn);

    public void OnColorblind() => SettingsManager.Instance.SetColorblindMode(colorblindToggle.isOn);
    public void OnSubtitles() => SettingsManager.Instance.SetSubtitlesEnabled(subtitlesToggle.isOn);
    public void OnSubtitleSize() => SettingsManager.Instance.SetSubtitleSize((int)subtitleSizeSlider.value);

    public void Save() => SettingsManager.Instance.SaveSettings();
}
