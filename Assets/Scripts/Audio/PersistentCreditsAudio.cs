using UnityEngine;

/// <summary>
/// Keeps specific AudioSources alive for the credits scene, then cleans them up afterward.
/// </summary>
public class PersistentCreditsAudio : MonoBehaviour
{
    private bool persistedForCredits;
    private AudioSource cachedSource;

    public static void Persist(AudioSource source)
    {
        if (source == null)
            return;

        PersistentCreditsAudio persistentAudio = source.GetComponent<PersistentCreditsAudio>();
        if (persistentAudio == null)
            persistentAudio = source.gameObject.AddComponent<PersistentCreditsAudio>();

        persistentAudio.PersistForCredits();
    }

    public static void CleanupAll()
    {
        PersistentCreditsAudio[] persistentAudios = FindObjectsByType<PersistentCreditsAudio>(FindObjectsSortMode.None);
        foreach (PersistentCreditsAudio persistentAudio in persistentAudios)
        {
            persistentAudio.Cleanup();
        }
    }

    private void Awake()
    {
        cachedSource = GetComponent<AudioSource>();
    }

    private void PersistForCredits()
    {
        if (persistedForCredits)
            return;

        cachedSource ??= GetComponent<AudioSource>();
        if (cachedSource == null)
            return;

        DontDestroyOnLoad(gameObject);
        persistedForCredits = true;
    }

    private void Cleanup()
    {
        if (!persistedForCredits)
            return;

        if (cachedSource != null)
            cachedSource.Stop();

        Destroy(gameObject);
    }
}
