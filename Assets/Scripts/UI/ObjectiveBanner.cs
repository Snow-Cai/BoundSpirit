using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple global banner UI for showing short objective / hint messages.
/// Appears at the top of the screen, fades in, stays for a duration, then fades out.
/// </summary>
public class ObjectiveBanner : MonoBehaviour
{
    public static ObjectiveBanner Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageLabel;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float visibleDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Behavior")]
    [SerializeField] private bool queueMessages = true;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Shows an objective or hint message. If a banner is already visible:
    /// - If queueMessages is true, it will wait and then show this one.
    /// - Otherwise, it will interrupt and show the new message immediately.
    /// </summary>
    public void ShowMessage(string message)
    {
        if (canvasGroup == null || messageLabel == null)
        {
            return;
        }

        if (currentRoutine != null)
        {
            if (!queueMessages)
            {
                StopCoroutine(currentRoutine);
            }
            else
            {
                // Start a new routine that waits for the existing one to complete
                StartCoroutine(QueueMessageRoutine(message));
                return;
            }
        }

        currentRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator QueueMessageRoutine(string message)
    {
        while (currentRoutine != null)
        {
            yield return null;
        }

        currentRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        messageLabel.text = message;

        // Fade in
        yield return FadeTo(1f, fadeInDuration);

        // Stay visible
        float elapsed = 0f;
        while (elapsed < visibleDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return FadeTo(0f, fadeOutDuration);

        currentRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
