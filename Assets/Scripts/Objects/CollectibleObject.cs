using UnityEngine;

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

    private Transform player;
    private bool collected = false;     // for items that do not disappear on pickup, track to prevent duplication
    private UICluePopup popup;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        popup = Object.FindFirstObjectByType<UICluePopup>();
        if (p != null)
            player = p.transform;
    }
    private void Update()
    {
        if (player == null)
            return;
        if (popup != null && popup.IsPopupOpen())
            return;
        float dist = Vector2.Distance(player.position, transform.position);
        if (dist <= collectDistance && Input.GetKeyDown(KeyCode.E))
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
}