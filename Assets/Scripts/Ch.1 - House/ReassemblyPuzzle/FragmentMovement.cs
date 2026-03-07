using UnityEngine;
using UnityEngine.EventSystems;

public class FragmentMovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform rectTransform, groupRoot, movementBounds;
    private Canvas canvas;
    private Vector2 offset;
    public bool locked = false;
    public FragmentConnection[] connections;

    [System.Serializable]
    public class FragmentConnection
    {
        public FragmentMovement otherFragment;
        public Vector2 expectedOffset;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        groupRoot = rectTransform;                             //each puzzle piece has its own group starting off
    }

    public void OnPointerDown(PointerEventData eventData)      //when clicking down
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        offset = groupRoot.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
        Vector2 newPos = localPoint + offset;
        Vector2 clampedPos = ClampToBounds(newPos);
        groupRoot.anchoredPosition = clampedPos;
    }

    public void OnPointerUp(PointerEventData eventData)        //when letting go of click
    {
        var manager = FindFirstObjectByType<ReassemblyPuzzleManager>();
        if(manager != null)
        {
            manager.CheckFragmentPosition(this);
        }
    }

    private Vector2 ClampToBounds(Vector2 targetPosition)
    {
        Rect bounds = movementBounds.rect;
        Vector3 scale = movementBounds.localScale;

        Rect groupRect = groupRoot.rect;
        Vector3 groupScale = groupRoot.localScale;

        float halfW = groupRect.width * groupScale.x / 2f;
        float halfH = groupRect.height * groupScale.y / 2f;

        float minX = bounds.xMin * scale.x + halfW;
        float maxX = bounds.xMax * scale.x - halfW;
        float minY = bounds.yMin * scale.y + halfH;
        float maxY = bounds.yMax * scale.y - halfH;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector2(clampedX, clampedY);
    }
}
