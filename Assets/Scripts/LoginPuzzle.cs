using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
    public UnityEvent OnLoginSuccess; // scene load later
    public UnityEvent OnLoginFail; // play error SFX / shake animation

    public void TryLogin()
    {
        if (!usernameInput || !passwordInput) return;

        string u = (usernameInput.text ?? "").Trim(); // trims whitespace
        string p = (passwordInput.text ?? "").Trim();

        bool pass = u.Equals(correctUsername, System.StringComparison.OrdinalIgnoreCase) && p == correctPassword;

        if (pass)
        {
            if (messageText)
            {
                messageText.text = "Login Successful!";
                messageText.color = new Color(0.2f, 0.8f, 0.3f); // pass -> green
            }
            OnLoginSuccess?.Invoke();
        }
        else
        {
            if (messageText)
            {
                messageText.text = "Incorrect username or password.";
                messageText.color = new Color(0.85f, 0.2f, 0.2f); // fail -> red
            }
            OnLoginFail?.Invoke();
        }
    }
}
