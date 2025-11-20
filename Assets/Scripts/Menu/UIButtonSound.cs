using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;

    Button btn;
    void Awake()
    {
        btn = GetComponent<Button>();   
    }
    void OnEnable()
    {
        if (btn != null) btn.onClick.AddListener(PlayClick);    
    }
    void OnDisable()
    {
        if(btn != null) btn.onClick.RemoveListener(PlayClick);
    }
    void PlayClick()
    {
        UIAudioManager.Instance?.PlayOneShot(clickSound, volume);
    }
}
