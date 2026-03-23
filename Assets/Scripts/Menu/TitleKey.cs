using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
//handles the special B key easter egg
public class TitleKey : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Key Identity")]
    public char keyLetter = 'B';
    public bool isBKey = false;             //tick on the B key

    [Header("Easter Egg - B Key Only")]
    public Sprite fKeySprite;               //drag F key PNG here on the B key

    [Header("References")]
    public TitleScreenController titleController;
    public AudioClip clickSound;

    [Header("Animation")]
    public float pressDepth = 6f;
    public float pressDuration = 0.08f;
    public float returnDuration = 0.12f;

    [Header("Tint Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.65f, 0.2f, 1f);      //warm orange hover
    public Color pressColor = new Color(1f, 0.4f, 0f, 1f);         //deep orange press
    public Color easterEggColor = new Color(1f, 0.35f, 0f, 1f);    //locked orange after swap

    private Image keyImage;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine pressRoutine;
    private bool easterEggTriggered = false;

    private void Awake()
    {
        keyImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (keyImage != null)
            keyImage.color = normalColor;
    }

    public void CapturePosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (easterEggTriggered) return;
        if (keyImage != null)
            keyImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (easterEggTriggered) return;
        if (keyImage != null)
            keyImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //play click sound
        if (clickSound != null && UIAudioManager.Instance != null)
            UIAudioManager.Instance.PlayOneShot(clickSound);

        //animate press
        if (pressRoutine != null)
            StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(PressAnimation());

        //B key easter egg swap sprite to F key
        if (isBKey && !easterEggTriggered)
        {
            easterEggTriggered = true;

            if (fKeySprite != null && keyImage != null)
                keyImage.sprite = fKeySprite;

            if (keyImage != null)
                keyImage.color = easterEggColor;
        }
    }

    private IEnumerator PressAnimation()
    {
        if (keyImage != null && !easterEggTriggered)
            keyImage.color = pressColor;

        float elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / pressDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(
                originalPosition,
                originalPosition + Vector2.down * pressDepth,
                t
            );
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition + Vector2.down * pressDepth;

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / returnDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(
                originalPosition + Vector2.down * pressDepth,
                originalPosition,
                t
            );
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;

        if (keyImage != null && !easterEggTriggered)
            keyImage.color = normalColor;

        pressRoutine = null;
    }
}