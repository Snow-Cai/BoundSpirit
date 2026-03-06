using UnityEngine;

public class StationComputerScreenController : MonoBehaviour
{
    [Header("Animator")]
    public Animator screenAnimator;
    public string activateTrigger = "Activate";

    public void ActivateScreen()
    {
        if(screenAnimator != null)
            screenAnimator.SetTrigger(activateTrigger);
    }
}
