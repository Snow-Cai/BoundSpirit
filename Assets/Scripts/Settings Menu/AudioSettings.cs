using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public SettingsData settingsData;
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        settingsData.Load();

        masterSlider.value = settingsData.masterVolume;
        musicSlider.value = settingsData.musicVolume;
        sfxSlider.value = settingsData.sfxVolume;

        // Apply initial values
        ApplyMasterVolume(masterSlider.value);
        ApplyMusicVolume(musicSlider.value);
        ApplySFXVolume(sfxSlider.value);

        // Hook sliders
        masterSlider.onValueChanged.AddListener(ApplyMasterVolume);
        musicSlider.onValueChanged.AddListener(ApplyMusicVolume);
        sfxSlider.onValueChanged.AddListener(ApplySFXVolume);
    }

    public void ApplyMasterVolume(float value)
    {
        settingsData.masterVolume = value;
        settingsData.Save();
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
    }

    public void ApplyMusicVolume(float value)
    {
        settingsData.musicVolume = value;
        settingsData.Save();

        // Update MusicManager
        MusicManager.Instance.SetSliderVolume(value);
    }


    public void ApplySFXVolume(float value)
    {
        settingsData.sfxVolume = value;
        settingsData.Save();
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
    }
}
