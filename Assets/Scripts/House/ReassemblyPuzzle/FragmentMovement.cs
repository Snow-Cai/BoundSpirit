using UnityEngine;
using UnityEngine.EventSystems;

public class FragmentMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 offset;
    public bool locked = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)      //when clicking down
    {
        if(locked) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        offset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(locked) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint + offset;
    }

    public void OnPointerUp(PointerEventData eventData)        //when letting go of click
    {
        if(locked) return;
        var manager = FindFirstObjectByType<ReassemblyPuzzleManager>();
        if(manager != null)
        {
            manager.CheckFragmentPosition(this);
        }
    }
}
