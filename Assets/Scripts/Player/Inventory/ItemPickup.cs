using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemID;             //set per item 
    public float pickupDistance = 2f;
    public PlayerInventory inventory;
    public AudioClip pickupSound;

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
            if (Input.GetKeyDown(KeyCode.E))     //player is able to pick up item when within distance
            {
                PickUp();
            }
        }
    }

    void PickUp()       //pick up item with SFX and remove from the world
    {
        if (inventory != null)
        {
            inventory.PickUpItem(itemID);
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            Destroy(gameObject);
        }
        //save to inventory
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.CollectItem(itemID.itemName);
        }
    }
}
