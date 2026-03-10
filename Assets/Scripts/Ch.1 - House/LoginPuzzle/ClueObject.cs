using UnityEngine;

public class ClueObject : MonoBehaviour
{
    [Header("Clue Data")]
    public Clue clue;

    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public bool showPrompt = true;
    public KeyCode interactKey = KeyCode.E;
    public GameObject promptUI;

    private bool collected = false;
    private Transform player;

    [Header("Highlight")]
    public SpriteRenderer outlineRenderer;
    public float pulseSpeed = 2f;

    private PlayerInventory playerInventory;
    private ClueJournal clueJournal;
    private UICluePopup popup;

    void Start()
    {
        //get player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            playerInventory = player.GetComponent<PlayerInventory>();

        clueJournal = Object.FindFirstObjectByType<ClueJournal>();
        popup = Object.FindFirstObjectByType<UICluePopup>();

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        if (player == null || collected) return;

        float dist = Vector2.Distance(player.position, transform.position);

        if (outlineRenderer != null)
        {
            outlineRenderer.color = new Color(
                1f,
                Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)),
                0f,
                1f
            );
        }


        if (dist <= interactRange)
        {
            if (showPrompt && promptUI != null)
                promptUI.SetActive(true);

            if (Input.GetKeyDown(interactKey))
                CollectClue();
        }
        else if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    void CollectClue()
    {
        if (collected) return;
        collected = true;

        // add to Inverntory
        if (playerInventory != null)
            playerInventory.PickUpItem(clue);

        // add to journal
        if (clueJournal != null)
            clueJournal.AddClue(clue);

        if (popup != null)
            popup.ShowClue("Clue Found: " + clue.clueText);

        if (outlineRenderer != null)
            outlineRenderer.enabled = false;

        gameObject.SetActive(false);
    }
}
