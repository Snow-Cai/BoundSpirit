using UnityEngine;

public class StairsTrigger : MonoBehaviour
{
    public StairsManager stairsManager;
    public bool goingUp = true;
    public float pushDistance = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Transform player = other.transform;

        SpriteRenderer playerRenderer = other.GetComponent<SpriteRenderer>();
        if(playerRenderer == null) return;

        stairsManager.UseStairs(goingUp);
        if(stairsManager.isOnSecondFloor)     //player is on the second floor and should appear above the floor
        {
            playerRenderer.sortingLayerName = "Player";
        }
        else                                 //player should be appearing underneath the 2nd floor's floor when going down the stairs
        {
            playerRenderer.sortingLayerName = "BelowSecondFloor";
        }
        Vector2 pushDir = goingUp ? Vector2.right : Vector2.left;
        player.position += (Vector3)pushDir * pushDistance;
    }
}
