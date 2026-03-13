using UnityEngine;

//float crystal ball
public class CrystalBallFloat : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How many units up and down the ball moves")]
    public float floatAmplitude = 12f;

    [Tooltip("Speed of the up/down cycle")]
    public float floatSpeed = 1.2f;

    [Tooltip("Random time offset so it doesn't look synced with other animations")]
    public float timeOffset = 0f;

    private Vector3 startLocalPosition;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;

        //random offset
        if (timeOffset == 0f)
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float yOffset = Mathf.Sin((Time.unscaledTime + timeOffset) * floatSpeed) * floatAmplitude;
        transform.localPosition = startLocalPosition + new Vector3(0f, yOffset, 0f);
    }
}