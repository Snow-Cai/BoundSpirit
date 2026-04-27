using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

//Displays "X / Y puzzles solved" for the current scene by reading
public class PuzzleProgressUI : MonoBehaviour
{
    [Header("Registry")]
    [Tooltip("ScenePuzzleRegistry asset listing puzzle IDs per scene name.")]
    [SerializeField] private ScenePuzzleRegistry registry;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI progressLabel;

    [Header("Format")]
    [Tooltip("{0} = solved count, {1} = total. e.g. \"{0} / {1} puzzles solved\"")]
    [SerializeField] private string labelFormat = "{0} / {1} puzzles solved";

    [Header("Polling")]
    [Tooltip("Seconds between save-system checks. 0.5 minimum is enforced.")]
    [SerializeField] private float pollInterval = 1f;

    private List<string> cachedIDs = new List<string>();
    private float timer;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshSceneData();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneData();
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = Mathf.Max(0.5f, pollInterval);
        UpdateLabel();
    }


    public void RefreshSceneData()
    {
        cachedIDs.Clear();

        if (registry != null)
            cachedIDs.AddRange(registry.GetPuzzleIDsForScene(SceneManager.GetActiveScene().name));

        timer = 0f; //force immediate label refresh
    }

    private void UpdateLabel()
    {
        if (progressLabel == null) return;
        if (cachedIDs.Count == 0 || SaveSystem.Instance == null)
        {
            progressLabel.text = string.Empty;
            return;
        }

        int total = cachedIDs.Count;
        int solved = 0;

        for (int i = 0; i < cachedIDs.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(cachedIDs[i]) &&
                SaveSystem.Instance.IsPuzzleSolved(cachedIDs[i]))
                solved++;
        }

        progressLabel.text = string.Format(labelFormat, solved, total);
    }
}