using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Profile (ScriptableObject)")] //will take out later, note to remember
    public MomBehaviorProfile profile;

    [Header("Waypoints")]
    public Transform[] randomPoints;
    public Transform[] scheduledPoints;

    [Header("Movement")]
    public float speed = 2f;
    public Animator animator;

    private Transform targetPoint;
    private bool isIdle = false;
    private float idleTimer;
    private int scheduleIndex = 0;

    private bool isInteracting = false;
    private bool playerNearby = false;
    private GameObject playerRef;


    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            Debug.LogError("NPCController requires a Rigidbody2D on NPC");

        rb.gravityScale = 0;
        rb.freezeRotation = true;     // no more spinning like a soccerball
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ChooseNextTarget();
    }

    void Update()
    {

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isIdle = false;
                ChooseNextTarget();
            }
        }
    }

    void FixedUpdate()
    {
        if (isInteracting)
        {
            rb.linearVelocity = Vector2.zero;
            return;   // NPC stays still during interaction
        }

        if (!isIdle && targetPoint != null)
        {
            MoveToTarget();
        }
    }


    void MoveToTarget()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = targetPoint.position;
        Vector2 direction = (targetPos - currentPos).normalized;

  
        rb.linearVelocity = direction * speed;

        //animation parameters
        if (animator != null)
        {
            animator.SetFloat("Horizontal", direction.x);
            animator.SetFloat("Vertical", direction.y);
            animator.SetFloat("Speed", direction.magnitude);
        }

        // stop when somewhat near waypoint
        if (Vector2.Distance(currentPos, targetPos) < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            StartIdle();
        }
    }

    void StartIdle()
    {
        isIdle = true;
        rb.linearVelocity = Vector2.zero; 

        idleTimer = Random.Range(profile.minIdleTime, profile.maxIdleTime);

        if (animator != null)
            animator.SetFloat("Speed", 0);
    }

    void ChooseNextTarget()
    {
        if (profile.useSchedule && scheduledPoints.Length > 0)
        {
            targetPoint = scheduledPoints[scheduleIndex];
            scheduleIndex = (scheduleIndex + 1) % scheduledPoints.Length;
            return;
        }

        if (profile.allowRandomWander && randomPoints.Length > 0)
        {
            int index = Random.Range(0, randomPoints.Length);
            targetPoint = randomPoints[index];
        }
    }
    public void StartInteraction()
    {
        isInteracting = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetFloat("Speed", 0);
    }

    public void EndInteraction()
    {
        isInteracting = false;
        ChooseNextTarget(); // resume regular behavior
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("PLAYER ENTERED NPC RANGE");
            playerNearby = true;
            playerRef = other.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("PLAYER LEFT NPC RANGE");
            playerNearby = false;
            playerRef = null;
            EndInteraction();
        }
    }

}


