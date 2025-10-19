using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.2f;    //camera smoothing

    private void LateUpdate()
    {
        if (player == null) return;     //avoid error if missing player
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);   //target camera position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);   //smooth camera movement
        transform.position = smoothedPosition;
    }
}
