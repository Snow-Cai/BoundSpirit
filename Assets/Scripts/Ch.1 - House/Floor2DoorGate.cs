using UnityEngine;

/// <summary>
/// Blocked door: plays locked dialogue when the player enters the trigger or holds movement into the room.
/// Put this on the same GameObject as a <see cref="Collider2D"/> with <c>Is Trigger</c> (or add a second collider:
/// leave <see cref="gateCollider"/> as the solid blocker and use another collider on this object for detection).
/// </summary>
public class Floor2DoorGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [Tooltip("Optional solid collider disabled when unlocked. If this is the only collider, add a second BoxCollider2D for detection and mark it as trigger.")]
    [SerializeField] private Collider2D gateCollider;
    [SerializeField] private bool isUnlocked;

    [Header("When dialogue plays")]
    [Tooltip("World direction from inside the locked room toward the hallway. Used to detect movement “into” the room (opposite to this vector).")]
    [SerializeField] private Vector2 pushOutDirection = Vector2.down;

    [Tooltip("Minimum speed into the room (while in the trigger) to count as trying the door. 0 = any movement into the room.")]
    [SerializeField] private float minSpeedIntoRoom = 0.12f;

    [Tooltip("Also play once when the player first steps into the trigger (even if velocity is still small).")]
    [SerializeField] private bool playOnFirstEnter = true;

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset lockedDialogue;
    [SerializeField] private float dialogueCooldownSeconds = 1.25f;

    [Header("Unlock progression (optional)")]
    [Tooltip("Open the gate once this dialogue id has been viewed (SaveSystem). E.g. Chapter2_dad for the office.")]
    [SerializeField] private string unlockWhenDialogueIdViewed;

    [Tooltip("Open the gate once this puzzle id is solved (SaveSystem). E.g. safe completion id for Akila's room.")]
    [SerializeField] private string unlockWhenPuzzleIdSolved;

    private float _nextDialogueAllowedTime = -1000f;

    private void Awake()
    {
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (gateCollider != null && c == gateCollider)
            {
                c.isTrigger = false;
                continue;
            }

            c.isTrigger = true;
        }
    }

    private void Update()
    {
        RefreshUnlockFromSave();
    }

    private void RefreshUnlockFromSave()
    {
        if (isUnlocked || SaveSystem.Instance == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(unlockWhenDialogueIdViewed) &&
            SaveSystem.Instance.HasViewedDialogue(unlockWhenDialogueIdViewed))
        {
            UnlockGate();
            return;
        }

        if (!string.IsNullOrEmpty(unlockWhenPuzzleIdSolved) &&
            SaveSystem.Instance.IsPuzzleSolved(unlockWhenPuzzleIdSolved))
        {
            UnlockGate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked && playOnFirstEnter)
        {
            TryPlayLockedDialogue(other, requireIntoRoomVelocity: false);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isUnlocked)
        {
            return;
        }

        TryPlayLockedDialogue(other, requireIntoRoomVelocity: true);
    }

    private void TryPlayLockedDialogue(Collider2D other, bool requireIntoRoomVelocity)
    {
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        if (lockedDialogue == null || DialogueSystem.Instance == null)
        {
            return;
        }

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null)
        {
            return;
        }

        Vector2 dir = pushOutDirection.sqrMagnitude > 0.0001f
            ? pushOutDirection.normalized
            : Vector2.down;

        if (requireIntoRoomVelocity)
        {
            float intoRoom = Vector2.Dot(rb.linearVelocity, -dir);
            if (intoRoom < minSpeedIntoRoom)
            {
                return;
            }
        }

        if (DialogueSystem.Instance.IsDialogueActive())
        {
            return;
        }

        if (Time.unscaledTime < _nextDialogueAllowedTime)
        {
            return;
        }

        _nextDialogueAllowedTime = Time.unscaledTime + dialogueCooldownSeconds;
        DialogueSystem.Instance.StartDialogue(lockedDialogue);
    }

    public void UnlockGate()
    {
        isUnlocked = true;

        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (c != null)
            {
                c.enabled = false;
            }
        }
    }
}
