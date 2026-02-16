using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class introController : MonoBehaviour
{
    [Header("Menu")]
    public CanvasGroup titleGroup;
    public CanvasGroup buttonsGroup;
    public GameObject firstSelected;

    [Header("Timing")]
    public float initialDelay = 1.0f;
    public float menuFadeIn = 0.6f;
    public KeyCode skipKey = KeyCode.Space;

    bool skipped = false;

    void Start()
    {
        if (titleGroup != null)
        {
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;
            titleGroup.interactable = false;
        }

        if (buttonsGroup != null) //Make sure starting state is correct
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = false;
        }
        StartCoroutine(RunIntro());
    }
    void Update()
    {
        if (!skipped && Input.GetKeyDown(skipKey))
            skipped = true;
    }

    IEnumerator RunIntro()
    {
        if (titleGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(titleGroup, 0f, 1f, menuFadeIn));
            titleGroup.blocksRaycasts = true;
            titleGroup.interactable = true;
        }
        else
            yield return null;


        float t = 0f;
        while(t < initialDelay && !skipped)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if(skipped)
        {
            ShowMenuInstant();
            yield break;
        }
        if (buttonsGroup != null)   //call reveal per button
        { 
            var reveal = buttonsGroup.GetComponent<MenuRevealController>();
            if (reveal != null)
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                reveal.RevealAfterMenu();
            }
            else //in case of failure, enable buttons immediately
            {
                buttonsGroup.interactable = true;
                buttonsGroup.blocksRaycasts = true;
                if(firstSelected != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }
    }

    void ShowMenuInstant()  //Skip to menu instantly
    {
        if (titleGroup == null) return;
        titleGroup.alpha = 1f;
        titleGroup.interactable = true;
        titleGroup.blocksRaycasts = true;
        if (buttonsGroup == null) return;
        buttonsGroup.alpha = 1f;
        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;
        var reveal = buttonsGroup.GetComponent<MenuRevealController>();
        if (reveal != null)
            reveal.ForceRevealInstant();
        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to,float duration)
    {
        if (cg == null)
            yield break;

        float elapsed = 0f;
        cg.alpha = from;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
