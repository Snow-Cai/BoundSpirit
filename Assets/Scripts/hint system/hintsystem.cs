using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HintSystem : MonoBehaviour
{

    [Header("HUD Button")]
    [Tooltip("The '?' button on the HUD the player presses to request a hint.")]
    [SerializeField] private Button hintButton;

    [Header("Popup")]
    [Tooltip("Root panel of the hint popup (enable/disable to show/hide).")]
    [SerializeField] private GameObject hintPopupPanel;

    [Tooltip("Text field inside the popup that shows the hint.")]
    [SerializeField] private TextMeshProUGUI hintPopupText;

    [Tooltip("Close/dismiss button inside the popup.")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button xButton;

    [Tooltip("Optional label that shows the cooldown countdown.")]
    [SerializeField] private TextMeshProUGUI cooldownLabel;

    [Header("Cooldown")]
    [Tooltip("Seconds the player must wait between hints.")]
    [SerializeField] private float hintCooldown = 30f;

    [Header("Hint Data — one asset per scene")]
    [Tooltip("Drag in every SceneHintData asset you've created. Matched by sceneName field.")]
    [SerializeField] private List<SceneHintData> sceneHints = new List<SceneHintData>();

    [Header("Fallback")]
    [Tooltip("Shown when all puzzles in the current scene are solved, or no hint data exists.")]
    [SerializeField]
    [TextArea(2, 4)]
    private string allSolvedMessage = "You've solved everything here. Time to move on.";

    private float lastHintTime = -9999f;
    private bool popupOpen = false;

    private void Awake()
    {
        if (hintPopupPanel != null)
            hintPopupPanel.SetActive(false);

        if (hintButton != null)
            hintButton.onClick.AddListener(OnHintButtonPressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        if (xButton != null)
            xButton.onClick.AddListener(ClosePopup);
    }

    private void Update()
    {
        RefreshButtonInteractable();
        UpdateCooldownLabel();

        // Allow closing with Escape
        //if (popupOpen && Input.GetKeyDown(KeyCode.Escape))
            //ClosePopup();
    }

    public void ShowHint()
    {
        OnHintButtonPressed();
    }

    private void OnHintButtonPressed()
    {
        if (!CanShowHint())
            return;

        string hintText = GetHintForCurrentScene();

        if (string.IsNullOrWhiteSpace(hintText))
            hintText = allSolvedMessage;

        OpenPopup(hintText);
        lastHintTime = Time.unscaledTime;
    }

    private bool CanShowHint()
    {
        //respect global input locks (puzzle UI open, dialogue active, etc.)
        if (GameInputState.DialogueActive)
            return false;

        if (GameInputState.MovementLocked)
            return false;

        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return false;

        if (Time.timeScale == 0f)
            return false;

        if (popupOpen)
            return false;

        if (Time.unscaledTime - lastHintTime < hintCooldown)
            return false;

        return true;
    }

    private void RefreshButtonInteractable()
    {
        if (hintButton == null)
            return;

        bool onCooldown = (Time.unscaledTime - lastHintTime) < hintCooldown;
        bool blocked = GameInputState.DialogueActive ||
                          GameInputState.MovementLocked ||
                          (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled) ||
                          Time.timeScale == 0f ||
                          popupOpen;

        hintButton.interactable = !onCooldown && !blocked;
    }

    private void UpdateCooldownLabel()
    {
        if (cooldownLabel == null)
            return;

        float elapsed = Time.unscaledTime - lastHintTime;
        float remaining = hintCooldown - elapsed;

        if (remaining > 0f && elapsed >= 0f && lastHintTime > -9000f)
        {
            cooldownLabel.text = Mathf.CeilToInt(remaining).ToString() + "s";
            cooldownLabel.gameObject.SetActive(true);
        }
        else
        {
            cooldownLabel.gameObject.SetActive(false);
        }
    }

    private string GetHintForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        foreach (SceneHintData data in sceneHints)
        {
            if (data == null)
                continue;

            if (data.name.IndexOf(sceneName, System.StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf(data.name.Replace("HintData_", ""), System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return data.GetCurrentHint();
            }
        }

        return null;
    }

    private void OpenPopup(string text)
    {
        if (hintPopupPanel == null)
            return;

        popupOpen = true;

        if (hintPopupText != null)
            hintPopupText.text = text;

        hintPopupPanel.SetActive(true);

        //[revent gameplay input while popup is open
        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = false;
    }

    private void ClosePopup()
    {
        if (hintPopupPanel != null)
            hintPopupPanel.SetActive(false);

        popupOpen = false;

        //Restore gameplay input
        if (InputLock.Instance != null)
            InputLock.Instance.GameplayInputEnabled = true;
    }
}