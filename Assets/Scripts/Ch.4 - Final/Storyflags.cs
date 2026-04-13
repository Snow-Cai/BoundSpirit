using UnityEngine;
//centralized helper for reading and writing story flags
public static class StoryFlags
{
    //flag definitions: add new flags here as the story continues
    public enum Flag
    {
        //Chapter 0
        KnowsPlayerIsDead,          // Read tombstone, knows Akila is dead
        KnowsNameIsAkila,           // Read own name from tombstone
        GraveyardGateUnlocked,      // Completed the shape puzzle on the gate

        // Chapter 1
        FoundKey,                   // Picked up the key from the first floor
        SafeOpened,                 // Opened the safe upstairs
        FoundWeapon,                // Saw the weapon in the safe
        WeaponIsPoliceGun,          // Understood weapon belonged to dad (police)
        FoundEdenPhoto,             // Found photo of Eden and Akila at library
        ReassembledNote,            // Completed the paper reassembly puzzle
        ComputerUnlocked,           // Solved the login puzzle on the computer
        KnowsEdenFromComputer,      // Found Eden's info on the computer

        //Chapter 2
        WentToLibrary,              // Visited the library scene
        LibraryClueFound,           // Found the key clue at the library
        PoliceStationVisited,       // Went to the police station

        //Chapter 3
        PoliceFilesContradiction,   // Noticed police files don't match Eden's story

        //Chapter 4/Endings
        EdenRevealed,               // Knows Eden killed Akila
        TruthRevealed,              // Knows Akila was stalking/threatening Eden
        SecretEndingUnlocked,       // Found all secret clues for secret ending
    }

    // Set a flag (marks it as true in SaveSystem)
    public static void Set(Flag flag)
    {
        if (SaveSystem.Instance == null) return;

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null) return;

        ApplyFlag(data, flag, true);
        SaveSystem.Instance.SaveGame();

        Debug.Log("STORYFLAG SET: " + flag);
    }

    //Read a flag
    public static bool IsSet(Flag flag)
    {
        if (SaveSystem.Instance == null) return false;

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null) return false;

        return ReadFlag(data, flag);
    }

    // Unlock the secret ending (requires all three core flags + special puzzle)
    public static void UnlockSecretEnding()
    {
        Set(Flag.SecretEndingUnlocked);
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.UnlockPuzzle("secret_ending_unlocked");
        Debug.Log("STORYFLAG: Secret ending unlocked!");
    }

    // Convenience: check if the player has enough info to trigger an ending
    public static bool HasEnoughForEnding()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogError("SaveSystem.Instance is NULL in HasEnoughForEnding!");
            return false;
        }
        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null)
        {
            Debug.LogError("SaveData is NULL!");
            return false;
        }
        Debug.Log("truthRevealed: " + data.truthRevealed + " | knowsPlayerIsDead: " + data.knowsPlayerIsDead);
        return data.truthRevealed && data.knowsPlayerIsDead;
    }

    //Called from DialogueSystem.HandleDialogueCompletionEffects
    //Add new dialogue ID cases here when I create dialogue assets
    public static void HandleDialogueID(string dialogueID)
    {
        switch (dialogueID)
        {
            //Chapter 0
            case "Chapter0_tombstonePrimary":
                Set(Flag.KnowsNameIsAkila);
                Set(Flag.KnowsPlayerIsDead);
                break;

            //Chapter 1 - Mom conversations
            case "Chapter1_momConvo_weapon":
                Set(Flag.WeaponIsPoliceGun);
                break;

            case "Chapter1_computerSuccess":
                Set(Flag.ComputerUnlocked);
                Set(Flag.KnowsEdenFromComputer);
                break;

            case "Chapter1_safeWeapon":
                Set(Flag.FoundWeapon);
                break;

            case "Chapter1_edenPhoto":
                Set(Flag.FoundEdenPhoto);
                break;

            case "Chapter1_safeKeyPickup":
                Set(Flag.FoundKey);
                break;

            //Chapter 2
            case "Chapter2_libraryClue":
                Set(Flag.LibraryClueFound);
                break;

            case "Chapter2_policeFiles":
                Set(Flag.PoliceStationVisited);
                break;

            //Chapter 3
            case "Chapter3_filesContradiction":
                Set(Flag.PoliceFilesContradiction);
                Set(Flag.EdenRevealed);
                break;

            //Chapter 4 the big reveal
            case "Chapter4_fullTruth":
                Set(Flag.TruthRevealed);
                break;

            case "Chapter4_secretClue":
                UnlockSecretEnding();
                break;
        }
    }

    private static void ApplyFlag(SaveData data, Flag flag, bool value)
    {
        switch (flag)
        {
            case Flag.KnowsPlayerIsDead: data.knowsPlayerIsDead = value; break;
            case Flag.KnowsNameIsAkila: data.knowsNameIsAkila = value; break;
            case Flag.EdenRevealed: data.edenRevealed = value; break;
            case Flag.TruthRevealed: data.truthRevealed = value; break;
            case Flag.GraveyardGateUnlocked:
                if (value) SaveSystem.Instance.UnlockPuzzle("Chapter0_graveyard_gate");
                break;
            case Flag.SecretEndingUnlocked:
                if (value) SaveSystem.Instance.UnlockPuzzle("secret_ending_unlocked");
                break;
            //flags without a dedicated SaveData bool use the solvedPuzzles list
            default:
                if (value) SaveSystem.Instance.UnlockPuzzle(flag.ToString());
                break;
        }
    }

    private static bool ReadFlag(SaveData data, Flag flag)
    {
        switch (flag)
        {
            case Flag.KnowsPlayerIsDead: return data.knowsPlayerIsDead;
            case Flag.KnowsNameIsAkila: return data.knowsNameIsAkila;
            case Flag.EdenRevealed: return data.edenRevealed;
            case Flag.TruthRevealed: return data.truthRevealed;
            case Flag.GraveyardGateUnlocked:
                return SaveSystem.Instance.IsPuzzleSolved("Chapter0_graveyard_gate");
            case Flag.SecretEndingUnlocked:
                return SaveSystem.Instance.IsPuzzleSolved("secret_ending_unlocked");
            default:
                return SaveSystem.Instance.IsPuzzleSolved(flag.ToString());
        }
    }
}