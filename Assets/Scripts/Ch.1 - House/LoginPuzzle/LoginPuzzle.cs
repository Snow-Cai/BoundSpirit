using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class LoginPuzzle : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;
    public Button loginButton;

    [Header("Credentials")]
    public string correctUsername = "bunny";
    public string correctPassword = "2001eden";

    [Header("Events")]
    public UnityEvent OnLoginSuccess;
    public UnityEvent OnLoginFail;

    [Header("Story")]
    [Tooltip("Queued after a successful login (e.g. hint to visit parents' room).")]
    public DialogueAsset dialogueAfterSuccessfulLogin;

    private bool hasLoggedInBefore;
    private bool hasQueuedDialogueThisSession;
    private Coroutine usernameShakeCoroutine;
    private Coroutine passwordShakeCoroutine;

    private Vector2 usernameOriginalPos;
    private Vector2 passwordOriginalPos;

    void Awake()
    {
        usernameOriginalPos = usernameInput.GetComponent<RectTransform>().anchoredPosition;
        passwordOriginalPos = passwordInput.GetComponent<RectTransform>().anchoredPosition;
    }

    void OnEnable()
    {
        StartCoroutine(SelectUsernameNextFrame());
    }

    void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool moveBackward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            FocusNextInputField(moveBackward);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitLogin();
        }
    }

    public void ResetFields()
    {
        if (usernameInput == null || passwordInput == null) return;

        if (hasLoggedInBefore)
        {
            usernameInput.text = correctUsername;
            passwordInput.text = correctPassword;
        }
        else
        {
            usernameInput.text = "";
            passwordInput.text = "";
        }

        if (messageText != null)
            messageText.text = "";
        messageText.color = Color.white;

        if (usernameInput.GetComponent<RectTransform>() != null)
            usernameInput.GetComponent<RectTransform>().anchoredPosition = usernameOriginalPos;
        if (passwordInput.GetComponent<RectTransform>() != null)
            passwordInput.GetComponent<RectTransform>().anchoredPosition = passwordOriginalPos;

        if (usernameInput.gameObject.activeInHierarchy)
        {
            SelectInputField(usernameInput);
        }
    }

    private void OnDisable()
    {
        hasQueuedDialogueThisSession = false;

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

    private bool IsCurrentPuzzleAlreadySolved()
    {
        if (PuzzleBridge.currentPuzzleSource == null || SaveSystem.Instance == null)
            return false;

        string currentPuzzleId = PuzzleBridge.currentPuzzleSource.puzzleID;
        if (string.IsNullOrWhiteSpace(currentPuzzleId))
            return false;

        return SaveSystem.Instance.IsPuzzleSolved(currentPuzzleId);
    }

    private void FocusNextInputField(bool moveBackward)
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        TMP_InputField nextField = usernameInput;

        if (selectedObject == usernameInput?.gameObject)
        {
            nextField = moveBackward ? passwordInput : passwordInput;
        }
        else if (selectedObject == passwordInput?.gameObject)
        {
            nextField = moveBackward ? usernameInput : usernameInput;
        }
        else
        {
            nextField = moveBackward ? passwordInput : usernameInput;
        }

        SelectInputField(nextField);
    }

    private void SelectInputField(TMP_InputField inputField)
    {
        if (inputField == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    private void SubmitLogin()
    {
        if (loginButton != null && loginButton.interactable)
        {
            loginButton.onClick.Invoke();
            return;
        }

        TryLogin();
    }

    private IEnumerator SelectUsernameNextFrame()
    {
        yield return null;
        SelectInputField(usernameInput);
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
            bool firstSuccessfulSolve = !IsCurrentPuzzleAlreadySolved();
            hasLoggedInBefore = true;
            messageText.text = "Login Successful!";
            messageText.color = new Color(0.2f, 0.8f, 0.3f);

            if (firstSuccessfulSolve && PuzzleBridge.currentPuzzleSource != null)
            {
                PuzzleBridge.currentPuzzleSource.OnPuzzleSolved();
            }

            if (!hasQueuedDialogueThisSession &&
                dialogueAfterSuccessfulLogin != null &&
                DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.QueueDialogue(dialogueAfterSuccessfulLogin);
                hasQueuedDialogueThisSession = true;
            }

            if (firstSuccessfulSolve)
            {
                OnLoginSuccess?.Invoke();
            }
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
