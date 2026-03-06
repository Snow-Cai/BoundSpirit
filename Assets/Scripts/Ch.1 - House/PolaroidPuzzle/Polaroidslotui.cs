using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

//attached to each polaroid card in the timeline puzzle UI
//handles drag-and-drop between slots and inspect popup
public class PolaroidSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image polaroidImage;
    public TextMeshProUGUI yearLabel;
    public TextMeshProUGUI captionLabel;
    public GameObject emptySlotVisual;      //when slot is empty
    public GameObject filledSlotVisual;     //when slot has a polaroid

    [Header("Drag Visual")]
    public CanvasGroup canvasGroup;

    public PolaroidData currentPolaroid { get; private set; }
    public int slotIndex;                   //which position in the timeline this slot represents

    private Canvas rootCanvas;
    private GameObject dragProxy;
    private PolaroidTimelinePuzzle puzzleManager;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        puzzleManager = GetComponentInParent<PolaroidTimelinePuzzle>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetPolaroid(PolaroidData data)
    {
        currentPolaroid = data;
        RefreshVisual();
    }

    public PolaroidData RemovePolaroid()
    {
        PolaroidData removed = currentPolaroid;
        currentPolaroid = null;
        RefreshVisual();
        return removed;
    }

    private void RefreshVisual()
    {
        bool hasPolaroid = currentPolaroid != null;

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(!hasPolaroid);
        if (filledSlotVisual != null)
            filledSlotVisual.SetActive(hasPolaroid);

        if (hasPolaroid)
        {
            if (polaroidImage != null && currentPolaroid.polaroidImage != null)
            {
                polaroidImage.sprite = currentPolaroid.polaroidImage;
                polaroidImage.enabled = true;
            }
            if (yearLabel != null)
                yearLabel.text = currentPolaroid.year;
            if (captionLabel != null)
                captionLabel.text = currentPolaroid.captionText;
        }
        else
        {
            if (polaroidImage != null)
                polaroidImage.enabled = false;
            if (yearLabel != null)
                yearLabel.text = "";
            if (captionLabel != null)
                captionLabel.text = "";
        }
    }

    //drag and drop

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentPolaroid == null) return;

        //make a ghost image for dragging
        dragProxy = new GameObject("DragProxy");
        dragProxy.transform.SetParent(rootCanvas.transform);
        dragProxy.transform.SetAsLastSibling();

        Image img = dragProxy.AddComponent<Image>();
        img.sprite = polaroidImage.sprite;
        img.raycastTarget = false;

        RectTransform rt = dragProxy.GetComponent<RectTransform>();
        rt.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        rt.localScale = Vector3.one;

        //dim original slot
        canvasGroup.alpha = 0.4f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragProxy == null) return;

        RectTransform rt = dragProxy.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            rootCanvas.worldCamera,
            out localPoint
        );
        rt.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragProxy != null)
            Destroy(dragProxy);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        PolaroidSlotUI dragSource = eventData.pointerDrag?.GetComponent<PolaroidSlotUI>();
        if (dragSource == null || dragSource == this) return;
        if (dragSource.currentPolaroid == null) return;

        //swap polaroids between slots
        PolaroidData temp = currentPolaroid;
        SetPolaroid(dragSource.currentPolaroid);
        dragSource.SetPolaroid(temp);

        //ask manager to check if puzzle is solved
        if (puzzleManager != null)
            puzzleManager.CheckSolution();
    }

    //inspect on click

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        if (currentPolaroid == null) return;
        if (puzzleManager != null)
            puzzleManager.ShowInspectPopup(currentPolaroid);
    }
}