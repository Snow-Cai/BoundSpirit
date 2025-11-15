using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using TMPro;

public class SafeController_SixDigits : MonoBehaviour
{
    [Header("Digits")]
    public DigitWheel[] digitWheels = new DigitWheel[6];

    [Header("UI")]
    public Button submitButton;
    public TextMeshProUGUI feedbackText;

    [Header("Safe Settings")]
    public string targetCode = "333333"; // default correct code

    [Header("Events")]
    public UnityEvent onUnlock;          // assign animations, item reveal, sound, etc.
    public UnityEvent onFail;            // optional fail reaction

    void Reset()
    {
        // default: try to find a submit button and text in children
        submitButton = GetComponentInChildren<Button>();
        feedbackText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Awake()
    {
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        if (feedbackText != null) feedbackText.text = "";
    }

    public void OnSubmit()
    {
        string code = ReadCode();
        if (code == targetCode)
        {
            HandleUnlock();
        }
        else
        {
            HandleFail(code);
        }
    }

    string ReadCode()
    {
        if (digitWheels == null || digitWheels.Length == 0) return "";
        var sb = new StringBuilder();
        foreach (var dw in digitWheels)
        {
            if (dw == null) sb.Append('0');
            else sb.Append(dw.GetChar());
        }
        return sb.ToString();
    }

    void HandleUnlock()
    {
        if (feedbackText != null) feedbackText.text = "Unlocked!";
        Debug.Log("[Safe] Unlocked with correct code.");
        onUnlock?.Invoke();
        // optional: disable UI or the whole safe
        // gameObject.SetActive(false);
    }

    void HandleFail(string attempted)
    {
        if (feedbackText != null) feedbackText.text = "Wrong code.";
        Debug.Log($"[Safe] Wrong code: {attempted}");
        onFail?.Invoke();
        // small UX tip: you can clear a field or play a shake animation here
    }

    // helper: programmatically set the digits (useful for tests)
    public void SetDigits(string code)
    {
        if (code == null) return;
        int len = Mathf.Min(code.Length, digitWheels.Length);
        for (int i = 0; i < len; i++)
        {
            if (digitWheels[i] != null)
            {
                char c = code[i];
                if (c >= '0' && c <= '9')
                    digitWheels[i].value = c - '0';
                digitWheels[i].UpdateUI();
            }
        }
    }
}
