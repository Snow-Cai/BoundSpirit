using UnityEngine;
using TMPro;
using System.Collections;

public class UICluePopup : MonoBehaviour
{
    public CanvasGroup popupCanvas;
    public TextMeshProUGUI clueText;
    public float fadeDuration = 0.4f;

    private bool closeRequested = false;
    private Coroutine popupRoutine;
    private CharMovement movementScript;
    private Rigidbody2D playerRigidbody;
    private bool popupOpen = false;

    private void Awake()
    {
        if (popupCanvas == null)
        {
            popupCanvas = GetComponent<CanvasGroup>();
        }

        if (popupCanvas != null)
        {
            popupCanvas.gameObject.SetActive(false);
            popupCanvas.alpha = 0f;
            popupCanvas.blocksRaycasts = false;
        }

        // Find player & movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            movementScript = player.GetComponent<CharMovement>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || popupCanvas == null || clueText == null)
            return;

        if (!popupCanvas.gameObject.activeSelf)
        {
            popupCanvas.gameObject.SetActive(true);
        }

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(PopupRoutine(message, null));
    }

    public void ShowTransientMessage(string message, float autoCloseDelay)
    {
        if (string.IsNullOrWhiteSpace(message) || popupCanvas == null || clueText == null)
            return;

        if (!popupCanvas.gameObject.activeSelf)
        {
            popupCanvas.gameObject.SetActive(true);
        }

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(PopupRoutine(message, autoCloseDelay));
    }

    public void ShowTidbit(InformationalTidbitData tidbit)
    {
        if (tidbit == null || !tidbit.HasContent())
            return;

        ShowTidbitMessage(tidbit.FormatForPopup());
    }

    public void ShowTidbitMessage(string message)
    {
        if (!AreInformationalTidbitsEnabled())
            return;

        ShowMessage(message);
    }

    public void ShowClue(string message)
    {
        ShowMessage(message);
    }

    private IEnumerator PopupRoutine(string msg, float? autoCloseDelay)
    {
        popupOpen = true;
        GameInputState.MovementLocked = true;
        StopPlayerImmediately();

        // Freeze player
        if (movementScript != null)
            movementScript.enabled = false;

        popupCanvas.gameObject.SetActive(true);
        popupCanvas.blocksRaycasts = true;
        clueText.text = msg;

        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            popupCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        popupCanvas.alpha = 1f;

        closeRequested = false;

        if (autoCloseDelay.HasValue)
        {
            float remaining = autoCloseDelay.Value;
            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            while (!closeRequested)
                yield return null;
        }

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            popupCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        popupCanvas.alpha = 0f;
        popupCanvas.blocksRaycasts = false;
        popupCanvas.gameObject.SetActive(false);

        if (movementScript != null)
            movementScript.enabled = true;

        GameInputState.MovementLocked = false;
        popupOpen = false;
        popupRoutine = null;
        closeRequested = false;
    }

    public bool IsPopupOpen()
    {
        return popupOpen;
    }

    private bool AreInformationalTidbitsEnabled()
    {
        return SettingsData.GetInformationalTidbitsEnabled();
    }

    private void StopPlayerImmediately()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }
    }

    public void ClosePopup()
    {
        closeRequested = true;
    }
    private void OnDisable()
    {
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }

        GameInputState.MovementLocked = false;
        popupOpen = false;
        popupRoutine = null;
    }
}
