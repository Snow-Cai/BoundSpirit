using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory inventory;
    public Transform inventorySlots;        //where slots are created
    public GameObject inventoryPanel;       //inventory window
    public GameObject itemSlotPrefab;
    public CharMovement movement;
    private SlotUI[] slots;
    private bool isOpen = false;

    private void Start()
    {
        slots = new SlotUI[9];
        for(int i = 0; i < 9; i++)
        {
            GameObject slotObject = Instantiate(itemSlotPrefab.gameObject, inventorySlots);
            slots[i] = slotObject.GetComponent<SlotUI>();
            slots[i].slotIndex = i;
            slots[i].SetItem(inventory.GetInventoryItem(i));
        }
        inventoryPanel.SetActive(false);
    }
    private void Update()
    {
        if(InputLock.Instance.GameplayInputEnabled && Input.GetKeyDown(KeyCode.I))                     //toggle inventory with I key
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

    public void RefreshUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetItem(inventory.GetInventoryItem(i));
        }
    }

    public void SetVisible(bool visible)
    { 
        inventoryPanel.SetActive(visible); 
    }
}
