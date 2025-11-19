using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
public class MenuRevealController : MonoBehaviour
{
    [Header("Button Reveal Order")]
    public List<GameObject> revealButtons = new List<GameObject>();

    [Header("Timing")]      //timing of fade-ins
    public float delayBetween = 0.08f;
    public float buttonFadeDuration = 0.18f;
    public float buttonScaleFrom = 0.85f;
    public float buttonScaleDuration = 0.16f;

    [Header("Interaction")]
    public CanvasGroup menuCanvasGroup;
    public GameObject firstSelected;

    bool skipped = false;

    public void RevealAfterMenu()
    {
        if (menuCanvasGroup != null)    //check menu not interactable yet
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.blocksRaycasts = false;
        }

        foreach (var go in revealButtons)   //prepare buttons scale
        {
            if (go == null) continue;
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            go.transform.localScale = Vector3.one * buttonScaleFrom;
        }
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine() //scaling and fade-in process for buttons
    {
        for (int i = 0; i < revealButtons.Count; i++)
        {
            var go = revealButtons[i];
            if (go == null) continue;
            CanvasGroup cg = go.GetComponent<CanvasGroup>();

            //animate scaling of buttons
            float t = 0f;
            float max = Mathf.Max(buttonFadeDuration, buttonScaleDuration);
            Vector3 startScale = Vector3.one * buttonScaleFrom;
            Vector3 targetScale = Vector3.one;

            while (t < max)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(t / buttonFadeDuration);
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / buttonScaleDuration));

                if (cg != null) cg.alpha = alpha;
                if (go != null) go.transform.localScale = Vector3.Lerp(startScale, targetScale, s);

                yield return null;
            }
            //ensure final states
            if(cg != null) cg.alpha = 1f;
            if (go != null) go.transform.localScale = targetScale;
            cg.blocksRaycasts = true;

            yield return new WaitForSecondsRealtime(delayBetween);
        }
        if(menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        if(firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void ForceRevealInstant()    //call if skipped to reveal buttons
    {
        if (skipped) return;
        skipped = true;

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        foreach (var go in revealButtons)       //reveal all buttons correctly
        {
            if (go == null) continue;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;     //ensures buttons are interactable once skipped
            cg.blocksRaycasts = true;
            go.transform.localScale = Vector3.one;
        }

        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

}
