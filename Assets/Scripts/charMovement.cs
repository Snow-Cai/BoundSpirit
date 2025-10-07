using UnityEngine;

public class CharMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    private Vector2 movementInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); //Grab RigidBody2D component
    }

    // Update is called once per frame
    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal"); //A/D or Left/Right
        movementInput.y = Input.GetAxisRaw("Vertical"); // W/S or Up/Down

        //normale the input vector to prevent faster diagonal movement
        movementInput.Normalize();
    }

    private void FixedUpdate()
    {
        //Apply velocity to the RigidBody2D based on input and speed
        rb.linearVelocity = movementInput * moveSpeed;
    }
}
