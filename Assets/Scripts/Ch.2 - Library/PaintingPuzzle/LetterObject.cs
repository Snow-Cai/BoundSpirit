using UnityEngine;
using UnityEngine.EventSystems;

public class LetterObject : MonoBehaviour, IPointerClickHandler
{
    public char letter;
    private bool cleared = false;

    [Header("Correct Letter Position")]
    public int slotIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        TryCollect();
    }

    public void TryCollect()
    {
        if (cleared) return;
        if (PaintingPuzzleManager.Instance == null) return;
        if (!PaintingPuzzleManager.Instance.CanAcceptLetters()) return;
        PaintingPuzzleManager.Instance.SetLetter(slotIndex, letter);

        cleared = true;
        gameObject.SetActive(false);
    }
}
