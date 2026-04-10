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

    [Header("Visual Highlight")]
    [SerializeField] private bool glowWhenInRange = true;
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
        popup = Object.FindFirstObjectByType<UICluePopup>();
        EnsureGlowReference();
        SetGlow(false);
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
        SetGlow(glowWhenInRange && withinRange);

        if (HasNearbyPriorityInteractable())
        {
            return;
        }

        if (withinRange && Input.GetKeyDown(KeyCode.E))
        {
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
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            collected = true;
        }

        if (showCluePopup)
        {
            if (popup != null)
                popup.ShowClue(clue.clueText);
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

        if (glow == null && glowWhenInRange)
        {
            glow = gameObject.AddComponent<InteractableGlow>();
        }

        if (glow != null)
        {
            glow.ApplyStyle(InteractableGlow.HighlightStyle.CollectibleSparkle);
        }
    }

    private bool HasNearbyPriorityInteractable()
    {
        InteractableObject[] interactables = Object.FindObjectsByType<InteractableObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null || !interactable.isActiveAndEnabled)
                continue;

            if (interactable.GetComponent<NPCController>() == null)
                continue;

            float dist = Vector2.Distance(player.position, interactable.transform.position);
            if (dist <= interactable.interactionRange)
                return true;
        }

        return false;
    }
}