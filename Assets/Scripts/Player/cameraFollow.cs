using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.2f;    //camera smoothing
    public float pixelsPerUnit = 32f; //PPU for camera position snapping

    private void FixedUpdate()
    {
        if (player == null) return;     //avoid error if missing player
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);   //target camera position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);   //smooth camera movement
        //Snap camera to pixel grid
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * pixelsPerUnit) / pixelsPerUnit;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * pixelsPerUnit) / pixelsPerUnit;
        transform.position = smoothedPosition;
    }
}
