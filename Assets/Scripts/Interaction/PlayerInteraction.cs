using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public UIPromptAnimator interactionPrompt;
    Interactable currentInteractable;

    private void Start()
    {
        interactionPrompt.Hide();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Interactable interactable))
        {
            currentInteractable = interactable;
            interactionPrompt.Show();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Interactable interactable))
        {
            if (interactable == currentInteractable)
            {
                currentInteractable = null;
                interactionPrompt.Hide();
            }
        }
    }

    private void Update()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interacted with " + currentInteractable.name);
        }
    }
}