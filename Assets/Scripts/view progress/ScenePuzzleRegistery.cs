using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bound Spirit/Scene Puzzle Registry", fileName = "ScenePuzzleRegistry")]
public class ScenePuzzleRegistry : ScriptableObject
{
    [Serializable]
    public class SceneEntry
    {
        [Tooltip("Must match SceneManager.GetActiveScene().name exactly.")]
        public string sceneName;

        [Tooltip("All puzzle IDs that count toward the tracker in this scene.")]
        public List<string> puzzleIDs = new List<string>();
    }

    public List<SceneEntry> scenes = new List<SceneEntry>();

    public List<string> GetPuzzleIDsForScene(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(scenes[i].sceneName, sceneName, StringComparison.Ordinal))
                return scenes[i].puzzleIDs ?? new List<string>();
        }
        return new List<string>();
    }
}