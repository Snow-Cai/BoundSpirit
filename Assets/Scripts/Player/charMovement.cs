using UnityEngine;

public class CharMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    private Vector2 movementInput;
    public float pixelsPerUnit = 32f;   //PPU for snapping
    private Animator anim;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); //Grab RigidBody2D component
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal"); //A/D or Left/Right
        movementInput.y = Input.GetAxisRaw("Vertical"); // W/S or Up/Down

        //normalize the input vector to prevent faster diagonal movement
        movementInput.Normalize();

        //send value of Speed to Animator
        anim.SetFloat("Speed", Mathf.Abs(movementInput.x));
    }

    private void FixedUpdate()
    {
        //Apply velocity to the RigidBody2D based on input and speed
        rb.linearVelocity = movementInput * moveSpeed;

        //After movement snap to pixel grid to prevent jitter visually
        Vector2 pos = rb.position;
        pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;
        rb.position = pos;
    }
}
