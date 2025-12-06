using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("References")]
    public SettingsData settings;
    public AudioMixer audioMixer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            ApplyAllSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // AUDIO
    public void SetMasterVolume(float v)
    {
        settings.masterVolume = v;
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(v) * 20f);
    }

    public void SetMusicVolume(float v)
    {
        settings.musicVolume = v;
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(v) * 20f);
    }

    public void SetSFXVolume(float v)
    {
        settings.sfxVolume = v;
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(v) * 20f);
    }

    // CONTROLS
    public void SetSensitivity(float v)
    {
        settings.moveSensitivity = v;
    }

    public void SetInvertY(bool b)
    {
        settings.invertYAxis = b;
    }

    // GRAPHICS
    public void SetResolution(int index)
    {
        settings.resolutionIndex = index;
        Resolution[] res = Screen.resolutions;

        Screen.SetResolution(res[index].width, res[index].height, settings.fullscreen);
    }

    public void SetQuality(int index)
    {
        settings.qualityIndex = index;
        QualitySettings.SetQualityLevel(index);
    }

    public void SetFullscreen(bool f)
    {
        settings.fullscreen = f;
        Screen.fullScreen = f;
    }

    // ACCESSIBILITY
    public void SetColorblindMode(bool b)
    {
        settings.colorblindMode = b;
        // to link to shader
    }

    public void SetSubtitlesEnabled(bool b)
    {
        settings.subtitlesEnabled = b;
    }

    public void SetSubtitleSize(int s)
    {
        settings.subtitleSize = s;
    }


    // APPLY ALL
    public void ApplyAllSettings()
    {
        SetMasterVolume(settings.masterVolume);
        SetMusicVolume(settings.musicVolume);
        SetSFXVolume(settings.sfxVolume);

        SetSensitivity(settings.moveSensitivity);
        SetInvertY(settings.invertYAxis);

        SetResolution(settings.resolutionIndex);
        SetQuality(settings.qualityIndex);
        SetFullscreen(settings.fullscreen);

        SetColorblindMode(settings.colorblindMode);
        SetSubtitlesEnabled(settings.subtitlesEnabled);
        SetSubtitleSize(settings.subtitleSize);
    }

    // SAVE / LOAD SETTINgs
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", settings.masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", settings.musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", settings.sfxVolume);

        PlayerPrefs.SetFloat("Sensitivity", settings.moveSensitivity);
        PlayerPrefs.SetInt("InvertY", settings.invertYAxis ? 1 : 0);

        PlayerPrefs.SetInt("ResIndex", settings.resolutionIndex);
        PlayerPrefs.SetInt("QualityIndex", settings.qualityIndex);
        PlayerPrefs.SetInt("Fullscreen", settings.fullscreen ? 1 : 0);

        PlayerPrefs.SetInt("Colorblind", settings.colorblindMode ? 1 : 0);
        PlayerPrefs.SetInt("Subtitles", settings.subtitlesEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SubtitleSize", settings.subtitleSize);

        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        settings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", settings.masterVolume);
        settings.musicVolume = PlayerPrefs.GetFloat("MusicVolume", settings.musicVolume);
        settings.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", settings.sfxVolume);

        settings.moveSensitivity = PlayerPrefs.GetFloat("Sensitivity", settings.moveSensitivity);
        settings.invertYAxis = PlayerPrefs.GetInt("InvertY", settings.invertYAxis ? 1 : 0) == 1;

        settings.resolutionIndex = PlayerPrefs.GetInt("ResIndex", settings.resolutionIndex);
        settings.qualityIndex = PlayerPrefs.GetInt("QualityIndex", settings.qualityIndex);
        settings.fullscreen = PlayerPrefs.GetInt("Fullscreen", settings.fullscreen ? 1 : 0) == 1;

        settings.colorblindMode = PlayerPrefs.GetInt("Colorblind", settings.colorblindMode ? 1 : 0) == 1;
        settings.subtitlesEnabled = PlayerPrefs.GetInt("Subtitles", settings.subtitlesEnabled ? 1 : 0) == 1;
        settings.subtitleSize = PlayerPrefs.GetInt("SubtitleSize", settings.subtitleSize);
    }
}

