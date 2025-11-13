using UnityEngine;

public class StairsTrigger : MonoBehaviour
{
    public FloorTransition floorTransitiion;
    public bool goingUp = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        SpriteRenderer playerRenderer = other.GetComponent<SpriteRenderer>();
        if(playerRenderer == null) return;

        if(goingUp)     //player goes up and appears above the stairs
        {
            playerRenderer.sortingLayerName = "Player";
            goingUp = false;
        }
        else            //player goes down and appears above stairs but below second floor 
        {
            playerRenderer.sortingLayerName = "BelowSecondFloor";
            goingUp= true;
        }
            floorTransitiion.TriggerTransition();
    }
}
