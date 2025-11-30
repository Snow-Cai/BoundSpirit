using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory inventory;
    public Transform inventorySlots;        //where slots are created
    public GameObject inventoryPanel;       //inventory window
    public GameObject itemSlotPrefab;
    public CharMovement movement;

    private bool isOpen = false;

    private void Start()
    {
        inventoryPanel.SetActive(false);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))                     //toggle inventory with I key
        {
            ToggleInventory();
        }

        if(Input.GetKeyDown(KeyCode.Escape) && isOpen)      //allows player to close inventory with esc key
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()      //open/close inventory screen
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        if (isOpen)             //disable movement and stop player in position when opening inventory
        {
            movement.enabled = false;
            Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
        else                    //re-enable movement when closing inventory
            movement.enabled = true;
        if(isOpen)
            RefreshUI();
    }

    void RefreshUI()
    {
        foreach (Transform child in inventorySlots)     //clear existing inventory items
            Destroy(child.gameObject);
        foreach (ItemData item in inventory.GetItems())     //add currently held items in inventory
        {
            GameObject slot = Instantiate(itemSlotPrefab, inventorySlots);
            Image iconImage = slot.GetComponentInChildren<Image>();
            if (iconImage != null)
                iconImage.sprite = item.icon;
        }
    }
}
