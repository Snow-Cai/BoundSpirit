using UnityEngine;
using System.Collections;

public class PrologueInventoryHintController : MonoBehaviour
{
    public static bool hasShownTutorial = false;

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
        yield return new WaitForSeconds(5f);
        tutorialPopup.SetActive(false);
    }
}
