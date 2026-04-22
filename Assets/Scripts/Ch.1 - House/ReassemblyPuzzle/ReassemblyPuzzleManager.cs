using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReassemblyPuzzleManager : MonoBehaviour
{
    public PuzzlePiece[] pieces;

    [SerializeField] private Image finalResultImage;

    [Header("Solve")]
    [SerializeField] private DialogueAsset solveDialogue;
    [SerializeField] private InteractableObject puzzleInteractable;

    public void CheckPuzzleCompletion()
    {
        foreach (var piece in pieces)
        {
            if (!piece.IsPlaced())
                return;
        }
        Debug.Log("Reassembly puzzle solved!");
        StartCoroutine(ShowCompletion());
    }

    IEnumerator ShowCompletion()
    {
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
