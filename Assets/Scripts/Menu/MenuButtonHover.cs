using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

//color transition
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Reference")]
    public TextMeshProUGUI buttonText;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color hoverColor = Color.white;

    [Header("Transition")]
    public float fadeDuration = 0.15f;

    private Coroutine colorRoutine;

    private void Awake()
    {
        //Auto find TMP text if need
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
            buttonText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(normalColor);
    }

    private void SetColor(Color target)
    {
        if (buttonText == null) return;

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(LerpColor(buttonText.color, target));
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            if (buttonText != null)
                buttonText.color = Color.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        if (buttonText != null)
            buttonText.color = to;

        colorRoutine = null;
    }

    //Reset to normal if button is disabled mid hover
    private void OnDisable()
    {
        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        if (buttonText != null)
            buttonText.color = normalColor;
    }
}