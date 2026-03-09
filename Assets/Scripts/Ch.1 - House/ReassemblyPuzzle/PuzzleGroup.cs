using UnityEngine;
using System.Collections.Generic;

public class PuzzleGroup : MonoBehaviour
{
    public RectTransform rect;
    public List<PuzzlePiece> pieces = new List<PuzzlePiece>();

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        foreach(Transform child in transform)
        {
            PuzzlePiece piece = child.GetComponent<PuzzlePiece>();
            if(piece != null)
            {
                pieces.Add(piece);
                piece.group = this;
            }
        }
    }

    public void AddPiece(PuzzlePiece piece)
    {
        pieces.Add(piece);
        piece.group = this;
        piece.transform.SetParent(transform, true);
    }
}