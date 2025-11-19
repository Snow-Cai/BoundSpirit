using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    public string itemID = "SafeKey";
    public float pickupDistance = 2f;
    public SafeControllerKeypad safeController;

    private Transform player;

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null && Vector2.Distance(player.position, transform.position) <= pickupDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))     //player is able to pick up key when within distance
            {
                PickUp();
            }
        }
    }

    void PickUp()       //pick up key and remove from the world
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.PickUpItem(itemID);
            AudioSource.PlayClipAtPoint(safeController.keyPickupSound, transform.position);
            Destroy(gameObject);
        }
    }
}
