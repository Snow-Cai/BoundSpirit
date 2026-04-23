using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTravelTrigger : MonoBehaviour
{
    public enum UnlockType
    {
        Always,
        ChapterUnlocked,
        PuzzleSolved,
        HasItem,
        KnowsNameIsAkila
    }

    [Serializable]
    public class Destination
    {
        [Header("Display")]
        public string displayName;
        public Button button;
        public bool hideWhenLocked = false;

        [Header("Travel")]
        public string sceneName;
        public string spawnPointId;

        [Header("Unlock")]
        public UnlockType unlockType = UnlockType.Always;
        public int requiredChapter;
        public string requiredPuzzleId;
        public string requiredItemId;
    }

    [Header("Destinations")]
    [SerializeField] private List<Destination> destinations = new List<Destination>();

    [Header("Player")]
    [SerializeField] private CharMovement charMovement;

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeScreen;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning;

    public IReadOnlyList<Destination> Destinations => destinations;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (MapPopupUI.Instance == null) return;
        if (isTransitioning) return;

        if (charMovement != null)
            charMovement.enabled = false;

        MapPopupUI.Instance.Open(this);
    }

    public void RestorePlayerControl()
    {
        if (charMovement != null)
            charMovement.enabled = true;
    }

    public bool IsUnlocked(Destination destination)
    {
        if (IsCurrentScene(destination))
            return false;

        switch (destination.unlockType)
        {
            case UnlockType.Always:
                return true;

            case UnlockType.ChapterUnlocked:
                return SaveSystem.Instance != null &&
                       SaveSystem.Instance.IsChapterUnlocked(destination.requiredChapter);

            case UnlockType.PuzzleSolved:
                return SaveSystem.Instance != null &&
                       !string.IsNullOrWhiteSpace(destination.requiredPuzzleId) &&
                       SaveSystem.Instance.IsPuzzleSolved(destination.requiredPuzzleId);

            case UnlockType.HasItem:
                return SaveSystem.Instance != null &&
                       !string.IsNullOrWhiteSpace(destination.requiredItemId) &&
                       SaveSystem.Instance.HasItem(destination.requiredItemId);

            case UnlockType.KnowsNameIsAkila:
                return SaveSystem.Instance != null &&
                       SaveSystem.Instance.KnowsNameIsAkila();
        }

        return false;
    }

    private bool IsCurrentScene(Destination destination)
    {
        return destination != null &&
               string.Equals(destination.sceneName, SceneManager.GetActiveScene().name, StringComparison.Ordinal);
    }

    public void TravelTo(Destination destination)
    {
        if (isTransitioning) return;
        if (IsCurrentScene(destination)) return;

        StartCoroutine(HandleTransition(destination));
    }

    private IEnumerator HandleTransition(Destination destination)
    {
        isTransitioning = true;

        TravelState.NextSpawnPointId = destination.spawnPointId;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            SaveSystem.Instance.SetTransitioning(true);
        }

        if (fadeScreen != null)
        {
            float elapsed = 0f;
            fadeScreen.alpha = 0f;
            fadeScreen.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeScreen.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            fadeScreen.alpha = 1f;
        }

        SceneManager.LoadScene(destination.sceneName);
    }

    public void ConfigureButtonVisual(Destination destination)
    {
        if (destination.button == null) return;

        TMP_Text label = destination.button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !string.IsNullOrWhiteSpace(destination.displayName))
            label.text = destination.displayName;

        bool unlocked = IsUnlocked(destination);

        if (destination.hideWhenLocked && !unlocked)
        {
            destination.button.gameObject.SetActive(false);
            return;
        }

        destination.button.gameObject.SetActive(true);
        destination.button.interactable = unlocked;
    }
}

public static class TravelState
{
    public static string NextSpawnPointId;
}
