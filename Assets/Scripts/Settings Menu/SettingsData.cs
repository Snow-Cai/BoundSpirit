using UnityEngine;

[CreateAssetMenu(fileName = "SettingsData", menuName = "GameSettings/Settings Data")]
public class SettingsData : ScriptableObject
{
    public const string InformationalTidbitsEnabledKey = "InformationalTidbitsEnabled";

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.75f;
    [Range(0f, 1f)] public float musicVolume = 0.75f;
    [Range(0f, 1f)] public float sfxVolume = 0.75f;

    [Header("Graphics Settings")]
    public int resolutionIndex = 0;
    public bool isFullscreen = true;
    public bool vSyncEnabled = true;

    [Header("Gameplay Settings")]
    public bool informationalTidbitsEnabled = true;

    [Header("Menu State")]
    public bool openMain = true;
    public bool openAudio = false;
    public bool openGraphics = false;

    public void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("VSync", vSyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt(InformationalTidbitsEnabledKey, informationalTidbitsEnabled ? 1 : 0);

        PlayerPrefs.SetInt("MenuMain", openMain ? 1 : 0);
        PlayerPrefs.SetInt("MenuAudio", openAudio ? 1 : 0);
        PlayerPrefs.SetInt("MenuGraphics", openGraphics ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void Load()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);

        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionIndex);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", isFullscreen ? 1 : 0) == 1;
        vSyncEnabled = PlayerPrefs.GetInt("VSync", vSyncEnabled ? 1 : 0) == 1;
        informationalTidbitsEnabled = GetInformationalTidbitsEnabled(informationalTidbitsEnabled);

        openMain = PlayerPrefs.GetInt("MenuMain", openMain ? 1 : 0) == 1;
        openAudio = PlayerPrefs.GetInt("MenuAudio", openAudio ? 1 : 0) == 1;
        openGraphics = PlayerPrefs.GetInt("MenuGraphics", openGraphics ? 1 : 0) == 1;
    }

    public static bool GetInformationalTidbitsEnabled(bool defaultValue = true)
    {
        return PlayerPrefs.GetInt(InformationalTidbitsEnabledKey, defaultValue ? 1 : 0) == 1;
    }

    public static void SetInformationalTidbitsEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(InformationalTidbitsEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
