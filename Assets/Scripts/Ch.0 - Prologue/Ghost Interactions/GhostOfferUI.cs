using System.Collections.Generic;
using UnityEngine;

public class GhostOfferUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GhostOfferSlotUI[] offerSlots;

    private GhostHintNPC currentGhost;
    private List<ItemData> currentItems = new List<ItemData>();

    private bool IsOpen =>
        panelRoot != null &&
        panelRoot.activeSelf &&
        currentGhost != null;

    private void Awake()
    {
        Close();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        // DialogueSystem clears GameInputState.DialogueActive when the dialogue queue empties,
        // which runs after OnDialogueEnded opens this UI — keep the modal flag until we close.
        GameInputState.DialogueActive = true;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            OfferByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            OfferByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            OfferByIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            Close();
        }
    }

    public void Open(GhostHintNPC ghost)
    {
        if (ghost == null)
        {
            Debug.LogWarning("GhostOfferUI.Open called with null ghost.");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("GhostOfferUI: PlayerInventory is not assigned.");
            return;
        }

        currentGhost = ghost;
        currentItems = playerInventory.GetItems() ?? new List<ItemData>();

        RefreshUI();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        GameInputState.DialogueActive = true;
        SetGameplayInputEnabled(false);
        if (InputLock.Instance != null)
        {
            InputLock.Instance.AllowInspect = false;
        }
    }

    public void Close()
    {
        currentGhost = null;
        currentItems.Clear();

        HideAllSlots();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        GameInputState.DialogueActive = false;
        SetGameplayInputEnabled(true);
        if (InputLock.Instance != null)
        {
            InputLock.Instance.AllowInspect = true;
        }
    }

    private static void SetGameplayInputEnabled(bool enabled)
    {
        if (InputLock.Instance != null)
        {
            InputLock.Instance.GameplayInputEnabled = enabled;
        }
    }

    public void OfferItem(ItemData item)
    {
        if (currentGhost == null)
        {
            Debug.LogWarning("GhostOfferUI: No current ghost to offer item to.");
            return;
        }

        if (item == null)
        {
            Debug.LogWarning("GhostOfferUI: Tried to offer a null item.");
            return;
        }

        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();

        GhostHintNPC targetGhost = currentGhost;

        Close();
        targetGhost.OfferItem(item);
    }

    private void OfferByIndex(int index)
    {
        if (index < 0 || index >= currentItems.Count)
        {
            return;
        }

        ItemData item = currentItems[index];
        if (item != null)
        {
            OfferItem(item);
        }
    }

    private void RefreshUI()
    {
        if (offerSlots == null || offerSlots.Length == 0)
        {
            Debug.LogWarning("GhostOfferUI: No offer slots assigned.");
            return;
        }

        for (int i = 0; i < offerSlots.Length; i++)
        {
            if (offerSlots[i] == null)
            {
                continue;
            }

            ItemData item = i < currentItems.Count ? currentItems[i] : null;
            offerSlots[i].Setup(item, i, this);
        }
    }

    private void HideAllSlots()
    {
        if (offerSlots == null)
        {
            return;
        }

        for (int i = 0; i < offerSlots.Length; i++)
        {
            if (offerSlots[i] != null)
            {
                offerSlots[i].gameObject.SetActive(false);
            }
        }
    }
}