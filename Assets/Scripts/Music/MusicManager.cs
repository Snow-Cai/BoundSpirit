using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Tracks")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip cutsceneMusic;
    public AudioClip winMusic;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f; // base volume
    public float fadeSpeed = 1f;

    [Header("Audio Mixer")]
    public AudioMixerGroup musicGroup; // assign Music mixer group (from AudioMixer, will later add Master and SFX routing).

    private AudioSource audioSource;
    private AudioClip currentClip;
    private float targetVolume;
    private bool isFading;

    private float sliderVolume = 1f; // slider multiplier (0-1)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (musicGroup != null)
            audioSource.outputAudioMixerGroup = musicGroup;

        targetVolume = musicVolume;
        audioSource.volume = musicVolume * sliderVolume;
    }

    void Update()
    {
        if (isFading)
        {
            // Multiply fade volume by slider value
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume * sliderVolume, fadeSpeed * Time.unscaledDeltaTime);

            if (Mathf.Approximately(audioSource.volume, targetVolume * sliderVolume))
            {
                isFading = false;

                if (targetVolume == 0)
                {
                    audioSource.Stop();
                    audioSource.volume = musicVolume * sliderVolume;
                }
            }
        }
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null) return;
        if (currentClip == clip && audioSource.isPlaying) return;

        currentClip = clip;

        if (fadeIn)
        {
            audioSource.volume = 0;
            audioSource.clip = clip;
            audioSource.Play();
            targetVolume = musicVolume;
            isFading = true;
        }
        else
        {
            audioSource.clip = clip;
            audioSource.volume = musicVolume * sliderVolume;
            audioSource.Play();
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (fadeOut)
        {
            targetVolume = 0;
            isFading = true;
        }
        else
        {
            audioSource.Stop();
        }
    }

    public void PauseMusic() => audioSource.Pause();
    public void ResumeMusic() => audioSource.UnPause();

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        targetVolume = musicVolume;
        if (!isFading)
            audioSource.volume = musicVolume * sliderVolume;
    }

//call from slider
    public void SetSliderVolume(float slider)
    {
        sliderVolume = Mathf.Clamp01(slider);
        targetVolume = musicVolume;
        // Apply immediately
        if (!isFading)
            audioSource.volume = musicVolume * sliderVolume;
    }
}
