using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Profile (ScriptableObject)")]
    public MomBehaviorProfile profile;

    [Header("Waypoints (Scene Objects)")]
    public Transform[] randomPoints;
    public Transform[] scheduledPoints;

    [Header("Movement")]
    public float speed = 2f;
    public Animator animator;

    private Transform targetPoint;
    private bool isIdle = false;
    private float idleTimer;
    private int scheduleIndex = 0;

    void Start()
    {
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
            return;
        }

        if (targetPoint != null)
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        Vector2 direction = (targetPoint.position - transform.position).normalized;

        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPoint.position,
            speed * Time.deltaTime
        );

        if (animator != null)
        {
            animator.SetFloat("Horizontal", direction.x);
            animator.SetFloat("Vertical", direction.y);
            animator.SetFloat("Speed", direction.magnitude);
        }

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            StartIdle();
        }
    }

    void StartIdle()
    {
        isIdle = true;
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
}
