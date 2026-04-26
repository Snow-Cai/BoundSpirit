using UnityEngine;

public class LibraryPuzzleStateBridge : MonoBehaviour
{
    public static LibraryPuzzleStateBridge Instance;

    [Header("References")]
    public CaesarDecodePanel caesarPanel;
    public PaintingPuzzleManager paintingPuzzle;

    [Header("State")]
    public bool cipherHalfSolved;
    public bool paintingSolved;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCipherHalfSolved()
    {
        cipherHalfSolved = true;
        paintingPuzzle.puzzleReady = true;
    }

    public void SetPaintingSolved()
    {
        paintingSolved = true;
    }

    public bool CanFinalize()
    {
        return cipherHalfSolved && paintingSolved;
    }
}
