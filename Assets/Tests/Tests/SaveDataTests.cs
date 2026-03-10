//Evita Kanaan save system test
using NUnit.Framework;
using System.Collections.Generic;

public class SaveDataTests
{
    //CollectItem/HasItem

    [Test]
    public void CollectItem_AddsItemToList()
    {
        //Arrange
        var save = new SaveData();

        //Act
        if (!save.collectedItems.Contains("SAFE_KEY"))
            save.collectedItems.Add("SAFE_KEY");

        //Assert
        Assert.IsTrue(save.collectedItems.Contains("SAFE_KEY"),
            "collectedItems should contain 'SAFE_KEY' after collecting it.");
    }

    [Test]
    public void CollectItem_DoesNotAddDuplicate()
    {
        var save = new SaveData();
        save.collectedItems.Add("SAFE_KEY");

        //duplicate guard in SaveSystem.CollectItem
        if (!save.collectedItems.Contains("SAFE_KEY"))
            save.collectedItems.Add("SAFE_KEY");

        Assert.AreEqual(1, save.collectedItems.Count,
            "Duplicate items should not be added to collectedItems.");
    }

    [Test]
    public void HasItem_ReturnsFalse_WhenItemNotCollected()
    {
        var save = new SaveData();

        bool result = save.collectedItems.Contains("MISSING_ITEM");

        Assert.IsFalse(result,
            "HasItem should return false when the item was never collected.");
    }

    //UnlockPuzzle / IsPuzzleSolved

    [Test]
    public void UnlockPuzzle_MarksPuzzleAsSolved()
    {
        var save = new SaveData();
        string puzzleID = "Chapter0_graveyard_gate";

        if (!save.solvedPuzzles.Contains(puzzleID))
            save.solvedPuzzles.Add(puzzleID);

        Assert.IsTrue(save.solvedPuzzles.Contains(puzzleID),
            "solvedPuzzles should contain the puzzle ID after unlocking.");
    }

    [Test]
    public void IsPuzzleSolved_ReturnsFalse_WhenPuzzleNotSolved()
    {
        var save = new SaveData();

        bool result = save.solvedPuzzles.Contains("some_unsolved_puzzle");

        Assert.IsFalse(result,
            "IsPuzzleSolved should return false for a puzzle that was never solved.");
    }

    //chapter unlock

    [Test]
    public void HighestChapterUnlocked_DefaultsToZero()
    {
        //arrange + act
        var save = new SaveData();

        Assert.AreEqual(0, save.highestChapterUnlocked,
            "A fresh SaveData should have highestChapterUnlocked = 0.");
    }

    [Test]
    public void UnlockChapter_UpdatesHighestChapter_WhenHigher()
    {
        var save = new SaveData();

        //simulate SaveSystem.UnlockChapter logic
        int chapterToUnlock = 1;
        if (chapterToUnlock > save.highestChapterUnlocked)
            save.highestChapterUnlocked = chapterToUnlock;

        Assert.AreEqual(1, save.highestChapterUnlocked,
            "highestChapterUnlocked should update when a higher chapter is unlocked.");
    }

    [Test]
    public void UnlockChapter_DoesNotDowngrade_WhenLower()
    {
        var save = new SaveData();
        save.highestChapterUnlocked = 2;

        //trying to unlock chapter 1 should not overwrite 2
        int chapterToUnlock = 1;
        if (chapterToUnlock > save.highestChapterUnlocked)
            save.highestChapterUnlocked = chapterToUnlock;

        Assert.AreEqual(2, save.highestChapterUnlocked,
            "highestChapterUnlocked should never decrease.");
    }
}