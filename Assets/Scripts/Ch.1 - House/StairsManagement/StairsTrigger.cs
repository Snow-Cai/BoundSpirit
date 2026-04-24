using System.Collections;
using UnityEngine;

public class StairsTrigger : MonoBehaviour
{
    public StairsManager stairsManager;
    public bool goingUp = true;
    public float pushDistance = 1.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if(stairsManager.isTransitioning) return;
        if (goingUp && !stairsManager.CanGoUp())
        {
            stairsManager.TryPlayBlockedUpstairsDialogue();
            return;
        }

        SpriteRenderer playerRenderer = other.GetComponent<SpriteRenderer>();
        if (!playerRenderer) return;

        stairsManager.UseStairs(goingUp);       //call StairsManager to go up 

        if (!goingUp)
        {
            StartCoroutine(TemporarilyHidePlayerUnderFloor(playerRenderer));
        }

        Vector2 pushDir = goingUp ? Vector2.right : Vector2.left;       //nudge slightly to prevent re-triggering and getting stuck in a stair loop
        other.transform.position += (Vector3)pushDir * pushDistance;
    }

    private IEnumerator TemporarilyHidePlayerUnderFloor(SpriteRenderer playerRenderer)      //visual correction for player to appear under 2nd floor when moving downstairs
    {
        yield return new WaitForSeconds(0.05f);         //wait to move down a step
        playerRenderer.sortingOrder = -5;              //hide player under floor, but still above stairs
        yield return new WaitForSeconds(stairsManager.floorTransition.fadeDuration - 0.05f);
        playerRenderer.sortingOrder = 2;                //restore order after fade animation
    }
}
