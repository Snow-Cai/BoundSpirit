using UnityEngine;
public class DemoEndingTrigger : MonoBehaviour
{
    [Header("Which ending to demo")]
    public EndingType endingToDemo = EndingType.Forgive;

    [Header("One-time trigger")]
    private bool triggered = false;

    public enum EndingType
    {
        Forgive,
        Revenge,
        Secret
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;

        if (SaveSystem.Instance == null) return;
        SaveData data = SaveSystem.Instance.GetSaveData();

        switch (endingToDemo)
        {
            case EndingType.Forgive:
                data.truthRevealed = true;
                data.knowsPlayerIsDead = true;
                Debug.Log("DEMO: Forgive ending flags set");
                break;

            case EndingType.Revenge:
                data.edenRevealed = true;
                data.truthRevealed = false;
                data.knowsPlayerIsDead = true;
                Debug.Log("DEMO: Revenge ending flags set");
                break;

            case EndingType.Secret:
                data.truthRevealed = true;
                data.edenRevealed = true;
                data.knowsPlayerIsDead = true;
                SaveSystem.Instance.UnlockPuzzle("secret_ending_unlocked");
                Debug.Log("DEMO: Secret ending flags set");
                break;
        }
    }
}