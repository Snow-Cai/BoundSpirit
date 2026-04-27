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
        if (isTransitioning) return;

        MapPopupUI mapPopup = MapPopupUI.GetOrCreateInstance();
        if (mapPopup == null) return;

        if (charMovement != null)
            charMovement.enabled = false;

        mapPopup.Open(this);
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
            //unlock the destination chapter so it becomes available in chapter select
            int destChapter = GetChapterNumberForScene(destination.sceneName);
            if (destChapter >= 0)
                SaveSystem.Instance.UnlockChapter(destChapter);

            SaveSystem.Instance.SaveGame();
            SaveSystem.Instance.SetTransitioning(true);
        }

        if (destination.sceneName == "ChapterFinal")
            MusicManager.Instance.StopMusic(true);

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

    //returns the chapter number for a given scene name, or -1 if not a chapter scene
    private static int GetChapterNumberForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;
        if (sceneName.StartsWith("Chapter0", System.StringComparison.OrdinalIgnoreCase)) return 0;
        if (sceneName.StartsWith("Chapter1", System.StringComparison.OrdinalIgnoreCase)) return 1;
        if (sceneName.StartsWith("Chapter2", System.StringComparison.OrdinalIgnoreCase)) return 2;
        if (sceneName.StartsWith("Chapter3", System.StringComparison.OrdinalIgnoreCase)) return 3;
        if (sceneName.StartsWith("Chapter4", System.StringComparison.OrdinalIgnoreCase)) return 4; //??? no need?
        if (sceneName.Equals("ChapterFinal", System.StringComparison.OrdinalIgnoreCase)) return 4;
        return -1;
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