using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class LoginPuzzle : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;

    [Header("Credentials")]
    public string correctUsername = "admin";
    public string correctPassword = "password123";

    [Header("Events")]
    public UnityEvent OnLoginSuccess;
    public UnityEvent OnLoginFail;

    [Header("Tidbit Popup")]
    [TextArea] public string tidbitMessage = "This is a tidbit message shown after solving the login puzzle.";

    public void TryLogin()
    {
        string u = usernameInput.text.Trim();
        string p = passwordInput.text.Trim();

        bool correct = u.Equals(correctUsername, System.StringComparison.OrdinalIgnoreCase) && p == correctPassword;

        if (correct)
        {
            messageText.text = "Login Successful!";
            messageText.color = new Color(0.2f, 0.8f, 0.3f);

            UICluePopup popup = FindAnyObjectByType<UICluePopup>();
            if (popup != null)
            {
                popup.ShowClue(tidbitMessage);
            }
            else
            {
                Debug.LogError("No UICluePopup found in this scene!");
            }

            OnLoginSuccess?.Invoke();
        }
        else
        {
            messageText.text = "Incorrect username or password.";
            messageText.color = new Color(0.9f, 0.2f, 0.2f);

            OnLoginFail?.Invoke();
        }
    }
}
