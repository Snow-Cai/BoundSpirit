using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float speed = 0.5f;
    public float resetOffset = 50f;

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
        if(transform.position.x > resetOffset / 2f)
        {
            transform.position -= new Vector3(resetOffset, 0, 0);
        }
    }
}
