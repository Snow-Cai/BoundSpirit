using UnityEngine;

public class InputLock : MonoBehaviour
{
    public static InputLock Instance;

    public bool GameplayInputEnabled = true;
    public bool CanToggleInventory = true;
    public bool AllowInspect = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}
