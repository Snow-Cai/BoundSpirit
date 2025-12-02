using UnityEngine;
using System.Collections;

public class StairsManager : MonoBehaviour
{
    public bool isOnSecondFloor = false;
    public bool isTransitioning = false;
    public Collider2D upStairCollider;
    public Collider2D downStairCollider;
    public FloorTransition floorTransition;

    public void UseStairs(bool goingUp)
    {
        if (isTransitioning) return;

        if ((goingUp && !isOnSecondFloor) || (!goingUp && isOnSecondFloor))
        {
            StartCoroutine(HandleTransition(goingUp));
        }
    }
    private IEnumerator HandleTransition(bool goingUp)      //handle transition with global lock to ensure player does not go back and forth on stairs
    {
        isTransitioning = true;
        floorTransition.TriggerTransition();
        isOnSecondFloor = goingUp;          //update floor state
        UpdateStairCollider();
        yield return new WaitForSeconds(floorTransition.fadeDuration);  //finish fade animation before ending transition
        isTransitioning = false;
    }

    void UpdateStairCollider()
    {
        if (upStairCollider != null)
        {
            upStairCollider.enabled = !isOnSecondFloor;
            downStairCollider.enabled = !isOnSecondFloor;
        }
    }
}
