using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;

public class SafeKeyhole : MonoBehaviour
{
    public SafeControllerKeypad safeController;
    public string requiredItemID = "SafeKey";
    public Image keyInsertedVisual;
    private bool keyInserted = false;

    public void OnKeyholePressed()      //when pressing keyhole attempting to insert key
    {
        if (keyInserted) return;
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if(inventory != null && inventory.HasItem(requiredItemID))          //correct key is in possession
        {
            keyInserted = true;
            if (keyInsertedVisual != null)
                keyInsertedVisual.enabled = true;
            safeController.InsertKey();
            if (safeController.audioSource && safeController.keyInsertSound)
                safeController.audioSource.PlayOneShot(safeController.keyInsertSound);
            Debug.Log("Inserted key into safe!");
        }
        else                                                               //key is not in possession
        {
            if (safeController.audioSource && safeController.keyholeEmptySound)
                safeController.audioSource.PlayOneShot(safeController.keyholeEmptySound);
            Debug.Log("Key is not in possession!");
        }
    }
}
