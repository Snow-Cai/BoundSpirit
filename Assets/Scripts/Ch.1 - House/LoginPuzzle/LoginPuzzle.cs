using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class LoginPuzzle : MonoBehaviour
{
    private const string ForgotPasswordHintMessage = "Hint: Password format is year + important person's name in lower case. EX: YYYYname";
    private const string ForgotUsernameHintMessage = "Hint: What dad calls me";
    private const string RecoveryTriggeredMessage = "Password entry wrong more than 5 times. Initiating recovery.";

    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;
    public Button forgotPasswordButton;
    public Button forgotUsernameButton;
    public GameObject hintPopupCanvas;
    public GameObject loginPanelRoot;
    public GameObject openComputerPanel;

    [Header("Open Computer Scroll")]
    public Scrollbar computerScrollBar;
    public RectTransform computerScrollContent;
    public RectTransform computerScrollViewport;
    public float mouseWheelScrollSpeed = 0.15f;

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
    private Coroutine recoveryPopupCoroutine;
    private bool computerScrollHooked;

    private RectTransform usernameShakeTarget;
    private RectTransform passwordShakeTarget;
    private Vector2 usernameOriginalPos;
    private Vector2 passwordOriginalPos;
    private int wrongPasswordAttempts;
    private bool hasShownRecoveryThisSession;

    void Awake()
    {
        usernameShakeTarget = GetShakeTarget(usernameInput);
        passwordShakeTarget = GetShakeTarget(passwordInput);
        usernameOriginalPos = usernameShakeTarget != null ? usernameShakeTarget.anchoredPosition : Vector2.zero;
        passwordOriginalPos = passwordShakeTarget != null ? passwordShakeTarget.anchoredPosition : Vector2.zero;
        ResolveLoginPanelRoot();
        HookComputerScrollBar();
    }

    void OnEnable()
    {
        RefreshComputerPanelState();
        StartCoroutine(SelectUsernameNextFrame());
    }

    void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (IsOpenComputerPanelActive())
        {
            HandleMouseWheelScroll();
            return;
        }

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

        if (recoveryPopupCoroutine != null)
        {
            StopCoroutine(recoveryPopupCoroutine);
            recoveryPopupCoroutine = null;
        }

        if (messageText != null)
        {
            messageText.text = "";
            messageText.color = Color.white;
        }

        wrongPasswordAttempts = 0;
        hasShownRecoveryThisSession = false;

        if (usernameShakeTarget != null)
            usernameShakeTarget.anchoredPosition = usernameOriginalPos;
        if (passwordShakeTarget != null)
            passwordShakeTarget.anchoredPosition = passwordOriginalPos;

        if (usernameInput.gameObject.activeInHierarchy)
        {
            SelectInputField(usernameInput);
        }

        RefreshComputerPanelState();
    }

    private void OnDisable()
    {
        hasQueuedDialogueThisSession = false;

        if (recoveryPopupCoroutine != null)
        {
            StopCoroutine(recoveryPopupCoroutine);
            recoveryPopupCoroutine = null;
        }

        if (usernameInput != null && passwordInput != null)
            ResetFields();
    }

    private void OnDestroy()
    {
        UnhookComputerScrollBar();
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
        RectTransform rt = usernameShakeTarget;
        if (rt == null)
            return;

        if (usernameShakeCoroutine != null)
            StopCoroutine(usernameShakeCoroutine);

        rt.anchoredPosition = usernameOriginalPos;

        usernameShakeCoroutine = StartCoroutine(
            ShakeUI(rt, usernameOriginalPos)
        );
    }

    void ShakePassword()
    {
        RectTransform rt = passwordShakeTarget;
        if (rt == null)
            return;

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
        TryLogin();
    }

    private IEnumerator SelectUsernameNextFrame()
    {
        yield return null;

        if (!IsOpenComputerPanelActive())
        {
            SelectInputField(usernameInput);
        }
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
            wrongPasswordAttempts = 0;
            hasShownRecoveryThisSession = false;
            messageText.text = "Login Successful!";
            messageText.color = new Color(0.2f, 0.8f, 0.3f);

            if (firstSuccessfulSolve && PuzzleBridge.currentPuzzleSource != null)
            {
                PuzzleBridge.currentPuzzleSource.OnPuzzleSolved();
            }

            RefreshComputerPanelState();

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
            HandleWrongPasswordAttempt();

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
            HandleWrongPasswordAttempt();

            OnLoginFail?.Invoke();
        }
    }

    public void ShowForgotPasswordHint()
    {
        ShowHintPopup(ForgotPasswordHintMessage);
    }

    public void ShowForgotUsernameHint()
    {
        ShowHintPopup(ForgotUsernameHintMessage);
    }

    private void HandleWrongPasswordAttempt()
    {
        wrongPasswordAttempts++;

        if (hasShownRecoveryThisSession || wrongPasswordAttempts <= 5)
        {
            return;
        }

        hasShownRecoveryThisSession = true;

        if (recoveryPopupCoroutine != null)
        {
            StopCoroutine(recoveryPopupCoroutine);
        }

        recoveryPopupCoroutine = StartCoroutine(ShowRecoveryThenHint());
    }

    private IEnumerator ShowRecoveryThenHint()
    {
        UICluePopup popup = ResolveHintPopup();
        if (popup == null)
        {
            ShowForgotPasswordHint();
            recoveryPopupCoroutine = null;
            yield break;
        }

        popup.ShowTransientMessage(RecoveryTriggeredMessage, 1.5f);

        while (popup != null && popup.IsPopupOpen())
        {
            yield return null;
        }

        popup = ResolveHintPopup();
        if (popup != null)
        {
            popup.ShowMessage(ForgotPasswordHintMessage);
        }

        recoveryPopupCoroutine = null;
    }

    private void ShowMessage(string text, Color color)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = text;
        messageText.color = color;
    }

    private void ShowHintPopup(string message)
    {
        UICluePopup popup = ResolveHintPopup();
        if (popup != null)
        {
            popup.ShowMessage(message);
            return;
        }

        ShowMessage(message, new Color(0.95f, 0.9f, 0.45f));
    }

    private UICluePopup ResolveHintPopup()
    {
        if (hintPopupCanvas != null)
        {
            UICluePopup popupFromCanvas = hintPopupCanvas.GetComponent<UICluePopup>();
            if (popupFromCanvas != null)
            {
                return popupFromCanvas;
            }

            popupFromCanvas = hintPopupCanvas.GetComponentInChildren<UICluePopup>(true);
            if (popupFromCanvas != null)
            {
                return popupFromCanvas;
            }
        }

        return FindFirstObjectByType<UICluePopup>(FindObjectsInactive.Include);
    }

    private void ResolveLoginPanelRoot()
    {
        if (loginPanelRoot != null || usernameInput == null)
        {
            return;
        }

        Transform current = usernameInput.transform;
        Transform candidate = null;

        while (current != null)
        {
            if (current.GetComponent<Canvas>() != null)
            {
                break;
            }

            candidate = current;
            current = current.parent;
        }

        if (candidate != null)
        {
            loginPanelRoot = candidate.gameObject;
        }
    }

    private void RefreshComputerPanelState()
    {
        ResolveLoginPanelRoot();

        bool unlocked = IsComputerUnlocked();

        if (loginPanelRoot != null)
        {
            loginPanelRoot.SetActive(!unlocked);
        }

        if (openComputerPanel != null)
        {
            openComputerPanel.SetActive(unlocked);
        }

        if (unlocked)
        {
            ResetComputerScroll();
        }
    }

    private bool IsComputerUnlocked()
    {
        return hasLoggedInBefore || IsCurrentPuzzleAlreadySolved();
    }

    private bool IsOpenComputerPanelActive()
    {
        return openComputerPanel != null && openComputerPanel.activeInHierarchy;
    }

    private void HookComputerScrollBar()
    {
        if (computerScrollHooked || computerScrollBar == null)
        {
            return;
        }

        computerScrollBar.onValueChanged.AddListener(HandleComputerScrollChanged);
        computerScrollHooked = true;
    }

    private void UnhookComputerScrollBar()
    {
        if (!computerScrollHooked || computerScrollBar == null)
        {
            return;
        }

        computerScrollBar.onValueChanged.RemoveListener(HandleComputerScrollChanged);
        computerScrollHooked = false;
    }

    private void HandleComputerScrollChanged(float sliderValue)
    {
        UpdateComputerScroll(sliderValue);
    }

    private void ResetComputerScroll()
    {
        if (computerScrollBar != null)
        {
            computerScrollBar.SetValueWithoutNotify(0f);
        }

        UpdateComputerScroll(0f);
    }

    private void UpdateComputerScroll(float sliderValue)
    {
        if (computerScrollContent == null)
        {
            return;
        }

        RectTransform viewport = computerScrollViewport;
        if (viewport == null && openComputerPanel != null)
        {
            viewport = openComputerPanel.GetComponent<RectTransform>();
        }

        if (viewport == null)
        {
            return;
        }

        float maxScroll = Mathf.Max(0f, computerScrollContent.rect.height - viewport.rect.height);
        Vector2 anchoredPosition = computerScrollContent.anchoredPosition;
        anchoredPosition.y = Mathf.Clamp01(sliderValue) * maxScroll;
        computerScrollContent.anchoredPosition = anchoredPosition;
    }

    private void HandleMouseWheelScroll()
    {
        if (computerScrollBar == null)
        {
            return;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        float nextValue = Mathf.Clamp01(computerScrollBar.value - scrollDelta * mouseWheelScrollSpeed);
        computerScrollBar.value = nextValue;
    }

    private static RectTransform GetShakeTarget(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return null;
        }

        if (inputField.targetGraphic != null)
        {
            return inputField.targetGraphic.rectTransform;
        }

        return inputField.GetComponent<RectTransform>();
    }
}
