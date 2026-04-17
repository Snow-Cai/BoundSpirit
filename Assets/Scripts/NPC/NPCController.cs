using System.Collections.Generic;
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
    public LayerMask obstacleLayers = 1;
    public float gridSize = 0.5f;
    public Vector2 collisionProbeSize = new Vector2(0.6f, 0.6f);
    public float repathInterval = 0.35f;
    public float pathNodeReachDistance = 0.06f;
    public float targetReachDistance = 0.12f;
    public float maxSearchDistance = 24f;
    public int maxSearchIterations = 1024;
    public bool allowDiagonalMovement = false;
    public float movementDeadZone = 0.02f;
    public float stuckRepathDelay = 0.4f;
    public float stuckVelocityThreshold = 0.05f;

    private Transform targetPoint;
    private bool isIdle = false;
    private float idleTimer;
    private int scheduleIndex = 0;

    private bool isInteracting = false;
    private bool playerNearby = false;
    private GameObject playerRef;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private readonly List<Vector2> currentPath = new List<Vector2>();
    private float repathTimer;
    private int currentPathIndex;
    private Vector2 lastRepathPosition;
    private bool hasPath;
    private float stuckTimer;
    private float failedPathRetryTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            Debug.LogError("NPCController requires a Rigidbody2D on NPC");

        rb.gravityScale = 0;
        rb.freezeRotation = true;     // no more spinning like a soccerball
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        spriteRenderer = GetComponent<SpriteRenderer>();

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
            repathTimer -= Time.fixedDeltaTime;
            failedPathRetryTimer -= Time.fixedDeltaTime;
            UpdatePathIfNeeded();
            MoveToTarget();
        }
    }


    void MoveToTarget()
    {
        if (!hasPath)
        {
            rb.linearVelocity = Vector2.zero;

            if (animator != null)
                animator.SetFloat("Speed", 0);

            return;
        }

        Vector2 currentPos = rb.position;
        Vector2 targetPos = GetCurrentMoveTarget(currentPos);
        Vector2 toTarget = targetPos - currentPos;

        if (toTarget.magnitude <= pathNodeReachDistance)
        {
            AdvancePathIfNeeded(currentPos);
            targetPos = GetCurrentMoveTarget(currentPos);
            toTarget = targetPos - currentPos;
        }

        if (toTarget.magnitude <= movementDeadZone)
        {
            rb.linearVelocity = Vector2.zero;

            if (animator != null)
                animator.SetFloat("Speed", 0);

            return;
        }

        Vector2 direction = toTarget.normalized;

        rb.linearVelocity = direction * speed;

        if (spriteRenderer != null)
        {
            if (direction.x > movementDeadZone)
                spriteRenderer.flipX = false;
            else if (direction.x < -movementDeadZone)
                spriteRenderer.flipX = true;
        }

        //animation parameters
        if (animator != null)
        {
            animator.SetFloat("Horizontal", direction.x);
            animator.SetFloat("Vertical", direction.y);
            animator.SetFloat("Speed", direction.magnitude);
        }

        // stop when somewhat near waypoint
        AdvancePathIfNeeded(currentPos);
        UpdateStuckState(direction);

        bool reachedPathGoal = currentPathIndex >= currentPath.Count - 1 &&
                               Vector2.Distance(currentPos, targetPos) < targetReachDistance;

        if (reachedPathGoal || Vector2.Distance(currentPos, targetPoint.position) < targetReachDistance)
        {
            rb.linearVelocity = Vector2.zero;
            StartIdle();
        }
    }

    void StartIdle()
    {
        isIdle = true;
        rb.linearVelocity = Vector2.zero;
        ClearPath();

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
            ClearPath();
            return;
        }

        if (profile.allowRandomWander && randomPoints.Length > 0)
        {
            int index = Random.Range(0, randomPoints.Length);
            targetPoint = randomPoints[index];
            ClearPath();
        }
    }
    public void StartInteraction()
    {
        isInteracting = true;
        rb.linearVelocity = Vector2.zero;

        if (playerRef == null)
            playerRef = GameObject.FindGameObjectWithTag("Player");

        if (playerRef != null && spriteRenderer != null)
        {
            if (playerRef.transform.position.x > transform.position.x)
                spriteRenderer.flipX = false;
            else
                spriteRenderer.flipX = true;
        }

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

    private void UpdatePathIfNeeded()
    {
        if (targetPoint == null)
            return;

        if (repathTimer > 0f && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            if (Vector2.Distance(rb.position, lastRepathPosition) > pathNodeReachDistance)
                return;
        }

        if (failedPathRetryTimer > 0f)
            return;

        repathTimer = Mathf.Max(0.05f, repathInterval);
        lastRepathPosition = rb.position;

        var settings = new GridAStar2D.Settings(
            gridSize,
            obstacleLayers,
            collisionProbeSize,
            allowDiagonalMovement,
            maxSearchIterations,
            maxSearchDistance);

        if (GridAStar2D.TryFindPath(rb.position, targetPoint.position, settings, currentPath))
        {
            currentPathIndex = currentPath.Count > 1 ? 1 : 0;
            hasPath = true;
            failedPathRetryTimer = 0f;
            return;
        }

        currentPath.Clear();
        currentPathIndex = 0;
        hasPath = false;
        failedPathRetryTimer = Mathf.Max(0.1f, repathInterval);
    }

    private Vector2 GetCurrentMoveTarget(Vector2 currentPos)
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
            return targetPoint.position;

        Vector2 node = currentPath[currentPathIndex];
        if (Vector2.Distance(currentPos, node) <= pathNodeReachDistance && currentPathIndex < currentPath.Count - 1)
        {
            currentPathIndex++;
            node = currentPath[currentPathIndex];
        }

        return node;
    }

    private void AdvancePathIfNeeded(Vector2 currentPos)
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
            return;

        while (currentPathIndex < currentPath.Count - 1 &&
               Vector2.Distance(currentPos, currentPath[currentPathIndex]) <= pathNodeReachDistance)
        {
            currentPathIndex++;
        }
    }

    private void ClearPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        repathTimer = 0f;
        hasPath = false;
        lastRepathPosition = rb.position;
        stuckTimer = 0f;
        failedPathRetryTimer = 0f;
    }

    private void UpdateStuckState(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude <= 0f)
        {
            stuckTimer = 0f;
            return;
        }

        if (rb.linearVelocity.magnitude <= stuckVelocityThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckRepathDelay)
            {
                repathTimer = 0f;
                stuckTimer = 0f;
            }

            return;
        }

        stuckTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawWireSphere(currentPath[i], 0.08f);

            if (i < currentPath.Count - 1)
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }
    }

}
