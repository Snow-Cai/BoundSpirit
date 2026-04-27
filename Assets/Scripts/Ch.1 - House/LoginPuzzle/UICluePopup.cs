using UnityEngine;
using TMPro;
using System.Collections;

public class UICluePopup : MonoBehaviour
{
    public static UICluePopup Instance { get; private set; }

    public CanvasGroup popupCanvas;
    public TextMeshProUGUI clueText;
    public float fadeDuration = 0.4f;

    private bool closeRequested = false;
    private Coroutine popupRoutine;
    private CharMovement movementScript;
    private Rigidbody2D playerRigidbody;
    private bool popupOpen = false;
    private bool previousCanToggleInventory = true;
    private bool previousCanToggleJournal = true;
    private bool restoredInputToggles = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
        ApplyInputToggleLock();

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

        RestoreInputToggleLock();
        GameInputState.MovementLocked = false;
        popupOpen = false;
        popupRoutine = null;
        closeRequested = false;
    }

    public bool IsPopupOpen()
    {
        return popupOpen;
    }

    public bool IsBlockingHotkeys()
    {
        return popupCanvas != null &&
               popupCanvas.gameObject.activeInHierarchy &&
               popupCanvas.alpha > 0f;
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

    private void ApplyInputToggleLock()
    {
        if (InputLock.Instance == null || !restoredInputToggles)
            return;

        previousCanToggleInventory = InputLock.Instance.CanToggleInventory;
        previousCanToggleJournal = InputLock.Instance.CanToggleJournal;

        InputLock.Instance.CanToggleInventory = false;
        InputLock.Instance.CanToggleJournal = false;
        restoredInputToggles = false;
    }

    private void RestoreInputToggleLock()
    {
        if (InputLock.Instance == null || restoredInputToggles)
            return;

        bool popupInactive = popupCanvas == null ||
                             !popupCanvas.gameObject.activeInHierarchy;

        InputLock.Instance.CanToggleInventory = popupInactive ? true : previousCanToggleInventory;
        InputLock.Instance.CanToggleJournal = popupInactive ? true : previousCanToggleJournal;
        restoredInputToggles = true;
    }

    private void OnDisable()
    {
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }

        RestoreInputToggleLock();
        GameInputState.MovementLocked = false;
        popupOpen = false;
        popupRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
