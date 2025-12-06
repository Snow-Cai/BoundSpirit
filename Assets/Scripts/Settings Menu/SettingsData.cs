using UnityEngine;

[CreateAssetMenu(fileName = "SettingsData", menuName = "Settings/Settings Data")]
public class SettingsData : ScriptableObject
{
    [Header("Audio")]
    [Range(0, 1)] public float masterVolume = 1f;
    [Range(0, 1)] public float musicVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;

    [Header("Controls")]
    public float moveSensitivity = 1f;
    public bool invertYAxis = false;

    [Header("Graphics")]
    public int resolutionIndex = 0;
    public int qualityIndex = 1;
    public bool fullscreen = true;

    [Header("Accessibility")]
    public bool colorblindMode = false;
    public bool subtitlesEnabled = true;
    public int subtitleSize = 16;

}
