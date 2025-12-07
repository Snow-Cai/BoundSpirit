using UnityEngine;

public class Interactable : MonoBehaviour
{
    // This gets called when the player presses E
    public virtual void OnInteract()
    {
        Debug.Log("Interacted with: " + name);
    }
}

