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
        if(InputLock.Instance.CanToggleInventory && Input.GetKeyDown(KeyCode.I))                     //toggle inventory with I key
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()      //open/close inventory screen
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();

        if (isOpen)             //disable movement and stop player in position when opening inventory, as well as disable gameplay input
        {
            movement.enabled = false;
            InputLock.Instance.GameplayInputEnabled = false;
            InputLock.Instance.InteractEnabled = false;
            Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
        else
        {                    //re-enable movement and gameplay input when closing inventory 
            movement.enabled = true;
            InputLock.Instance.GameplayInputEnabled = true;
            InputLock.Instance.InteractEnabled = true;
        }
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
        if (inventoryPanel != null)
            inventoryPanel.SetActive(visible);

        isOpen = visible;

        if (!visible && TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }
}
