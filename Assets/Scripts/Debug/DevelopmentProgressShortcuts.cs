using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

/// <summary>
/// Editor / development-build only: jump save progress to "everything through chapter N" completed.
/// Uses SaveData fields, StoryFlags puzzle IDs (enum names), and chapter unlocks.
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class DevelopmentProgressShortcuts : MonoBehaviour
{
    private const string LogPrefix = "[DEV Progress] ";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DevelopmentProgressShortcuts>() != null)
        {
            return;
        }

        var go = new GameObject(nameof(DevelopmentProgressShortcuts));
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<DevelopmentProgressShortcuts>();
    }

    private void Update()
    {
        if (!DevModifiersHeld())
        {
            return;
        }

        // Ctrl+Shift+9 — reload active scene (full object state from save on load).
        if (TryGetRestartSceneShortcutPressed())
        {
            ReloadCurrentScene();
            return;
        }

        // Ctrl+Shift+0 .. 4 — complete through that chapter (cumulative from prologue).
        // Uses the new Input System when available (project uses com.unity.inputsystem); legacy Input alone often misses keys in "Both" mode.
        if (TryGetDigitPressed(out int digit) && digit >= 0 && digit <= 4)
        {
            if (SaveSystem.Instance == null)
            {
                Debug.LogWarning(LogPrefix + "SaveSystem not ready yet — open a scene with SaveSystem or wait a frame.");
                return;
            }

            ApplyThroughChapter(digit);
        }
    }

    private static bool TryGetRestartSceneShortcutPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame))
        {
            return true;
        }

        return Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);
    }

    private static void ReloadCurrentScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
        {
            Debug.LogWarning(LogPrefix + "No valid active scene to reload.");
            return;
        }

        Debug.Log(LogPrefix + "Reloading scene: " + active.name);
        SceneManager.LoadScene(active.buildIndex);
    }

    private static bool DevModifiersHeld()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            return ctrl && shift;
        }

        bool legacyCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool legacyShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        return legacyCtrl && legacyShift;
    }

    /// <summary>Returns true if 0–4 was pressed this frame (main row or numpad).</summary>
    private static bool TryGetDigitPressed(out int digit)
    {
        digit = -1;
        Keyboard kb = Keyboard.current;

        if (kb != null)
        {
            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
            {
                digit = 0;
                return true;
            }

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                digit = 1;
                return true;
            }

            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                digit = 2;
                return true;
            }

            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                digit = 3;
                return true;
            }

            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
            {
                digit = 4;
                return true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            digit = 0;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            digit = 1;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            digit = 2;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            digit = 3;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            digit = 4;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Completes all story content through the given chapter index (0 = prologue, 1 = home, …).
    /// Unlocks the next chapter in the menu and advances currentChapter accordingly.
    /// </summary>
    public static void ApplyThroughChapter(int throughChapter)
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning(LogPrefix + "SaveSystem missing.");
            return;
        }

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null)
        {
            Debug.LogWarning(LogPrefix + "SaveData missing.");
            return;
        }

        throughChapter = Mathf.Clamp(throughChapter, 0, 4);

        if (throughChapter >= 0)
        {
            ApplyChapter0(data);
        }

        if (throughChapter >= 1)
        {
            ApplyChapter1(data);
        }

        if (throughChapter >= 2)
        {
            ApplyChapter2(data);
        }

        if (throughChapter >= 3)
        {
            ApplyChapter3(data);
        }

        if (throughChapter >= 4)
        {
            ApplyChapter4(data);
        }

        int unlockThrough = throughChapter + 1;
        data.highestChapterUnlocked = Mathf.Max(data.highestChapterUnlocked, unlockThrough);
        data.currentChapter = Mathf.Max(data.currentChapter, unlockThrough);

        SaveSystem.Instance.SaveGame();
        SaveSystem.Instance.ApplySaveToLoadedScene();
        Debug.Log(LogPrefix + "Applied dev progress through chapter " + throughChapter +
                  " (highestChapterUnlocked=" + data.highestChapterUnlocked +
                  ", currentChapter=" + data.currentChapter + ").");
    }

    private static void ApplyChapter0(SaveData data)
    {
        data.knowsPlayerIsDead = true;
        data.knowsNameIsAkila = true;

        AddPuzzle(data, "Chapter0_graveyard_gate");
        AddPuzzle(data, "graveyard_ghost_rose");
        AddPuzzle(data, "graveyard_ghost_crumpledPaper");
        AddPuzzle(data, "graveyard_ghost_key");

        MarkDialogues(data,
            "Chapter0_tombstonePrimary",
            "Chapter0_gateCluePrimary",
            "Chapter0_awakening");
    }

    private static void ApplyChapter1(SaveData data)
    {
        // StoryFlags defaults (stored as solved puzzle IDs by name)
        AddPuzzle(data, "FoundKey");
        AddPuzzle(data, "SafeOpened");
        AddPuzzle(data, "FoundWeapon");
        AddPuzzle(data, "WeaponIsPoliceGun");
        AddPuzzle(data, "FoundEdenPhoto");
        AddPuzzle(data, "ReassembledNote");
        AddPuzzle(data, "ComputerUnlocked");
        AddPuzzle(data, "KnowsEdenFromComputer");

        AddPuzzle(data, "Chapter1_polaroid_timeline");
        AddPuzzle(data, "ReassemblyPuzzle");

        MarkDialogues(data,
            "Chapter1_momConvo_weapon",
            "Chapter1_computerSuccess",
            "Chapter1_safeWeapon",
            "Chapter1_edenPhoto",
            "Chapter1_safeKeyPickup");
    }

    private static void ApplyChapter2(SaveData data)
    {
        AddPuzzle(data, "WentToLibrary");
        AddPuzzle(data, "LibraryClueFound");
        AddPuzzle(data, "PoliceStationVisited");
        AddPuzzle(data, "Caesar_Note_Library");
        AddPuzzle(data, "CaesarCipher Puzzle");

        MarkDialogues(data,
            "Chapter2_libraryClue",
            "Chapter2_policeFiles");
    }

    private static void ApplyChapter3(SaveData data)
    {
        data.edenRevealed = true;
        AddPuzzle(data, "PoliceFilesContradiction");

        MarkDialogues(data, "Chapter3_filesContradiction");
    }

    private static void ApplyChapter4(SaveData data)
    {
        data.truthRevealed = true;

        MarkDialogues(data, "Chapter4_fullTruth");
    }

    private static void AddPuzzle(SaveData data, string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId) || data.solvedPuzzles.Contains(puzzleId))
        {
            return;
        }

        data.solvedPuzzles.Add(puzzleId);
    }

    private static void MarkDialogues(SaveData data, params string[] dialogueIds)
    {
        if (dialogueIds == null)
        {
            return;
        }

        foreach (string id in dialogueIds)
        {
            if (string.IsNullOrEmpty(id) || data.viewedDialogues.Contains(id))
            {
                continue;
            }

            data.viewedDialogues.Add(id);
        }
    }
}

#endif
