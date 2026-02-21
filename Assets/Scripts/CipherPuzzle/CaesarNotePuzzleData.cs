using UnityEngine;

[CreateAssetMenu(menuName = "Puzzles/Caesar Note Puzzle", fileName = "CaesarNotePuzzle")]
public sealed class CaesarNotePuzzleData : ScriptableObject
{
    [Header("Puzzle Identity")]
    [Tooltip("Unique key stored in SaveSystem.solvedPuzzles.")]
    [SerializeField] private string puzzleKey = "Caesar_Note_Library";

    [Header("Content")]
    [TextArea(2, 8)]
    [SerializeField] private string plaintext = "Meet me at the library";

    [Range(0, 25)]
    [SerializeField] private int shift = 7;

    public string PuzzleKey => puzzleKey;
    public string Plaintext => plaintext;
    public int Shift => shift;
}
