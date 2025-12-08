using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Tracks")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip cutsceneMusic;
    public AudioClip winMusic;

    [Header("Audio Settings")]
    public float musicVolume = 0.5f;
    public float fadeSpeed = 1f;

    private AudioSource audioSource;
    private AudioClip currentClip;
    private float targetVolume;
    private bool isFading;

    void Awake()
    {
        //singleton pattern: keeps music playing between scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //setup audio source
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;
        targetVolume = musicVolume;
    }

    void Update()
    {
        //handle fade in/out
        if (isFading)
        {
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.unscaledDeltaTime);

            if (Mathf.Approximately(audioSource.volume, targetVolume))
            {
                isFading = false;

                //if fading out to 0, stop the audio
                if (targetVolume == 0)
                {
                    audioSource.Stop();
                    audioSource.volume = musicVolume;
                }
            }
        }
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null) return;

        //if same music is already playing, don't restart
        if (currentClip == clip && audioSource.isPlaying)
            return;

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
            audioSource.volume = musicVolume;
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

    public void PauseMusic()
    {
        audioSource.Pause();
    }

    public void ResumeMusic()
    {
        audioSource.UnPause();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (!isFading)
        {
            audioSource.volume = musicVolume;
        }
        targetVolume = musicVolume;
    }
}