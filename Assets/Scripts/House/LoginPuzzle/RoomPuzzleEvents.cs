using UnityEngine;

public class RoomPuzzleEvents : MonoBehaviour
{
    [Header("Room Objects")]
    public GameObject doorToUnlock;
    public GameObject[] lightsToTurnOn;

    [Header("Audio")]
    public AudioSource completionSound;

    [Header("Particles")]
    public ParticleSystem sparkEffect;

    public void OnPuzzleSolved()
    {
        Debug.Log("Puzzle Solved! Executing events...");

        if (doorToUnlock != null)
            doorToUnlock.SetActive(false);

        foreach (var light in lightsToTurnOn)
            if (light != null)
                light.SetActive(true);

        if (completionSound != null)
            completionSound.Play();

        if (sparkEffect != null)
            sparkEffect.Play();
    }
}
