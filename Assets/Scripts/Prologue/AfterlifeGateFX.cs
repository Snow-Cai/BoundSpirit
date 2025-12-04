using UnityEngine;

public class AfterlifeGateFX : MonoBehaviour
{
    public float amplitude = 0.3f;
    public float frequency = 1f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }
    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;       //vertical floating effect
        transform.position = startPos + new Vector3(0f, yOffset, 0f);
    }
}
