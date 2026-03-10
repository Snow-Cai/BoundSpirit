using System.Text;

/// <summary>
/// Caesar-cipher helpers for shifting text and displaying wheel mappings.
/// Shifts letters A-Z / a-z; leaves punctuation, numbers, and whitespace unchanged.
/// </summary>
public static class CaesarCipher
{
    /// <summary>
    /// Shifts alphabetical characters by <paramref name="shift"/> (can be negative). Preserves case.
    /// Non-letters are unchanged.
    /// </summary>
    public static string Shift(string input, int shift)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        shift %= 26;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c is >= 'A' and <= 'Z')
                sb.Append(ShiftChar(c, 'A', shift));
            else if (c is >= 'a' and <= 'z')
                sb.Append(ShiftChar(c, 'a', shift));
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes text for comparison: uppercases, removes punctuation, and collapses whitespace.
    /// This makes typed answers forgiving (e.g., optional periods/commas).
    /// </summary>
    public static string NormalizeForCompare(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        s = s.Trim().ToUpperInvariant();

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        }

        s = sb.ToString();

        while (s.Contains("  "))
            s = s.Replace("  ", " ");

        return s;
    }

    /// <summary>
    /// Builds a mapping string like "A→H  B→I ...". Use a negative shift for decode mapping.
    /// </summary>
    public static string BuildMappingLine(int shift)
    {
        shift %= 26;

        var sb = new StringBuilder(200);
        for (char c = 'A'; c <= 'Z'; c++)
        {
            char mapped = (char)('A' + ((c - 'A' + shift + 26) % 26));
            sb.Append(c).Append('→').Append(mapped);
            if (c != 'Z') sb.Append("   ");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Cleaner 2-line alphabet strip for UI:
    /// "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nTUVWXYZABCDEFGHIJKLMNOPQRS"
    /// Use a negative shift for decode mapping.
    /// </summary>
    public static string BuildAlphabetStrip(int shift)
    {
        shift %= 26;
        const string abc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var shifted = new char[26];
        for (int i = 0; i < 26; i++)
            shifted[i] = (char)('A' + ((i + shift + 26) % 26));

        return $"{abc}\n \n{new string(shifted)}";
    }

    private static char ShiftChar(char c, char baseChar, int shift)
    {
        int offset = c - baseChar;
        int shifted = (offset + shift + 26) % 26;
        return (char)(baseChar + shifted);
    }
}
