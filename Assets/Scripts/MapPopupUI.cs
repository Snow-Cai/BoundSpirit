using UnityEngine;
using UnityEngine.UI;

public class MapPopupUI : MonoBehaviour
{
    public static MapPopupUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button closeButton;

    private SceneTravelTrigger currentTrigger;
    private bool previousMovementLocked;
    private bool isMovementLockedByMap;

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

    private void OnDestroy()
    {
        UnlockPlayerMovement();

        if (Instance == this)
            Instance = null;
    }

    public void Open(SceneTravelTrigger trigger)
    {
        if (trigger == null) return;

        currentTrigger = trigger;
        LockPlayerMovement();

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
                    UnlockPlayerMovement();
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

        UnlockPlayerMovement();
        currentTrigger = null;
    }

    private void CloseWithoutRestoringControl()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void LockPlayerMovement()
    {
        if (isMovementLockedByMap) return;

        previousMovementLocked = GameInputState.MovementLocked;
        GameInputState.MovementLocked = true;
        isMovementLockedByMap = true;
    }

    private void UnlockPlayerMovement()
    {
        if (!isMovementLockedByMap) return;

        GameInputState.MovementLocked = previousMovementLocked;
        isMovementLockedByMap = false;
    }
}
