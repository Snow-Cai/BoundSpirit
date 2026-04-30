using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class LoginPuzzle : MonoBehaviour
{
    private const string ForgotPasswordHintMessage = "Hint: Password format is year + important person's name in <i><b>lower case</b></i>. EX: YYYYname";
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

    private RectTransform usernameFeedbackRect;
    private RectTransform passwordFeedbackRect;
    private Graphic usernameGraphic;
    private Graphic passwordGraphic;
    [SerializeField] public RectTransform usernameShakeTarget;
    [SerializeField] public RectTransform passwordShakeTarget;
    private Color usernameOriginalColor;
    private Color passwordOriginalColor;
    private Vector2 usernameOriginalAnchoredPosition;
    private Vector2 passwordOriginalAnchoredPosition;
    private Vector2 usernameInputOriginalAnchoredPosition;
    private Vector2 passwordInputOriginalAnchoredPosition;
    private Vector2 usernameShakeTargetOriginalAnchoredPosition;
    private Vector2 passwordShakeTargetOriginalAnchoredPosition;
    private int wrongPasswordAttempts;
    private bool hasShownRecoveryThisSession;
    private float lastSubmitRealtime = -10f;

    void Awake()
    {
        usernameFeedbackRect = usernameInput.GetComponent<RectTransform>();
        passwordFeedbackRect = passwordInput.GetComponent<RectTransform>();
        usernameGraphic = usernameInput.targetGraphic;
        passwordGraphic = passwordInput.targetGraphic;
        usernameOriginalColor = usernameGraphic != null ? usernameGraphic.color : Color.white;
        passwordOriginalColor = passwordGraphic != null ? passwordGraphic.color : Color.white;
        usernameOriginalAnchoredPosition =
            usernameFeedbackRect != null ? usernameFeedbackRect.anchoredPosition : Vector2.zero;
        passwordOriginalAnchoredPosition =
            passwordFeedbackRect != null ? passwordFeedbackRect.anchoredPosition : Vector2.zero;
        usernameInputOriginalAnchoredPosition =
            usernameInput != null ? usernameInput.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;
        passwordInputOriginalAnchoredPosition =
            passwordInput != null ? passwordInput.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;
        usernameShakeTargetOriginalAnchoredPosition =
            usernameShakeTarget != null ? usernameShakeTarget.anchoredPosition : Vector2.zero;
        passwordShakeTargetOriginalAnchoredPosition =
            passwordShakeTarget != null ? passwordShakeTarget.anchoredPosition : Vector2.zero;
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
            TrySubmitFromKeyboard();
        }
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled || IsOpenComputerPanelActive())
            return;

        RestoreInputFieldVisualPositions();
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

        if (usernameGraphic != null)
        {
            usernameGraphic.color = usernameOriginalColor;
        }
        if (passwordGraphic != null)
        {
            passwordGraphic.color = passwordOriginalColor;
        }

        if (usernameInput.gameObject.activeInHierarchy)
        {
            SelectInputField(usernameInput);
        }

        RestoreInputFieldVisualPositions();
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

    private IEnumerator ShakeGraphicInPlace(RectTransform rect, Vector2 originalPosition, float duration = 0.25f, float strength = 10f)
    {
        Debug.Log("Shake started!");
        if (rect == null)
            yield break;

        rect.anchoredPosition = originalPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float horizontalOffset = Mathf.Sin(elapsed * 55f) * strength;
            rect.anchoredPosition = originalPosition + new Vector2(horizontalOffset, 0f);
            yield return null;
        }

        rect.anchoredPosition = originalPosition;
    }

    void ShakeUsername()
    {
        if (usernameFeedbackRect == null)
            return;

        if (usernameShakeCoroutine != null)
        {
            StopCoroutine(usernameShakeCoroutine);
            if (usernameFeedbackRect != null)
                usernameFeedbackRect.anchoredPosition = usernameOriginalAnchoredPosition;
        }

        usernameShakeCoroutine = StartCoroutine(
            ShakeGraphicInPlace(usernameShakeTarget, usernameShakeTargetOriginalAnchoredPosition)
        );
    }

    void ShakePassword()
    {
        if (passwordFeedbackRect == null)
            return;

        if (passwordShakeCoroutine != null)
        {
            StopCoroutine(passwordShakeCoroutine);
            if (passwordFeedbackRect != null)
                passwordFeedbackRect.anchoredPosition = passwordOriginalAnchoredPosition;
        }

        passwordShakeCoroutine = StartCoroutine(
            ShakeGraphicInPlace(passwordShakeTarget, passwordInputOriginalAnchoredPosition)
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
        if (Time.unscaledTime - lastSubmitRealtime < 0.15f)
        {
            return;
        }

        lastSubmitRealtime = Time.unscaledTime;
        TryLogin();
    }

    private void TrySubmitFromKeyboard()
    {
        if (EventSystem.current != null)
        {
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject != usernameInput?.gameObject &&
                selectedObject != passwordInput?.gameObject)
            {
                return;
            }
        }

        SubmitLogin();
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
        ForceComputerScrollLayoutRefresh();

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

        ForceComputerScrollLayoutRefresh();

        float contentHeight = computerScrollContent.rect.height * computerScrollContent.localScale.y;
        float viewportHeight = viewport.rect.height * viewport.localScale.y;
        float topY = ((1f - viewport.pivot.y) * viewportHeight) - ((1f - computerScrollContent.pivot.y) * contentHeight);
        float bottomY = (-viewport.pivot.y * viewportHeight) + (computerScrollContent.pivot.y * contentHeight);
        Vector2 anchoredPosition = computerScrollContent.anchoredPosition;
        anchoredPosition.y = Mathf.Lerp(topY, bottomY, Mathf.Clamp01(sliderValue));
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

    private void ForceComputerScrollLayoutRefresh()
    {
        Canvas.ForceUpdateCanvases();

        if (computerScrollViewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(computerScrollViewport);
        }

        if (computerScrollContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(computerScrollContent);
        }
    }

    private void RestoreInputFieldVisualPositions()
    {
        if (usernameInput != null)
            usernameInput.GetComponent<RectTransform>().anchoredPosition = usernameInputOriginalAnchoredPosition;

        if (passwordInput != null)
            passwordInput.GetComponent<RectTransform>().anchoredPosition = passwordInputOriginalAnchoredPosition;

        if (usernameFeedbackRect != null && usernameShakeCoroutine == null)
            usernameFeedbackRect.anchoredPosition = usernameOriginalAnchoredPosition;

        if (passwordFeedbackRect != null && passwordShakeCoroutine == null)
            passwordFeedbackRect.anchoredPosition = passwordOriginalAnchoredPosition;
    }
}
