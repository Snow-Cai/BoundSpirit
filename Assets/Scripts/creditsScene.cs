using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CreditsRoller : MonoBehaviour
{
    [Header("References")]
    public RectTransform creditsContainer;   //parent of all TMP text objects
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public float scrollSpeed = 60f;          //pixels per second
    public float fadeInDuration = 1.5f;
    public string returnScene = "MenuScene";

    [Header("Skip")]
    public KeyCode skipKey = KeyCode.Space;
    public KeyCode escapeKey = KeyCode.Escape;

    private bool skipping = false;

    private void Start()
    {
        StartCoroutine(RunCredits());
    }

    private void Update()
    {
        if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(escapeKey))
            skipping = true;
    }

    private IEnumerator RunCredits()
    {
        //wait two frames for ContentSizeFitter to finish calculating height
        yield return null;
        yield return null;

        //force layout rebuild so rect height is correct before read it
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContainer);

        //set start position after height is known
        float screenHeight = Screen.height;
        creditsContainer.anchoredPosition = new Vector2(0f, -screenHeight * 0.8f);

        //fade in
        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < fadeInDuration && !skipping)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        //scroll
        float totalHeight = creditsContainer.rect.height;
        Vector2 endPos = new Vector2(0f, totalHeight + screenHeight * 0.1f);

        while (creditsContainer.anchoredPosition.y < endPos.y && !skipping)
        {
            creditsContainer.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
            yield return null;
        }

        //fade out
        t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - t / 1.5f);
            yield return null;
        }

        PersistentCreditsAudio.CleanupAll();
        SceneManager.LoadScene(returnScene);
    }
}
