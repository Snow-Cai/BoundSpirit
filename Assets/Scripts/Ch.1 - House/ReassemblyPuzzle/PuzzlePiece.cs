using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform rect;
    public PuzzleGroup group;
    public PieceConnection[] connections;
    public RectTransform movementBounds;

    Vector2 dragOffset;
    Canvas canvas;
    ReassemblyPuzzleManager manager;

    [System.Serializable]
    public class PieceConnection
    {
        public PuzzlePiece otherPiece;
        public Vector2 expectedOffset;
        public bool connected;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        manager = FindFirstObjectByType<ReassemblyPuzzleManager>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        dragOffset = group.rect.anchoredPosition - localPoint;
        group.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        Vector2 newPos = localPoint + dragOffset;
        group.rect.anchoredPosition = ClampToBounds(newPos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manager.CheckConnections(this);
    }

    private Vector2 ClampToBounds(Vector2 targetPosition)
    {
        if(movementBounds == null) return targetPosition;
        Rect bounds = movementBounds.rect;
        Vector3 boundsScale = movementBounds.localScale;

        Rect groupRect = group.rect.rect;
        Vector3 groupScale = group.rect.localScale;

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
