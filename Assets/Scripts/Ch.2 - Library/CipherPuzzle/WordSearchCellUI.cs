using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class WordSearchCellUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    private Action<int, int> onPointerDown;
    private Action<int, int> onPointerEnter;
    private Action<int, int> onPointerUp;

    public int Row { get; private set; }
    public int Column { get; private set; }
    public TMP_Text Label { get; private set; }
    public Image Background { get; private set; }

    public void Initialize(
        int row,
        int column,
        TMP_Text label,
        Image background,
        Action<int, int> pointerDown,
        Action<int, int> pointerEnter,
        Action<int, int> pointerUp)
    {
        Row = row;
        Column = column;
        Label = label;
        Background = background;
        onPointerDown = pointerDown;
        onPointerEnter = pointerEnter;
        onPointerUp = pointerUp;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke(Row, Column);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId >= -1)
            onPointerEnter?.Invoke(Row, Column);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp?.Invoke(Row, Column);
    }
}
