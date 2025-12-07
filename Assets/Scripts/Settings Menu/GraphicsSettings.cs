using UnityEngine;
using UnityEngine.UI;  
using TMPro;            

public class GraphicsSettings : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] availableResolutions;

    private void Start()
    {
        LoadResolutions();
        LoadFullscreenState();
    }

    private void LoadResolutions()
    {
        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            string option = r.width + " x " + r.height;
            options.Add(option);

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadFullscreenState()
    {
        fullscreenToggle.isOn = Screen.fullScreen;
    }

    public void SetResolution(int index)
    {
        Resolution r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Debug.Log("Toggle sent value: " + isFullscreen);

        Screen.fullScreen = isFullscreen;

        Debug.Log("Fullscreen switched to: " + Screen.fullScreen);
    }

}
