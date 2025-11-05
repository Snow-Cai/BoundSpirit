using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class DigitWheel : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI digitText;          // the UI Text showing the digit (0-9)
    public Button upButton;
    public Button downButton;

    [Header("Settings")]
    [Range(0, 9)] public int value = 0; // initial value

    void Reset()
    {
        // try to auto-wire common children if created in editor
        digitText = GetComponentInChildren<TextMeshProUGUI>();
        var buttons = GetComponentsInChildren<Button>();
        if (buttons.Length >= 2)
        {
            upButton = buttons[0];
            downButton = buttons[1];
        }
    }

    void Awake()
    {
        UpdateUI();
        if (upButton != null) upButton.onClick.AddListener(OnUp);
        if (downButton != null) downButton.onClick.AddListener(OnDown);
    }

    public void OnUp()
    {
        value = (value + 1) % 10;
        UpdateUI();
    }

    public void OnDown()
    {
        value = (value + 9) % 10; // +9 mod10 == -1 mod10
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (digitText != null) digitText.text = value.ToString();
    }

    // allows SafeController to query digit as char
    public char GetChar() => (char)('0' + value);
}
