using UnityEngine;

public class InputLock : MonoBehaviour
{
    public static InputLock Instance;

    public bool GameplayInputEnabled = true;
    public bool CanToggleInventory = true;          // Can press I to open/close inventory
    public bool AllowInspect = true;                // Can inspect window
    public bool InteractEnabled = true;             // Can press E to open/close windows

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}
