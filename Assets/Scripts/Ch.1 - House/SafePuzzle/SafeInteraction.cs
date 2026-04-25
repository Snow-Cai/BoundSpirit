using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class SafeInteraction : MonoBehaviour
{
    public GameObject safeUIPanel;
    public float interactionRadius = 1.5f;
    public Transform playerTransform;

    private CanvasGroup cg;
    public bool isOpen = false;
    private bool isSolved = false;

    public CanvasGroup weaponCanvas;
    public Image weaponObject;
    public DialogueAsset dialogueOnShowWeapon;

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
        if(dist <= interactionRadius && Input.GetKeyDown(KeyCode.E))
        {
            if (isSolved && !isOpen)
            {
                ShowWeapon();
            }
            else if(isSolved && isOpen && !DialogueSystem.Instance.IsDialogueActive())
            {
                HideWeapon();
            }
            else if(isSolved && isOpen && DialogueSystem.Instance.IsDialogueActive())
            {
                return;
            }
            else
            {
                if (!isOpen)
                {
                    isOpen = true;
                    StartCoroutine(OpenSafe());
                }
                else
                {
                    if (InputLock.Instance.InteractEnabled) StartCoroutine(CloseRoutine());
                }
            }
        }
    }
    private IEnumerator OpenSafe()
    {
        PuzzleBridge.currentPuzzleSource = GetComponent<InteractableObject>();
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
        if (PuzzleBridge.currentPuzzleSource == GetComponent<InteractableObject>())
            PuzzleBridge.currentPuzzleSource = null;
        InputLock.Instance.CanToggleInventory = true;
        InputLock.Instance.GameplayInputEnabled = true;
        isOpen = false;
        yield return null;
    }

    void ShowWeapon()
    {
        if (safeUIPanel != null) safeUIPanel.SetActive(false);
        if (weaponCanvas != null) weaponCanvas.alpha = 1f;
        if (weaponObject != null) weaponObject.gameObject.SetActive(true);
        isOpen = true;
        DialogueSystem.Instance.StartDialogue(dialogueOnShowWeapon);
        InputLock.Instance.CanToggleInventory = false;
        InputLock.Instance.GameplayInputEnabled = false;
    }

    void HideWeapon()
    {
        if (weaponCanvas != null) weaponCanvas.alpha = 0f;
        if (weaponObject != null) weaponObject.gameObject.SetActive(false);
        StartCoroutine(CloseRoutine());
    }

    public void SetSolved()
    {
        isSolved = true;
    }

    public void SetOpen()
    {
        isOpen = false;
    }
}
