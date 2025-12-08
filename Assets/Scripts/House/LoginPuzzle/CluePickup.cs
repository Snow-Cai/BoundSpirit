using UnityEngine;

public class CluePickup : MonoBehaviour
{
    public Clue clue;
    public float interactRange = 2f;

    private Transform player;
    private bool collected = false; // Only for inventory tracking

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) 
        {
            return;
        }
        ;


        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= interactRange)
        {

            UICluePopup popup = Object.FindFirstObjectByType<UICluePopup>();
            if (popup != null && popup.IsPopupOpen()) 
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }
    }

    void Interact()
    {
        PlayerInventory inv = player.GetComponent<PlayerInventory>();

        if (!collected && inv != null)
        {
            inv.PickUpItem(clue);
            collected = true;
        }

        UICluePopup popup = Object.FindFirstObjectByType<UICluePopup>();
        if (popup != null)
            popup.ShowClue(clue.clueText);

    }
}
