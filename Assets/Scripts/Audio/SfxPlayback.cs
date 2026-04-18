using UnityEngine;

/// <summary>
/// World / gameplay one-shots that must respect the SFX mixer bus (unlike AudioSource.PlayClipAtPoint).
/// </summary>
public static class SfxPlayback
{
    public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("One shot audio");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.outputAudioMixerGroup = UIAudioManager.SharedSfxGroup;
        source.clip = clip;
        source.volume = volumeScale;
        source.Play();
        Object.Destroy(go, clip.length);
    }
}
