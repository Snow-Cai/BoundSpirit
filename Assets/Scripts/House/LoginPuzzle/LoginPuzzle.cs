using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;


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
    public UICluePopup cluePopup;
    [TextArea] public string tidbitMessage = "This is a tidbit message shown after solving the login puzzle.";

    private Coroutine usernameShakeCoroutine;
    private Coroutine passwordShakeCoroutine;

    private Vector2 usernameOriginalPos;
    private Vector2 passwordOriginalPos;

    void Awake()
    {
        usernameOriginalPos = usernameInput.GetComponent<RectTransform>().anchoredPosition;
        passwordOriginalPos = passwordInput.GetComponent<RectTransform>().anchoredPosition;
    }

    public void ResetFields()
    {
        if (usernameInput == null || passwordInput == null) return;

        usernameInput.text = "";
        passwordInput.text = "";

        if (messageText != null)
            messageText.text = "";
        messageText.color = Color.white;

        if (usernameInput.GetComponent<RectTransform>() != null)
            usernameInput.GetComponent<RectTransform>().anchoredPosition = usernameOriginalPos;
        if (passwordInput.GetComponent<RectTransform>() != null)
            passwordInput.GetComponent<RectTransform>().anchoredPosition = passwordOriginalPos;
    }

    private void OnDisable()
    {
        if (usernameInput != null && passwordInput != null)
            ResetFields();
    }

    private IEnumerator ShakeUI(RectTransform target, Vector2 originalPos, float duration = 0.25f, float magnitude = 5f)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            target.anchoredPosition = originalPos + new Vector2(x, y);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.anchoredPosition = originalPos;
    }

    void ShakeUsername()
    {
        RectTransform rt = usernameInput.GetComponent<RectTransform>();

        if (usernameShakeCoroutine != null)
            StopCoroutine(usernameShakeCoroutine);

        rt.anchoredPosition = usernameOriginalPos;

        usernameShakeCoroutine = StartCoroutine(
            ShakeUI(rt, usernameOriginalPos)
        );
    }

    void ShakePassword()
    {
        RectTransform rt = passwordInput.GetComponent<RectTransform>();

        if (passwordShakeCoroutine != null)
            StopCoroutine(passwordShakeCoroutine);

        rt.anchoredPosition = passwordOriginalPos;

        passwordShakeCoroutine = StartCoroutine(
            ShakeUI(rt, passwordOriginalPos)
        );
    }


    public void TryLogin()
    {
        string u = usernameInput.text.Trim();
        string p = passwordInput.text.Trim();

        bool usernameCorrect =
            u.Equals(correctUsername, System.StringComparison.OrdinalIgnoreCase);

        bool passwordCorrect =
            p == correctPassword;

        // both right
        if (usernameCorrect && passwordCorrect)
        {
            messageText.text = "Login Successful!";
            messageText.color = new Color(0.2f, 0.8f, 0.3f);

            if (cluePopup != null)
            {
                cluePopup.enabled = true;
                cluePopup.ShowClue(tidbitMessage);
            }

            OnLoginSuccess?.Invoke();
        }
        // both wrong
        else if (!usernameCorrect && !passwordCorrect)
        {
            messageText.text = "Wrong username and password!";
            messageText.color = new Color(0.9f, 0.2f, 0.2f);
            ShakeUsername();
            ShakePassword();

            OnLoginFail?.Invoke();
        }
        // User wrong
        else if (!usernameCorrect)
        {
            messageText.text = "Wrong username!";
            messageText.color = new Color(0.9f, 0.2f, 0.2f);
            ShakeUsername();

            OnLoginFail?.Invoke();
        }
        // password wrong
        else
        {
            messageText.text = "Wrong password!";
            messageText.color = new Color(0.9f, 0.2f, 0.2f);
            ShakePassword();

            OnLoginFail?.Invoke();
        }
    }
}
