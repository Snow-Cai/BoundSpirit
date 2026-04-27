using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bound Spirit/Scene Hint Data", fileName = "HintData_SceneName")]
public class SceneHintData : ScriptableObject
{
    [Serializable]
    public class HintEntry
    {
        [Tooltip("Short label shown only in the Inspector. Not visible to the player.")]
        public string editorLabel;

        [Tooltip("The hint text shown to the player.")]
        [TextArea(2, 5)]
        public string hintText;

        [Header("Fires while this puzzle is NOT yet solved (leave empty to ignore)")]
        public string requiredPuzzleID;

        [Header("Fires while this dialogue has NOT yet been viewed (leave empty to ignore)")]
        public string requiredDialogueID;

        [Header("Fires while this StoryFlags.Flag is NOT yet set (leave empty to ignore)")]
        public string requiredStoryFlag;
        // GATE CONDITIONS

        [Header("Skip this hint until this puzzle IS solved")]
        public string gatedBehindPuzzleID;

        [Header("Skip this hint until this dialogue IS viewed")]
        public string gatedBehindDialogueID;

        public bool ShouldShow()
        {
            if (SaveSystem.Instance == null)
                return false;

            //GATE CHECKS: skip entirely if prerequisites not met

            if (!string.IsNullOrWhiteSpace(gatedBehindPuzzleID) &&
                !SaveSystem.Instance.IsPuzzleSolved(gatedBehindPuzzleID))
                return false;

            if (!string.IsNullOrWhiteSpace(gatedBehindDialogueID) &&
                !SaveSystem.Instance.HasViewedDialogue(gatedBehindDialogueID))
                return false;

            //condition checks

            bool hasPuzzleCondition = !string.IsNullOrWhiteSpace(requiredPuzzleID);
            bool hasDialogueCondition = !string.IsNullOrWhiteSpace(requiredDialogueID);
            bool hasFlagCondition = !string.IsNullOrWhiteSpace(requiredStoryFlag);

            //No conditions at all = fallback entry, always fires once gates pass
            if (!hasPuzzleCondition && !hasDialogueCondition && !hasFlagCondition)
                return true;

            if (hasPuzzleCondition && !SaveSystem.Instance.IsPuzzleSolved(requiredPuzzleID))
                return true;

            if (hasDialogueCondition && !SaveSystem.Instance.HasViewedDialogue(requiredDialogueID))
                return true;

            if (hasFlagCondition)
            {
                if (System.Enum.TryParse(requiredStoryFlag, true, out StoryFlags.Flag flag))
                {
                    if (!StoryFlags.IsSet(flag))
                        return true;
                }
                else if (!SaveSystem.Instance.IsPuzzleSolved(requiredStoryFlag))
                {
                    return true;
                }
            }

            //All conditions for this entry are met: entry is done, skip it
            return false;
        }
    }

    [Tooltip("Evaluated top-to-bottom. The FIRST entry whose ShouldShow() returns true is used.")]
    public List<HintEntry> hints = new List<HintEntry>();

    public string GetCurrentHint()
    {
        foreach (HintEntry entry in hints)
        {
            if (entry != null && entry.ShouldShow())
            {
                Debug.Log($"[HintSystem] Firing hint: '{entry.editorLabel}' - {entry.hintText}");
                return entry.hintText;
            }
            else if (entry != null)
            {
                Debug.Log($"[HintSystem] Skipping hint: '{entry.editorLabel}' (conditions met or gate not passed)");
            }
        }
        return null;
    }
}