using System;

/// <summary>
/// Hides the player character's name in dialogue UI until the story marks identity as learned
/// (<see cref="SaveSystem.KnowsNameIsAkila"/>), so authored lines can use "Akila" without spoiling early reads.
/// </summary>
public static class PlayerIdentityPresentation
{
    private const string TombstoneRevealDialogueId = "Chapter0_tombstonePrimary";
    private const string PlayerSpeakerName = "Akila";
    private const string UnknownSpeakerLabel = "???";

    public static string GetDisplayedSpeakerName(string authoredSpeakerName, string activeDialogueId)
    {
        if (string.IsNullOrWhiteSpace(authoredSpeakerName))
        {
            return authoredSpeakerName ?? string.Empty;
        }

        if (IdentityKnown())
        {
            return authoredSpeakerName;
        }

        if (string.Equals(activeDialogueId, TombstoneRevealDialogueId, StringComparison.Ordinal))
        {
            return authoredSpeakerName;
        }

        if (string.Equals(authoredSpeakerName.Trim(), PlayerSpeakerName, StringComparison.OrdinalIgnoreCase))
        {
            return UnknownSpeakerLabel;
        }

        return authoredSpeakerName;
    }

    private static bool IdentityKnown()
    {
        return SaveSystem.Instance != null && SaveSystem.Instance.KnowsNameIsAkila();
    }
}
