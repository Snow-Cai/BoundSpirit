using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public readonly struct WordSearchPlacement
{
    public WordSearchPlacement(string word, Vector2Int start, Vector2Int direction)
    {
        Word = word;
        Start = start;
        Direction = direction;
    }

    public string Word { get; }
    public Vector2Int Start { get; }
    public Vector2Int Direction { get; }
}

public sealed class WordSearchBoard
{
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),
        new(1, 0),
        new(1, 1),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, -1),
        new(-1, 1)
    };

    private readonly char[,] letters;
    private readonly List<WordSearchPlacement> placements;

    private WordSearchBoard(char[,] letters, List<WordSearchPlacement> placements)
    {
        this.letters = letters;
        this.placements = placements;
    }

    public int Size => letters.GetLength(0);
    public IReadOnlyList<WordSearchPlacement> Placements => placements;

    public char GetLetter(int row, int column) => letters[row, column];

    public static WordSearchBoard Create(IEnumerable<string> rawWords, int size, int seed)
    {
        if (rawWords == null)
            throw new ArgumentNullException(nameof(rawWords));

        if (size < 4)
            throw new ArgumentOutOfRangeException(nameof(size), "Word-search board must be at least 4x4.");

        List<string> words = rawWords
            .Select(NormalizeWord)
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct()
            .OrderByDescending(w => w.Length)
            .ToList();

        if (words.Count == 0)
            throw new InvalidOperationException("Word-search board requires at least one valid word.");

        if (words.Any(w => w.Length > size))
            throw new InvalidOperationException("One or more words are longer than the board size.");

        var random = new System.Random(seed);
        var letters = new char[size, size];
        var placements = new List<WordSearchPlacement>(words.Count);

        foreach (string word in words)
        {
            if (!TryPlaceWord(word, letters, placements, random))
                throw new InvalidOperationException($"Could not place word '{word}' on a {size}x{size} board.");
        }

        FillEmptyCells(letters, random);
        return new WordSearchBoard(letters, placements);
    }

    public static WordSearchBoard CreateFixed(IEnumerable<string> rawRows, IEnumerable<WordSearchPlacement> rawPlacements)
    {
        if (rawRows == null)
            throw new ArgumentNullException(nameof(rawRows));

        if (rawPlacements == null)
            throw new ArgumentNullException(nameof(rawPlacements));

        List<string> rows = rawRows.ToList();
        if (rows.Count == 0)
            throw new InvalidOperationException("Fixed word-search board requires at least one row.");

        int size = rows[0].Length;
        if (rows.Any(r => string.IsNullOrWhiteSpace(r) || r.Length != size))
            throw new InvalidOperationException("All fixed board rows must be non-empty and the same length.");

        var letters = new char[size, size];
        for (int row = 0; row < size; row++)
        {
            string normalizedRow = rows[row].ToUpperInvariant();
            for (int column = 0; column < size; column++)
                letters[row, column] = normalizedRow[column];
        }

        List<WordSearchPlacement> placements = rawPlacements
            .Select(p => new WordSearchPlacement(NormalizeWord(p.Word), p.Start, p.Direction))
            .ToList();

        if (placements.Count == 0)
            throw new InvalidOperationException("Fixed word-search board requires at least one placement.");

        foreach (WordSearchPlacement placement in placements)
            ValidatePlacement(letters, placement);

        RemoveAccidentalMatches(letters, placements);

        return new WordSearchBoard(letters, placements);
    }

    public bool TryGetMatchedWord(
        int startRow,
        int startColumn,
        int endRow,
        int endColumn,
        IReadOnlyCollection<string> alreadyFoundWords,
        out string matchedWord,
        out List<Vector2Int> path)
    {
        matchedWord = string.Empty;
        path = null;

        if (!TryBuildSelectionPath(startRow, startColumn, endRow, endColumn, out List<Vector2Int> selectionPath))
            return false;

        string selectedWord = BuildWordFromPath(selectionPath);
        string reversedWord = Reverse(selectedWord);

        foreach (WordSearchPlacement placement in placements)
        {
            if (alreadyFoundWords != null && alreadyFoundWords.Contains(placement.Word))
                continue;

            if (placement.Word == selectedWord || placement.Word == reversedWord)
            {
                matchedWord = placement.Word;
                path = selectionPath;
                return true;
            }
        }

        return false;
    }

    public List<Vector2Int> BuildPath(WordSearchPlacement placement)
    {
        var path = new List<Vector2Int>(placement.Word.Length);
        Vector2Int current = placement.Start;

        for (int i = 0; i < placement.Word.Length; i++)
        {
            path.Add(current);
            current += placement.Direction;
        }

        return path;
    }

    public string FormatWordList(IReadOnlyCollection<string> foundWords)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < placements.Count; i++)
        {
            string word = placements[i].Word;
            bool found = foundWords != null && foundWords.Contains(word);

            if (found)
                sb.Append("<s><color=#5A4B3A>");

            sb.Append(word);

            if (found)
                sb.Append("</color></s>");

            if (i < placements.Count - 1)
                sb.Append("    ");
        }

        return sb.ToString();
    }

    public static string NormalizeWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (char.IsLetter(c))
                sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    private static bool TryPlaceWord(
        string word,
        char[,] letters,
        List<WordSearchPlacement> placements,
        System.Random random)
    {
        const int maxAttempts = 400;
        int size = letters.GetLength(0);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int direction = Directions[random.Next(Directions.Length)];
            Vector2Int start = new(random.Next(size), random.Next(size));

            if (!CanPlaceWord(word, letters, start, direction))
                continue;

            WriteWord(word, letters, start, direction);
            placements.Add(new WordSearchPlacement(word, start, direction));
            return true;
        }

        return false;
    }

    private static bool CanPlaceWord(string word, char[,] letters, Vector2Int start, Vector2Int direction)
    {
        int size = letters.GetLength(0);
        Vector2Int current = start;

        for (int i = 0; i < word.Length; i++)
        {
            if (current.x < 0 || current.x >= size || current.y < 0 || current.y >= size)
                return false;

            char existing = letters[current.x, current.y];
            if (existing != '\0' && existing != word[i])
                return false;

            current += direction;
        }

        return true;
    }

    private static void WriteWord(string word, char[,] letters, Vector2Int start, Vector2Int direction)
    {
        Vector2Int current = start;
        for (int i = 0; i < word.Length; i++)
        {
            letters[current.x, current.y] = word[i];
            current += direction;
        }
    }

    private static void FillEmptyCells(char[,] letters, System.Random random)
    {
        for (int row = 0; row < letters.GetLength(0); row++)
        {
            for (int column = 0; column < letters.GetLength(1); column++)
            {
                if (letters[row, column] == '\0')
                    letters[row, column] = (char)('A' + random.Next(26));
            }
        }
    }

    private static Vector2Int GetEndCell(WordSearchPlacement placement)
    {
        return placement.Start + placement.Direction * (placement.Word.Length - 1);
    }

    private bool TryBuildSelectionPath(
        int startRow,
        int startColumn,
        int endRow,
        int endColumn,
        out List<Vector2Int> path)
    {
        path = null;

        int rowDelta = endRow - startRow;
        int columnDelta = endColumn - startColumn;

        int stepRow = rowDelta == 0 ? 0 : rowDelta / Mathf.Abs(rowDelta);
        int stepColumn = columnDelta == 0 ? 0 : columnDelta / Mathf.Abs(columnDelta);

        bool isHorizontal = rowDelta == 0 && columnDelta != 0;
        bool isVertical = columnDelta == 0 && rowDelta != 0;
        bool isDiagonal = Mathf.Abs(rowDelta) == Mathf.Abs(columnDelta) && rowDelta != 0;
        bool isSingleCell = rowDelta == 0 && columnDelta == 0;

        if (!isHorizontal && !isVertical && !isDiagonal && !isSingleCell)
            return false;

        int steps = Mathf.Max(Mathf.Abs(rowDelta), Mathf.Abs(columnDelta));
        path = new List<Vector2Int>(steps + 1);

        for (int i = 0; i <= steps; i++)
            path.Add(new Vector2Int(startRow + (stepRow * i), startColumn + (stepColumn * i)));

        return true;
    }

    private string BuildWordFromPath(List<Vector2Int> path)
    {
        var sb = new StringBuilder(path.Count);
        for (int i = 0; i < path.Count; i++)
            sb.Append(letters[path[i].x, path[i].y]);

        return sb.ToString();
    }

    private static string Reverse(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char[] chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    private static void ValidatePlacement(char[,] letters, WordSearchPlacement placement)
    {
        Vector2Int current = placement.Start;
        for (int i = 0; i < placement.Word.Length; i++)
        {
            if (current.x < 0 || current.x >= letters.GetLength(0) || current.y < 0 || current.y >= letters.GetLength(1))
                throw new InvalidOperationException($"Placement for '{placement.Word}' goes out of bounds.");

            if (letters[current.x, current.y] != placement.Word[i])
            {
                throw new InvalidOperationException(
                    $"Fixed board letter mismatch for '{placement.Word}' at row {current.x}, column {current.y}.");
            }

            current += placement.Direction;
        }
    }

    private static void RemoveAccidentalMatches(char[,] letters, List<WordSearchPlacement> placements)
    {
        var intendedPaths = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var intendedCells = new HashSet<Vector2Int>();

        for (int i = 0; i < placements.Count; i++)
        {
            WordSearchPlacement placement = placements[i];
            List<Vector2Int> path = BuildStaticPath(placement);
            string key = PathKey(path);

            if (!intendedPaths.TryGetValue(placement.Word, out HashSet<string> keys))
            {
                keys = new HashSet<string>(StringComparer.Ordinal);
                intendedPaths[placement.Word] = keys;
            }

            keys.Add(key);

            for (int cellIndex = 0; cellIndex < path.Count; cellIndex++)
                intendedCells.Add(path[cellIndex]);
        }

        bool changed;
        int safety = 0;

        do
        {
            changed = false;

            for (int row = 0; row < letters.GetLength(0); row++)
            {
                for (int column = 0; column < letters.GetLength(1); column++)
                {
                    for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                    {
                        Vector2Int direction = Directions[directionIndex];

                        for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
                        {
                            WordSearchPlacement target = placements[placementIndex];
                            if (!TryBuildPath(row, column, direction, target.Word.Length, letters.GetLength(0), out List<Vector2Int> candidatePath))
                                continue;

                            if (!PathMatchesWord(letters, candidatePath, target.Word))
                                continue;

                            string key = PathKey(candidatePath);
                            if (intendedPaths.TryGetValue(target.Word, out HashSet<string> keys) && keys.Contains(key))
                                continue;

                            if (BreakAccidentalMatch(letters, candidatePath, intendedCells))
                            {
                                changed = true;
                                goto RestartScan;
                            }
                        }
                    }
                }
            }

        RestartScan:
            safety++;
        }
        while (changed && safety < 200);
    }

    private static bool BreakAccidentalMatch(char[,] letters, List<Vector2Int> candidatePath, HashSet<Vector2Int> intendedCells)
    {
        for (int i = candidatePath.Count - 1; i >= 0; i--)
        {
            Vector2Int cell = candidatePath[i];
            if (intendedCells.Contains(cell))
                continue;

            char original = letters[cell.x, cell.y];
            for (char replacement = 'A'; replacement <= 'Z'; replacement++)
            {
                if (replacement == original)
                    continue;

                letters[cell.x, cell.y] = replacement;
                return true;
            }

            letters[cell.x, cell.y] = original;
        }

        return false;
    }

    private static bool TryBuildPath(int startRow, int startColumn, Vector2Int direction, int length, int size, out List<Vector2Int> path)
    {
        path = new List<Vector2Int>(length);
        Vector2Int current = new(startRow, startColumn);

        for (int i = 0; i < length; i++)
        {
            if (current.x < 0 || current.x >= size || current.y < 0 || current.y >= size)
            {
                path = null;
                return false;
            }

            path.Add(current);
            current += direction;
        }

        return true;
    }

    private static bool PathMatchesWord(char[,] letters, List<Vector2Int> path, string word)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (letters[path[i].x, path[i].y] != word[i])
                return false;
        }

        return true;
    }

    private static List<Vector2Int> BuildStaticPath(WordSearchPlacement placement)
    {
        var path = new List<Vector2Int>(placement.Word.Length);
        Vector2Int current = placement.Start;

        for (int i = 0; i < placement.Word.Length; i++)
        {
            path.Add(current);
            current += placement.Direction;
        }

        return path;
    }

    private static string PathKey(List<Vector2Int> path)
    {
        var sb = new StringBuilder(path.Count * 8);
        for (int i = 0; i < path.Count; i++)
            sb.Append(path[i].x).Append(',').Append(path[i].y).Append(';');

        return sb.ToString();
    }
}
