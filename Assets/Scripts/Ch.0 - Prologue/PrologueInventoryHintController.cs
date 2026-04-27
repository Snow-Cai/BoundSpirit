using UnityEngine;
using System.Collections;

public class PrologueInventoryHintController : MonoBehaviour
{
    public static bool hasShownTutorial = false;
    public float timeShown = 5f;

    [SerializeField] private GameObject tutorialPopup;

    public void TryShowTutorial()
    {
        if (hasShownTutorial)
            return;
        hasShownTutorial = true;
        tutorialPopup.SetActive(true);
        StartCoroutine(Hide());
    }

    IEnumerator Hide()
    {
        yield return new WaitForSeconds(timeShown);
        tutorialPopup.SetActive(false);
    }
}
