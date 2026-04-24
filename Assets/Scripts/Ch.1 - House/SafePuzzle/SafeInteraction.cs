using UnityEngine;
using System.Collections;
public class SafeInteraction : MonoBehaviour
{
    public GameObject safeUIPanel;
    public float interactionRadius = 1.5f;
    public Transform playerTransform;

    private CanvasGroup cg;
    private bool isOpen = false;

    private void Start()
    {
        cg = safeUIPanel.GetComponent<CanvasGroup>();
        if(cg == null)
            cg = safeUIPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        safeUIPanel.SetActive(false);
    }
    private void Update()
    {
        if (playerTransform == null) return;

        if (!isOpen &&
            InputLock.Instance != null &&
            (!InputLock.Instance.GameplayInputEnabled || !InputLock.Instance.InteractEnabled))
        {
            return;
        }

        float dist = Vector2.Distance(playerTransform.position, transform.position);
        if(!isOpen && dist <= interactionRadius && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = true;
            StartCoroutine(OpenSafe());
        }
        else if(isOpen && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(CloseRoutine());
        }
    }
    private IEnumerator OpenSafe()
    {
        safeUIPanel.SetActive(true);
        InputLock.Instance.CanToggleInventory = false;
        InputLock.Instance.GameplayInputEnabled = false;
        //panel appears and is interactable
        cg.alpha = 1;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        yield return null;
    }
    public void CloseSafe()
    {
        StartCoroutine(CloseRoutine());
    }
    private IEnumerator CloseRoutine()
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = 0;
        safeUIPanel.SetActive(false);
        InputLock.Instance.CanToggleInventory = true;
        InputLock.Instance.GameplayInputEnabled = true;
        isOpen = false;
        yield return null;
    }
}
