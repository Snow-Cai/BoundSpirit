using UnityEngine;

[RequireComponent(typeof(InteractableGlow))]
public class CollectibleObject : MonoBehaviour
{
    [Header("Item Information")]
    public ItemData item;
    public float collectDistance = 2f;

    [Header("Optional Behaviors")]
    public bool disappearOnPickup = true;
    public bool showCluePopup = false;
    public Clue clue;                   // attach if item is a Clue object

    [Header("Audio")]
    public AudioClip pickupSound;

    [Header("Pickup Dialogue")]
    [Tooltip("Optional dialogue queued right after the item is added to inventory (e.g. key pickup reaction).")]
    public DialogueAsset pickupDialogue;
    [Tooltip("If true, pressing interact again after pickup still queues pickupDialogue (e.g. re-read a clue).")]
    public bool repeatPickupDialogueAfterCollect = false;

    [Header("Visual Highlight")]
    [Tooltip("If enabled, sparkle only appears when the player is within collect distance. If disabled, sparkle stays visible whenever gameplay allows.")]
    [SerializeField] private bool glowWhenInRange = false;
    [SerializeField] private InteractableGlow glow;

    private Transform player;
    private bool collected = false;     // for items that do not disappear on pickup, track to prevent duplication
    private UICluePopup popup;

    private void Reset()
    {
        EnsureGlowReference();
    }

    private void OnValidate()
    {
        EnsureGlowReference();
        SetGlow(false);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        popup = Object.FindFirstObjectByType<UICluePopup>(FindObjectsInactive.Include);
        EnsureGlowReference();
        if (p != null)
            player = p.transform;
    }
    private void Update()
    {
        if (player == null)
        {
            SetGlow(false);
            return;
        }
        if (popup != null && popup.IsPopupOpen())
        {
            SetGlow(false);
            return;
        }
        float dist = Vector2.Distance(player.position, transform.position);

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
        {
            SetGlow(false);
            return;
        }

        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
        {
            SetGlow(false);
            return;
        }

        bool withinRange = dist <= collectDistance;
        bool shouldShowSparkle = !glowWhenInRange || withinRange;
        SetGlow(shouldShowSparkle);

        if (withinRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!InteractionPriorityResolver.IsHighestPriorityTarget(this, player))
                return;

            if (!InteractionPriorityResolver.TryConsumeInteraction())
                return;

            PickUp();
        }
    }
    void PickUp()
    {
        PlayerInventory inv = player.GetComponent<PlayerInventory>();
        if (inv != null && item != null && !collected)
        {
            inv.PickUpItem(item);
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.CollectItem(item.itemID);
            if (pickupSound != null)
                SfxPlayback.PlayClipAtPoint(pickupSound, transform.position);
            if (pickupDialogue != null && DialogueSystem.Instance != null)
                DialogueSystem.Instance.QueueDialogue(pickupDialogue);
            collected = true;
        }
        else if (
            collected &&
            repeatPickupDialogueAfterCollect &&
            pickupDialogue != null &&
            DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.QueueDialogue(pickupDialogue);
        }

        if (showCluePopup)
        {
            if (popup != null)
                popup.ShowMessage(clue.clueText);
        }

        if (disappearOnPickup)
        {
            Destroy(gameObject);
        }
    }

    private void SetGlow(bool enabled)
    {
        if (glow == null)
            return;

        glow.SetHighlighted(enabled);
    }

    private void EnsureGlowReference()
    {
        if (glow == null)
        {
            glow = GetComponent<InteractableGlow>();
        }

        if (glow == null)
        {
            glow = gameObject.AddComponent<InteractableGlow>();
        }

        if (glow != null)
        {
            glow.ApplyStyle(InteractableGlow.HighlightStyle.CollectibleSparkle);
        }
    }

    public bool CanBeInteractedWith(Transform targetPlayer)
    {
        if (targetPlayer == null || !isActiveAndEnabled)
            return false;

        if (popup != null && popup.IsPopupOpen())
            return false;

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
            return false;

        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return false;

        if (collected && !repeatPickupDialogueAfterCollect)
            return false;

        return GetDistanceTo(targetPlayer) <= collectDistance;
    }

    public float GetDistanceTo(Transform targetPlayer)
    {
        if (targetPlayer == null)
            return float.MaxValue;

        return Vector2.Distance(targetPlayer.position, transform.position);
    }
}
