using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    /// <summary>Used by gameplay AudioSources and <see cref="SfxPlayback"/> so the SFX volume slider applies.</summary>
    public static AudioMixerGroup SharedSfxGroup { get; private set; }

    /// <summary>Call from gameplay bootstrap (e.g. SceneInitializer) when the menu was skipped so SFX routing still works.</summary>
    public static void RegisterSharedSfxGroup(AudioMixerGroup group)
    {
        if (group == null) return;
        SharedSfxGroup = group;
        RouteOrphanSourcesToSfxBus();
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public float defaultVolume = 0.7f;
    [Tooltip("Route UI/menu one-shots through the mixer so the SFX volume slider applies.")]
    public AudioMixerGroup sfxMixerGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        SharedSfxGroup = sfxMixerGroup;
        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.volume = defaultVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        RouteOrphanSourcesToSfxBus();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            SharedSfxGroup = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RouteOrphanSourcesToSfxBus();
        StartCoroutine(CoRouteOrphansAfterFrame());
    }

    IEnumerator CoRouteOrphansAfterFrame()
    {
        yield return null;
        RouteOrphanSourcesToSfxBus();
    }

    static void RouteOrphanSourcesToSfxBus()
    {
        if (SharedSfxGroup == null) return;

        foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (src.outputAudioMixerGroup != null) continue;
            if (src.GetComponent<MusicManager>() != null) continue;
            src.outputAudioMixerGroup = SharedSfxGroup;
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
