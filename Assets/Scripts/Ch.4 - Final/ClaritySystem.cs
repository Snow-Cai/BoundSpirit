public static class ClaritySystem
{
    public const int ThresholdForForgive = 4;   // minimum score to see the Forgive choice
    public const int MaxScore = 8;              // 5 choice moments, max +2 each = 8 possible

    public static void AddClarity(int amount)
    {
        if (SaveSystem.Instance == null) return;

        SaveData data = SaveSystem.Instance.GetSaveData();
        if (data == null) return;

        int before = data.clarityScore;
        data.clarityScore = UnityEngine.Mathf.Clamp(data.clarityScore + amount, 0, MaxScore);

        if (data.clarityScore != before)
            SaveSystem.Instance.SaveGame();

        UnityEngine.Debug.Log($"CLARITY: +{amount} : score is now {data.clarityScore}/{MaxScore}");
    }

    public static int GetScore()
    {
        if (SaveSystem.Instance == null) return 0;
        SaveData data = SaveSystem.Instance.GetSaveData();
        return data != null ? data.clarityScore : 0;
    }

    public static bool CanSeeForgiveEnding() => GetScore() >= ThresholdForForgive;
}