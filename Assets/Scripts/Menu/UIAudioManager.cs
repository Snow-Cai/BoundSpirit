using JetBrains.Annotations;
using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Audio")]
    public AudioSource audioSource;
    public float defaultVolume = 0.7f;
    void Awake()
    {
        
        if(Instance != null && Instance != this)
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
        audioSource.volume = defaultVolume;
    }
    
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
