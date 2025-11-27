using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float parallaxMultiplier = 0.5f;
    private Vector3 lastCamPos;
    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += delta * parallaxMultiplier;
        lastCamPos = cam.position;
    }
}
