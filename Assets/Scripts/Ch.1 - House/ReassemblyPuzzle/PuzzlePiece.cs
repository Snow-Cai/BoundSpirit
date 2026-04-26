using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform rect;
    public Vector2 correctPos;
    public RectTransform movementBounds;
    public float snapDistance = 40f;

    private Vector2 dragOffset;
    private Canvas canvas;
    private bool isPlaced = false;
    private ReassemblyPuzzleManager manager;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        manager = FindFirstObjectByType<ReassemblyPuzzleManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPlaced) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        dragOffset = rect.anchoredPosition - localPoint;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        Vector2 newPos = localPoint + dragOffset;
        rect.anchoredPosition = ClampToBounds(newPos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPlaced) return;
        float dist = Vector2.Distance(rect.anchoredPosition, correctPos);
        if(dist < snapDistance)
        {
            rect.anchoredPosition = correctPos;
            isPlaced = true;

            GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            rect.SetSiblingIndex(1);

            manager.CheckPuzzleCompletion();
        }
    }

    public bool IsPlaced() => isPlaced;

    private Vector2 ClampToBounds(Vector2 targetPosition)
    {
        if(movementBounds == null) return targetPosition;
        Rect bounds = movementBounds.rect;
        Vector3 boundsScale = movementBounds.localScale;

        Rect groupRect = rect.rect;
        Vector3 groupScale = rect.localScale;

        float halfW = groupRect.width * groupScale.x / 2f;
        float halfH = groupRect.height * groupScale.y / 2f;

        float minX = bounds.xMin * boundsScale.x + halfW;
        float maxX = bounds.xMax * boundsScale.x - halfW;
        float minY = bounds.yMin * boundsScale.y + halfH;
        float maxY = bounds.yMax * boundsScale.y - halfH;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector2(clampedX, clampedY);
    }
}
