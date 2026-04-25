using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WordSearchBoardTests
{
    [Test]
    public void Create_PlacesEachTargetWord()
    {
        List<string> words = new() { "LIBRARY", "CIPHER", "SHIFT", "GHOST", "CLUE", "LETTER", "SPIRIT" };

        WordSearchBoard board = WordSearchBoard.Create(words, 10, 1307);

        Assert.AreEqual(words.Count, board.Placements.Count);
        foreach (WordSearchPlacement placement in board.Placements)
            Assert.Contains(placement.Word, words);
    }

    [Test]
    public void TryGetMatchedWord_AcceptsReversedDragAcrossPlacedWord()
    {
        List<string> words = new() { "LIBRARY", "CIPHER", "SHIFT", "GHOST", "CLUE", "LETTER", "SPIRIT" };
        WordSearchBoard board = WordSearchBoard.Create(words, 10, 1307);

        WordSearchPlacement placement = board.Placements[0];
        List<Vector2Int> path = board.BuildPath(placement);
        Vector2Int start = path[^1];
        Vector2Int end = path[0];

        bool matched = board.TryGetMatchedWord(
            start.x,
            start.y,
            end.x,
            end.y,
            new HashSet<string>(),
            out string matchedWord,
            out List<Vector2Int> matchedPath);

        Assert.IsTrue(matched);
        Assert.AreEqual(placement.Word, matchedWord);
        Assert.AreEqual(placement.Word.Length, matchedPath.Count);
    }

    [Test]
    public void TryGetMatchedWord_RejectsUnknownLine()
    {
        List<string> words = new() { "LIBRARY", "CIPHER", "SHIFT", "GHOST", "CLUE", "LETTER", "SPIRIT" };
        WordSearchBoard board = WordSearchBoard.Create(words, 10, 1307);

        bool matched = board.TryGetMatchedWord(
            0,
            0,
            0,
            2,
            new HashSet<string>(),
            out _,
            out _);

        Assert.IsFalse(matched);
    }

    [Test]
    public void CreateFixed_UsesHardCodedPlacements()
    {
        string[] rows =
        {
            "QWERTYUIOPASDFG",
            "LKJHGFDSAZXCVBN",
            "MNBVCXLIBRARYRY",
            "QWESASBUIOPLCJC",
            "TYUHOHZNMASDLGL",
            "ASDIGIOMQWERUYU",
            "POIFYFKLKJHGEDE",
            "ZXCTBTCIPHERYRM",
            "QAZWSXEDCRFVGAB",
            "MNBBCBZLKJHGHQH",
            "PLMOKOIJBUHVODO",
            "TREOQOKJHGFDSOS",
            "YUIKPLSPIRITTWT",
            "CVBSMSSDFGHJKLZ",
            "XCVBNMQWERTYUIO"
        };

        List<WordSearchPlacement> placements = new()
        {
            new WordSearchPlacement("BOOKS", new Vector2Int(9, 3), new Vector2Int(1, 0)),
            new WordSearchPlacement("SHIFT", new Vector2Int(3, 3), new Vector2Int(1, 0)),
            new WordSearchPlacement("LIBRARY", new Vector2Int(2, 6), new Vector2Int(0, 1)),
            new WordSearchPlacement("CLUE", new Vector2Int(3, 12), new Vector2Int(1, 0)),
            new WordSearchPlacement("CIPHER", new Vector2Int(7, 6), new Vector2Int(0, 1)),
            new WordSearchPlacement("GHOST", new Vector2Int(8, 12), new Vector2Int(1, 0)),
            new WordSearchPlacement("SPIRIT", new Vector2Int(12, 6), new Vector2Int(0, 1))
        };

        WordSearchBoard board = WordSearchBoard.CreateFixed(rows, placements);

        Assert.AreEqual('S', board.GetLetter(3, 3));
        Assert.AreEqual('B', board.GetLetter(9, 3));
        Assert.AreEqual('L', board.GetLetter(2, 6));
        Assert.AreEqual(7, board.Placements.Count);
    }
}
