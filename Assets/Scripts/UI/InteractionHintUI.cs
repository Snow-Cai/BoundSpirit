using UnityEngine;
using TMPro;

/// <summary>
/// Global interaction hint that shows "Press E to interact" (or similar)
/// when the player is near any InteractableObject or GraveyardGateController.
/// Place one instance in the scene and it will work for all supported interactables.
/// </summary>
public class InteractionHintUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private TextMeshProUGUI hintLabel;

    [Header("Config")]
    [Tooltip("Maximum distance to show the interaction hint.")]
    [SerializeField] private float maxDistance = 2f;

    [Tooltip("Format string for the hint. {0} will be the key label.")]
    [SerializeField] private string hintFormat = "Press {0} to interact";

    [Header("Behavior")]
    [Tooltip("Hide the hint while dialogue or puzzles are active.")]
    [SerializeField] private bool hideWhenDialogueActive = true;

    [Tooltip("Do not show interaction hints until the intro dialogue has played.")]
    [SerializeField] private bool hideUntilIntroDialogueSeen = true;
    [SerializeField] private string introDialogueID = "Chapter0_awakening";
    [SerializeField] private float fadeSpeed = 10f;

    private Transform player;
    private float targetAlpha;

    private void Awake()
    {
        if (hintCanvasGroup == null)
        {
            hintCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (hintCanvasGroup != null)
        {
            hintCanvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        EnsurePlayerReference();

        if (player == null || hintCanvasGroup == null || hintLabel == null)
        {
            return;
        }

        bool endingPresentationActive =
            EndingManager.Instance != null &&
            EndingManager.Instance.IsEndingPresentationActive;

        bool inspectOpen = InspectUI.Instance != null && InspectUI.Instance.IsOpen;
        bool puzzleOverlayOpen = CaesarDecodePanel.IsPanelActuallyOpen || LibraryWordSearchPanel.IsPanelActuallyOpen;
        bool gameplayInputBlocked = InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled;

        if ((hideWhenDialogueActive && (GameInputState.DialogueActive || endingPresentationActive)) ||
            puzzleOverlayOpen ||
            gameplayInputBlocked ||
            inspectOpen ||
            !IntroDialogueHasPlayed())
        {
            SetTargetVisible(false);
            ApplyFade();
            return;
        }

        bool hasTarget = TryFindNearestTarget(out Vector2 targetPosition, out KeyCode targetKey);

        if (hasTarget)
        {
            UpdateHintText(targetKey);
            SetTargetVisible(true);
        }
        else
        {
            SetTargetVisible(false);
        }

        ApplyFade();
    }

    private void EnsurePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private bool TryFindNearestTarget(out Vector2 targetPosition, out KeyCode targetKey)
    {
        targetPosition = Vector2.zero;
        targetKey = KeyCode.None;

        if (player == null)
        {
            return false;
        }

        if (!InteractionPriorityResolver.TryGetHighestPriorityKey(player, out targetKey))
        {
            return false;
        }

        targetPosition = player.position;
        return true;
    }

    private void UpdateHintText(KeyCode key)
    {
        string keyLabel = FormatKeyLabel(key);

        if (string.IsNullOrEmpty(hintFormat))
        {
            hintLabel.text = keyLabel;
        }
        else
        {
            hintLabel.text = string.Format(hintFormat, keyLabel);
        }
    }

    private bool IntroDialogueHasPlayed()
    {
        if (!hideUntilIntroDialogueSeen)
        {
            return true;
        }

        if (SaveSystem.Instance == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(introDialogueID))
        {
            return true;
        }

        return SaveSystem.Instance.HasViewedDialogue(introDialogueID);
    }

    private string FormatKeyLabel(KeyCode key)
    {
        return key.ToString().ToUpperInvariant();
    }

    private void SetTargetVisible(bool visible)
    {
        targetAlpha = visible ? 1f : 0f;
    }

    private void ApplyFade()
    {
        float newAlpha = Mathf.MoveTowards(
            hintCanvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );

        hintCanvasGroup.alpha = newAlpha;
        bool visible = newAlpha > 0.01f;

        hintCanvasGroup.interactable = false;
        hintCanvasGroup.blocksRaycasts = false;

        if (!visible)
        {
            hintLabel.text = string.Empty;
        }
    }
}

public static class InteractionPriorityResolver
{
    private static int lastConsumedFrame = -1;

    private enum InteractionPriority
    {
        Collectible = 0,
        Interactable = 1,
        Puzzle = 2,
        Dialogue = 3
    }

    private struct Candidate
    {
        public Object Source;
        public KeyCode Key;
        public InteractionPriority Priority;
        public float Distance;
    }

    public static bool IsHighestPriorityTarget(CollectibleObject collectible, Transform player)
    {
        return collectible != null &&
               TryGetHighestPriorityTarget(player, out Candidate candidate) &&
               ReferenceEquals(candidate.Source, collectible);
    }

    public static bool IsHighestPriorityTarget(InteractableObject interactable, Transform player)
    {
        return interactable != null &&
               TryGetHighestPriorityTarget(player, out Candidate candidate) &&
               ReferenceEquals(candidate.Source, interactable);
    }

    public static bool IsHighestPriorityTarget(GraveyardGateController gate, Transform player)
    {
        return gate != null &&
               TryGetHighestPriorityTarget(player, out Candidate candidate) &&
               ReferenceEquals(candidate.Source, gate);
    }

    public static bool TryGetHighestPriorityKey(Transform player, out KeyCode key)
    {
        if (TryGetHighestPriorityTarget(player, out Candidate candidate))
        {
            key = candidate.Key;
            return true;
        }

        key = KeyCode.None;
        return false;
    }

    public static bool TryConsumeInteraction()
    {
        if (Time.frameCount == lastConsumedFrame)
        {
            return false;
        }

        lastConsumedFrame = Time.frameCount;
        return true;
    }

    private static bool TryGetHighestPriorityTarget(Transform player, out Candidate bestCandidate)
    {
        bestCandidate = default;

        if (player == null)
        {
            return false;
        }

        bool found = false;

        CollectibleObject[] collectibles = Object.FindObjectsByType<CollectibleObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (CollectibleObject collectible in collectibles)
        {
            if (collectible == null || !collectible.CanBeInteractedWith(player))
            {
                continue;
            }

            Candidate candidate = new Candidate
            {
                Source = collectible,
                Key = KeyCode.E,
                Priority = InteractionPriority.Collectible,
                Distance = collectible.GetDistanceTo(player)
            };

            if (!found || IsBetterCandidate(candidate, bestCandidate))
            {
                bestCandidate = candidate;
                found = true;
            }
        }

        InteractableObject[] interactables = Object.FindObjectsByType<InteractableObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null || !interactable.CanBeInteractedWith(player))
            {
                continue;
            }

            Candidate candidate = new Candidate
            {
                Source = interactable,
                Key = interactable.interactKey,
                Priority = GetInteractablePriority(interactable),
                Distance = interactable.GetDistanceTo(player)
            };

            if (!found || IsBetterCandidate(candidate, bestCandidate))
            {
                bestCandidate = candidate;
                found = true;
            }
        }

        GraveyardGateController[] gates = Object.FindObjectsByType<GraveyardGateController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (GraveyardGateController gate in gates)
        {
            if (gate == null || !gate.CanBeInteractedWith(player))
            {
                continue;
            }

            Candidate candidate = new Candidate
            {
                Source = gate,
                Key = gate.InteractKey,
                Priority = InteractionPriority.Puzzle,
                Distance = gate.GetDistanceTo(player)
            };

            if (!found || IsBetterCandidate(candidate, bestCandidate))
            {
                bestCandidate = candidate;
                found = true;
            }
        }

        return found;
    }

    private static bool IsBetterCandidate(Candidate candidate, Candidate currentBest)
    {
        if (candidate.Priority != currentBest.Priority)
        {
            return candidate.Priority < currentBest.Priority;
        }

        if (!Mathf.Approximately(candidate.Distance, currentBest.Distance))
        {
            return candidate.Distance < currentBest.Distance;
        }

        return candidate.Source.GetInstanceID() < currentBest.Source.GetInstanceID();
    }

    private static InteractionPriority GetInteractablePriority(InteractableObject interactable)
    {
        if (interactable.IsDialoguePriorityTarget())
        {
            return InteractionPriority.Dialogue;
        }

        if (interactable.IsPuzzlePriorityTarget())
        {
            return InteractionPriority.Puzzle;
        }

        return InteractionPriority.Interactable;
    }
}
