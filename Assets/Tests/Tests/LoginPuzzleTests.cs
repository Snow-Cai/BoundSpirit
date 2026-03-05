using NUnit.Framework;
using UnityEngine;
using TMPro;

public class LoginPuzzleTests
{
    [Test]
    public void TryLogin_CorrectCredentials_ShowsSuccessMessage()
    {
        // Create disabled puzzle object (not "Awake")
        GameObject puzzleObj = new GameObject();
        puzzleObj.SetActive(false);

        LoginPuzzle puzzle = puzzleObj.AddComponent<LoginPuzzle>();

        // Create username, password, and message fields
        GameObject usernameObj = new GameObject();
        usernameObj.AddComponent<RectTransform>();
        TMP_InputField usernameInput = usernameObj.AddComponent<TMP_InputField>();

        GameObject passwordObj = new GameObject();
        passwordObj.AddComponent<RectTransform>();
        TMP_InputField passwordInput = passwordObj.AddComponent<TMP_InputField>();

        GameObject messageObj = new GameObject();
        messageObj.AddComponent<RectTransform>();
        TMP_Text messageText = messageObj.AddComponent<TextMeshProUGUI>();

        puzzle.usernameInput = usernameInput;
        puzzle.passwordInput = passwordInput;
        puzzle.messageText = messageText;

        // Enable object ("Awake")
        puzzleObj.SetActive(true);

        // Login credentials to test 
        usernameInput.text = "akila";
        passwordInput.text = "2001eden";

        // Run login function
        puzzle.TryLogin();

        // Verify the result and report test result back
        Assert.AreEqual("Login Successful!", messageText.text);
    }
}