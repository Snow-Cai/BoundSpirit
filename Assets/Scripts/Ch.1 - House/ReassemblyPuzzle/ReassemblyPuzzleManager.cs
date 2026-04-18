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

    [Header("Solve")]
    [SerializeField] private DialogueAsset solveDialogue;
    [SerializeField] private InteractableObject puzzleInteractable;

    private bool puzzleSolved;

    /// <summary>For PuzzlePiece and input guards — there must be exactly one manager per puzzle UI.</summary>
    public bool IsPuzzleSolved => puzzleSolved;

    private void Start()
    {
        if (puzzleInteractable != null &&
            SaveSystem.Instance != null &&
            !string.IsNullOrEmpty(puzzleInteractable.puzzleID) &&
            SaveSystem.Instance.IsPuzzleSolved(puzzleInteractable.puzzleID))
        {
            ApplyPersistedSolvedVisuals();
        }
    }

    private void OnDestroy()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueEnded -= HandleSolveDialogueEnded;
    }

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
        if (puzzleSolved)
            return;

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
        if (puzzleSolved)
            return;

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

        puzzleSolved = true;
        Debug.Log("Reassembly puzzle solved!");
        StartCoroutine(ShowCompletion());
    }

    /// <summary>Match save state without re-running dialogue or unlock (used when loading or syncing).</summary>
    public void ApplyPersistedSolvedVisuals()
    {
        puzzleSolved = true;
        if (puzzleGroupsParent != null)
            puzzleGroupsParent.SetActive(false);
        if (finalResultImage != null)
        {
            Color c = finalResultImage.color;
            c.a = 1f;
            finalResultImage.color = c;
            finalResultImage.rectTransform.localScale = Vector3.one * 3f;
        }
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
            t += Time.unscaledDeltaTime;
            float n = t / duration;
            finalResultImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, n);
            yield return null;
        }
        finalResultImage.rectTransform.localScale = endScale;

        if (puzzleInteractable != null && SaveSystem.Instance != null && !string.IsNullOrEmpty(puzzleInteractable.puzzleID))
            SaveSystem.Instance.UnlockPuzzle(puzzleInteractable.puzzleID);

        if (solveDialogue != null && DialogueSystem.Instance != null)
        {
            // Dialogue typing uses scaled time; puzzle leaves Time.timeScale at 0 until now.
            Time.timeScale = 1f;
            DialogueSystem.Instance.OnDialogueEnded += HandleSolveDialogueEnded;
            DialogueSystem.Instance.StartDialogue(solveDialogue);
        }
        else if (puzzleInteractable != null)
        {
            puzzleInteractable.ClosePuzzle();
        }
    }

    private void HandleSolveDialogueEnded(DialogueAsset asset)
    {
        if (asset != solveDialogue)
            return;

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueEnded -= HandleSolveDialogueEnded;

        if (puzzleInteractable != null)
            puzzleInteractable.ClosePuzzle();
    }
}
