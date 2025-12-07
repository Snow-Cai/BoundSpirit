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

    [Tooltip("Smooth fade speed for the hint.")]
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

        // Hide hint whenever dialogue/puzzle is active.
        if (hideWhenDialogueActive && GameInputState.DialogueActive)
        {
            SetTargetVisible(false);
            ApplyFade();
            return;
        }

        // Find closest valid interaction target (InteractableObject or GraveyardGateController).
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

        Vector2 playerPosition = player.position;
        float nearestDistance = maxDistance;
        bool found = false;

        // 1) Check InteractableObjects
        InteractableObject[] interactables = FindObjectsOfType<InteractableObject>();
        foreach (var interactable in interactables)
        {
            if (interactable == null || !interactable.enabled)
            {
                continue;
            }

            float distance = Vector2.Distance(playerPosition, interactable.transform.position);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                targetPosition = interactable.transform.position;
                targetKey = interactable.interactKey;   // assumes this is public or has a getter
                found = true;
            }
        }

        // 2) Check GraveyardGateController (special-case gate interaction)
        GraveyardGateController[] gates = FindObjectsOfType<GraveyardGateController>();
        foreach (var gate in gates)
        {
            if (gate == null || !gate.enabled)
            {
                continue;
            }

            float distance = Vector2.Distance(playerPosition, gate.transform.position);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                targetPosition = gate.transform.position;
                targetKey = gate.InteractKey;  // uses the property we just added
                found = true;
            }
        }

        return found;
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
            fadeSpeed * Time.deltaTime
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
