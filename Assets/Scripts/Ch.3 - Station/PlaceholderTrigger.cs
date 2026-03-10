using UnityEngine;

public class PlaceholderTrigger : MonoBehaviour
{
    public AlarmEventController alarmEvent;

    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f;

    private Transform player;
    private Collider2D objectCollider;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        objectCollider = GetComponent<Collider2D>();
    }
    private void Update()
    {
        float distance = Vector3.Distance(
            objectCollider != null ? objectCollider.bounds.center : transform.position,
            player.position
        );


        if (distance <= interactionRange && Input.GetKeyDown(interactKey))
        {
            TriggerPuzzle();
        }
        
    }

    void TriggerPuzzle()
    {
        Debug.Log("Placeholder puzzle trigger activated.");
        if (alarmEvent != null)
            alarmEvent.TriggerAlarmEvent();
    }
}
