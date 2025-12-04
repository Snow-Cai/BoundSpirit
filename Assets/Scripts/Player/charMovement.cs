using UnityEngine;
using UnityEngine.Windows;

public class CharMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    private Vector2 movementInput;
    public float pixelsPerUnit = 32f;   //PPU for snapping
    private Animator anim;
    private float lastMoveX = 0;
    private float lastMoveY = -1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); //Grab RigidBody2D component
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        movementInput.x = UnityEngine.Input.GetAxisRaw("Horizontal"); //A/D or Left/Right
        movementInput.y = UnityEngine.Input.GetAxisRaw("Vertical"); // W/S or Up/Down

        //normalize the input vector to prevent faster diagonal movement
        movementInput.Normalize();


        // Track last movement direction only when moving
        if (movementInput.sqrMagnitude > 0.1f)
        {
            lastMoveX = movementInput.x;
            lastMoveY = movementInput.y;
        }

        // Flip sprite for left/right movement
        if (movementInput.x > 0)
            GetComponent<SpriteRenderer>().flipX = true;  // facing left (due to how sprite originally faces)
        else if (movementInput.x < 0)
            GetComponent<SpriteRenderer>().flipX = false;   // facing right (due to how sprite originally faces)

        //update animator parameters
        anim.SetFloat("MoveX", movementInput.x);
        anim.SetFloat("MoveY", movementInput.y);
        anim.SetFloat("Speed", movementInput.sqrMagnitude);
        anim.SetFloat("LastMoveX", lastMoveX);
        anim.SetFloat("LastMoveY", lastMoveY);
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
