using UnityEngine;

public class UIPromptAnimator : MonoBehaviour
{
    CanvasGroup canvasGroup;
    RectTransform rect;

    public float fadeSpeed = 5f;
    public float bounceSpeed = 3f;
    public float bounceAmount = 0.1f;

    float targetAlpha = 0f;
    Vector2 originalPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = GetComponent<RectTransform>();
        originalPosition = rect.anchoredPosition;
    }

    void Update()
    {
        // fade
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // bounce (when popup is visible)
        if (targetAlpha > 0.1f)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
            rect.anchoredPosition = originalPosition + new Vector2(0, bounce);
        }
        else
        {
            rect.anchoredPosition = originalPosition;
        }
    }

    public void Show()
    {
        targetAlpha = 1f;
    }

    public void Hide()
    {
        targetAlpha = 0f;
    }
}
