using UnityEngine;
using TMPro;
using System.Collections;

public class UICluePopup : MonoBehaviour
{
    public CanvasGroup popupCanvas;
    public TextMeshProUGUI clueText;
    public float fadeDuration = 0.4f;

    private Coroutine popupRoutine;
    private CharMovement movementScript; 
    private bool popupOpen = false;

    private void Awake()
    {
        
        if (popupCanvas != null)
        {
            popupCanvas.alpha = 0f;
            popupCanvas.blocksRaycasts = false;  
        }

        // Find player & movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            movementScript = player.GetComponent<CharMovement>();
    }

    public void ShowClue(string message)
    {
        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(PopupRoutine(message));
    }

    private IEnumerator PopupRoutine(string msg)
    {
        popupOpen = true;

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

        while (!Input.GetKeyDown(KeyCode.E))
            yield return null;

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            popupCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        popupCanvas.alpha = 0f;
        popupCanvas.blocksRaycasts = false;
        popupCanvas.gameObject.SetActive(true); 

        if (movementScript != null)
            movementScript.enabled = true;

        popupOpen = false;
        popupRoutine = null;
    }

    public bool IsPopupOpen()
    {
        return popupOpen;
    }
}
