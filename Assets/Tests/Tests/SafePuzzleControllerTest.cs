using UnityEngine;
using NUnit.Framework;

public class SafePuzzleControllerT : MonoBehaviour
{
    [Test]
    public void IsCodeCorrect_ReturnsTrue_WhenPasswordMatchesAndKeyInserted()
    {
        var keypad = new SafeControllerKeypad();
        
        // set test code and simulate key as inserted
        keypad.targetCode = "3333";
        keypad.InsertKey();

        // test correct input
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");

        Assert.IsTrue(keypad.CheckSafeUnlockRequirements(), "The unlock requirements should be satisfied and return true when the correct code is provided and the key is inserted.");
    }

    [Test]
    public void IsCodeCorrect_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
        var keypad = new SafeControllerKeypad();

        // set test code and simulate key as inserted
        keypad.targetCode = "3333";
        keypad.InsertKey();

        // test correct input
        keypad.OnDigitPressed("5");
        keypad.OnDigitPressed("1");
        keypad.OnDigitPressed("4");
        keypad.OnDigitPressed("2");

        Assert.IsFalse(keypad.CheckSafeUnlockRequirements(), "The unlock requirements should not get satisfied and return false when the wrong code is provided.");
    }

    [Test]
    public void IsCodeCorrect_ReturnsFalse_WhenKeyIsMissing()
    {
        var keypad = new SafeControllerKeypad();

        // set test code
        keypad.targetCode = "3333";

        // test correct input
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");
        keypad.OnDigitPressed("3");

        Assert.IsFalse(keypad.CheckSafeUnlockRequirements(), "The unlock requirements should not get satisfied and return false when the correct code is provided but the key has not been inserted.");
    }
}