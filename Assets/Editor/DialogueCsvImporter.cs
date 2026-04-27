using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public static class DialogueCsvImporter
{
    private const string CsvPath = "Assets/Dialogue/Import/AllDialogue.csv";
    private const string OutputRootFolder = "Assets/Dialogue/Imported";

    private class DialogueRow
    {
        public string SourceAsset;
        public string Category;
        public string DialogueID;
        public int LineIndex;
        public string Speaker;
        public string Text;
        public string ChoiceText;
        public string NextDialogueID;
        public string Event;
        public string Notes;
    }

    [MenuItem("Tools/Dialogue/Import CSV To DialogueAssets")]
    public static void ImportDialogueCsv()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError($"Dialogue CSV not found at: {CsvPath}");
            return;
        }

        EnsureFolderExists("Assets/Dialogue");
        EnsureFolderExists("Assets/Dialogue/Import");
        EnsureFolderExists(OutputRootFolder);

        List<DialogueRow> rows = LoadRows(CsvPath);

        if (rows.Count == 0)
        {
            Debug.LogWarning("No dialogue rows found in CSV.");
            return;
        }

        Dictionary<string, List<DialogueRow>> groupedRows = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.DialogueID))
            .GroupBy(r => r.DialogueID.Trim())
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.LineIndex).ThenBy(r => r.ChoiceText).ToList()
            );

        Dictionary<string, DialogueAsset> createdAssets = new Dictionary<string, DialogueAsset>();

        // First pass: create assets and assign dialogue lines
        foreach (KeyValuePair<string, List<DialogueRow>> pair in groupedRows)
        {
            string dialogueID = pair.Key;
            List<DialogueRow> dialogueRows = pair.Value;

            string folderName = GetFolderNameFromDialogue(dialogueRows);
            string fullFolderPath = $"{OutputRootFolder}/{folderName}";

            EnsureFolderExists(fullFolderPath);

            string assetPath = $"{fullFolderPath}/{SanitizeFileName(dialogueID)}.asset";
            DialogueAsset asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.dialogueID = dialogueID;
            asset.lines = new List<DialogueLine>();
            asset.choices = new List<DialogueChoice>();
            asset.choicesAfterLineIndex = -1;
            asset.saveAfterDialogue = true;
            asset.showChoicesAtEnd = true;

            List<DialogueRow> lineRows = dialogueRows
                .Where(r => string.IsNullOrWhiteSpace(r.ChoiceText))
                .OrderBy(r => r.LineIndex)
                .ToList();

            foreach (DialogueRow row in lineRows)
            {
                DialogueLine line = new DialogueLine
                {
                    speakerName = row.Speaker ?? string.Empty,
                    dialogueText = row.Text ?? string.Empty,
                    voiceClip = null
                };

                asset.lines.Add(line);
            }

            EditorUtility.SetDirty(asset);
            createdAssets[dialogueID] = asset;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Second pass: assign choices after all assets exist
        foreach (KeyValuePair<string, List<DialogueRow>> pair in groupedRows)
        {
            string dialogueID = pair.Key;
            List<DialogueRow> dialogueRows = pair.Value;

            if (!createdAssets.TryGetValue(dialogueID, out DialogueAsset asset) || asset == null)
            {
                continue;
            }

            List<DialogueRow> choiceRows = dialogueRows
                .Where(r => !string.IsNullOrWhiteSpace(r.ChoiceText))
                .OrderBy(r => r.LineIndex)
                .ThenBy(r => r.ChoiceText)
                .ToList();

            if (choiceRows.Count > 0)
            {
                asset.choices = new List<DialogueChoice>();

                foreach (DialogueRow row in choiceRows)
                {
                    DialogueChoice choice = new DialogueChoice
                    {
                        choiceText = row.ChoiceText ?? string.Empty,
                        nextDialogue = null,
                        requiredFlags = new List<string>(),
                        forbiddenFlags = new List<string>(),
                        onChoiceSelectedID = row.Event
                    };

                    if (!string.IsNullOrWhiteSpace(row.NextDialogueID) &&
                        createdAssets.TryGetValue(row.NextDialogueID.Trim(), out DialogueAsset nextAsset))
                    {
                        choice.nextDialogue = nextAsset;
                    }

                    asset.choices.Add(choice);
                }

                // For now, choices appear at the end of the dialogue.
                asset.choicesAfterLineIndex = -1;
                asset.showChoicesAtEnd = true;
            }

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Imported {groupedRows.Count} dialogue assets from CSV into categorized folders.");
    }

    private static List<DialogueRow> LoadRows(string csvPath)
    {
        List<DialogueRow> rows = new List<DialogueRow>();
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);

        if (lines.Length <= 1)
        {
            return rows;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            List<string> cols = ParseCsvLine(lines[i]);

            // Expected columns:
            // SourceAsset, Category, DialogueID, LineIndex, Speaker, Text,
            // ChoiceText, NextDialogueID, Event, Notes
            while (cols.Count < 10)
            {
                cols.Add(string.Empty);
            }

            int lineIndex = 0;
            int.TryParse(cols[3], out lineIndex);

            DialogueRow row = new DialogueRow
            {
                SourceAsset = cols[0]?.Trim(),
                Category = cols[1]?.Trim(),
                DialogueID = cols[2]?.Trim(),
                LineIndex = lineIndex,
                Speaker = cols[4],
                Text = cols[5],
                ChoiceText = cols[6],
                NextDialogueID = cols[7],
                Event = cols[8],
                Notes = cols[9]
            };

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        StringBuilder current = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private static string GetFolderNameFromDialogue(List<DialogueRow> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return "Uncategorized";
        }

        string dialogueID = rows[0].DialogueID ?? string.Empty;
        string sourceAsset = rows[0].SourceAsset ?? string.Empty;

        string combined = $"{dialogueID} {sourceAsset}";
        string lower = combined.ToLowerInvariant();

        if (lower.Contains("ghost"))
        {
            return "Ghosts";
        }

        Match chapterMatch = Regex.Match(combined, @"Chapter\d+", RegexOptions.IgnoreCase);
        if (chapterMatch.Success)
        {
            return chapterMatch.Value;
        }

        if (lower.Contains("gate") ||
            lower.Contains("door") ||
            lower.Contains("passcode") ||
            lower.Contains("pattern"))
        {
            return "GatePuzzle";
        }

        if (lower.Contains("tombstone") || lower.Contains("grave"))
        {
            return "Graves";
        }

        if (lower.Contains("npc") ||
            lower.Contains("awakening") ||
            lower.Contains("story"))
        {
            return "Story";
        }

        // fallback to spreadsheet category if useful
        string category = rows[0].Category ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(category))
        {
            return SanitizeFolderName(category.Replace(" ", string.Empty).Replace("/", string.Empty));
        }

        return "Uncategorized";
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string normalizedPath = path.Replace("\\", "/");
        string[] parts = normalizedPath.Split('/');

        if (parts.Length == 0)
        {
            return;
        }

        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "UnnamedDialogue";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        return input.Trim();
    }

    private static string SanitizeFolderName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Uncategorized";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        return input.Trim();
    }
}