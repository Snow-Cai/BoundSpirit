using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReassemblyPuzzleManager : MonoBehaviour
{
    public PuzzlePiece[] pieces;
    public float snapDistance = 60f;

    [SerializeField] private GameObject puzzleGroupsParent;
    [SerializeField] private Image finalResultImage;

    public void Awake()
    {
        if (pieces == null || pieces.Length == 0)
            pieces = FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.InstanceID);
        if (puzzleGroupsParent == null)
            puzzleGroupsParent = GameObject.Find("PuzzleAreaHolder");
        if (finalResultImage == null)
            finalResultImage = GameObject.Find("FinalNote")?.GetComponent<Image>();
    }

    public void CheckConnections(PuzzlePiece piece)
    {
        foreach(var connection in piece.connections)
        {
            if (connection.connected) continue;
            PuzzlePiece other = connection.otherPiece;
            Vector2 offset = (Vector2)other.rect.position - (Vector2)piece.rect.position;

            if (Vector2.Distance(offset, connection.expectedOffset) < snapDistance)
            {
                Snap(piece, other, connection);
                break;
            }
        }
    }

    private void Snap(PuzzlePiece a, PuzzlePiece b, PuzzlePiece.PieceConnection connection)         // snap pieces together
    {
        PuzzleGroup groupA = a.group;
        PuzzleGroup groupB = b.group;
        if (groupA == groupB) return;
        Vector2 target = (Vector2)a.rect.position + connection.expectedOffset;
        Vector2 delta = target - (Vector2)b.rect.position;
        groupB.rect.position += (Vector3)delta;
        List<PuzzlePiece> movingPieces = new List<PuzzlePiece>(groupB.pieces);
        foreach (PuzzlePiece piece in movingPieces)
        {
            groupA.AddPiece(piece);
            piece.group = groupA;
        }
        Destroy(groupB.gameObject);
        connection.connected = true;
        CheckPuzzleCompletion();
    }

    public void CheckPuzzleCompletion()
    {
        if (pieces == null || pieces.Length == 0)
        {
            Debug.LogWarning("PuzzleManager: pieces array is empty!");
            return;
        }

        Transform finalGroup = pieces[1].transform.parent;
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            if (piece.transform.parent != finalGroup)
                return;
        }

        Debug.Log("Reassembly puzzle solved!");
        StartCoroutine(ShowCompletion());
    }

    IEnumerator ShowCompletion()
    {
        puzzleGroupsParent.SetActive(false);
        Color c = finalResultImage.color;
        c.a = 1f;
        finalResultImage.color = c;
        float t = 0f;
        float duration = 0.5f;
        Vector3 startScale = Vector3.one * 2.5f;
        Vector3 endScale = Vector3.one * 3f;

        finalResultImage.rectTransform.localScale = startScale;
        while(t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            finalResultImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, n);
            yield return null;
        }
        finalResultImage.rectTransform.localScale = endScale;
    }
}
