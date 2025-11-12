using UnityEngine;

public class StairsTrigger : MonoBehaviour
{
    public FloorTransition floorTransitiion;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            floorTransitiion.TriggerTransition();
        }
    }
}
