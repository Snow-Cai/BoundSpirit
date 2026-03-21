using UnityEngine;
using UnityEngine.UI;

public class MapPopupUI : MonoBehaviour
{
    public static MapPopupUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button closeButton;

    private SceneTravelTrigger currentTrigger;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(SceneTravelTrigger trigger)
    {
        if (trigger == null) return;

        currentTrigger = trigger;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        foreach (SceneTravelTrigger.Destination destination in trigger.Destinations)
        {
            if (destination.button == null) continue;

            trigger.ConfigureButtonVisual(destination);

            destination.button.onClick.RemoveAllListeners();

            if (trigger.IsUnlocked(destination))
            {
                SceneTravelTrigger.Destination capturedDestination = destination;
                destination.button.onClick.AddListener(() =>
                {
                    CloseWithoutRestoringControl();
                    currentTrigger.TravelTo(capturedDestination);
                });
            }
        }
    }

    public void Close()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (currentTrigger != null)
            currentTrigger.RestorePlayerControl();

        currentTrigger = null;
    }

    private void CloseWithoutRestoringControl()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}