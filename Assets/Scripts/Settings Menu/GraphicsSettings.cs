using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  
using TMPro;            

public class GraphicsSettings : MonoBehaviour
{
    [Header("Data")]
    public SettingsData settingsData;

    [Header("UI Elements")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private readonly List<Resolution> uniqueResolutions = new List<Resolution>();

    private void Start()
    {
        if (settingsData != null)
        {
            settingsData.Load();
        }

        LoadResolutions();
        LoadFullscreenState();

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }
    }

    private void LoadResolutions()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("GraphicsSettings: resolutionDropdown is not assigned.");
            return;
        }

        uniqueResolutions.Clear();
        resolutionDropdown.ClearOptions();

        Resolution[] rawResolutions = Screen.resolutions;
        var options = new List<string>();

        // Keep unique width/height pairs, ignoring refresh rate duplicates.
        foreach (Resolution res in rawResolutions)
        {
            bool exists = uniqueResolutions.Exists(r => r.width == res.width && r.height == res.height);
            if (exists)
            {
                continue;
            }
            uniqueResolutions.Add(res);
            options.Add(res.width + " x " + res.height);
        }

        if (uniqueResolutions.Count == 0)
        {
            Debug.LogWarning("GraphicsSettings: No available resolutions detected.");
            return;
        }

        int selectedIndex = FindCurrentResolutionIndex();

        if (settingsData != null)
        {
            selectedIndex = Mathf.Clamp(settingsData.resolutionIndex, 0, uniqueResolutions.Count - 1);
            ApplyResolutionFromIndex(selectedIndex);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            Resolution res = uniqueResolutions[i];
            if (res.width == Screen.width && res.height == Screen.height)
            {
                return i;
            }
        }

        return 0;
    }

    private void LoadFullscreenState()
    {
        if (fullscreenToggle == null)
        {
            Debug.LogWarning("GraphicsSettings: fullscreenToggle is not assigned.");
            return;
        }

        bool isFullscreen = settingsData != null ? settingsData.isFullscreen : Screen.fullScreen;

        Screen.fullScreen = isFullscreen;
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
    }

    public void SetResolution(int index)
    {
        if (uniqueResolutions.Count == 0)
        {
            return;
        }

        int clamped = Mathf.Clamp(index, 0, uniqueResolutions.Count - 1);
        ApplyResolutionFromIndex(clamped);

        if (settingsData != null)
        {
            settingsData.resolutionIndex = clamped;
            settingsData.Save();
        }
    }

    private void ApplyResolutionFromIndex(int index)
    {
        Resolution resolution = uniqueResolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (settingsData != null)
        {
            settingsData.isFullscreen = isFullscreen;
            settingsData.Save();
        }
    }
}
