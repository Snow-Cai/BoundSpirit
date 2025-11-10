using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class BackgroundClickSound : MonoBehaviour, IPointerClickHandler
{
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 1f;
    //only plays if clicking on the panel and not a UI element
    public void OnPointerClick(PointerEventData eventData)
    {
        UIAudioManager.Instance?.PlayOneShot(clickClip, volume);
    }
}