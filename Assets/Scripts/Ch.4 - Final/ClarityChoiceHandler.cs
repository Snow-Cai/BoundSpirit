using UnityEngine;

public class ClarityChoiceHandler : MonoBehaviour
{
    // ?? Chapter 0

    public void OnTombstone_SelfAware()
    {
        ClaritySystem.AddClarity(1);
    }

    public void OnTombstone_Deflect()
    {
        //No clarity gained. still a valid choice, just doesn't add score
        ClaritySystem.AddClarity(0);
    }

    // ?? Chapter 1 — weapon

    public void OnWeapon_SelfAware()
    {
        ClaritySystem.AddClarity(1);
    }

    public void OnWeapon_Deflect()
    {
        ClaritySystem.AddClarity(0);
    }

    // ?? Chapter 1 — polaroid

    public void OnPolaroid_Empathy()
    {
        ClaritySystem.AddClarity(1);
    }

    public void OnPolaroid_Deflect()
    {
        ClaritySystem.AddClarity(0);
    }

    // ?? Chapter 2 — cipher

    public void OnCipher_OpenMinded()
    {
        ClaritySystem.AddClarity(1);
    }

    public void OnCipher_Deflect()
    {
        ClaritySystem.AddClarity(0);
    }

    // ?? Chapter 3 — police files

    public void OnFiles_FullAcceptance()
    {
        ClaritySystem.AddClarity(2);
    }

    public void OnFiles_PartialAcceptance()
    {
        ClaritySystem.AddClarity(1);
    }

    public void OnFiles_Denial()
    {
        ClaritySystem.AddClarity(0);
    }

    // ?? Chapter 4 — final choice

    public void OnForgiveChosen()
    {
        StoryFlags.Set(StoryFlags.Flag.TruthRevealed);
        Debug.Log("Chapter4: Forgive ending chosen.");
    }

    public void OnRevengeChosen()
    {
        // EdenRevealed should already be true from chpt3, but set it explicitly as a safety net
        StoryFlags.Set(StoryFlags.Flag.EdenRevealed);

        // Ensure TruthRevealed stays false so ResolveSelectedEnding returns Revenge
        if (SaveSystem.Instance != null)
        {
            SaveData data = SaveSystem.Instance.GetSaveData();
            if (data != null)
            {
                data.truthRevealed = false;
                SaveSystem.Instance.SaveGame();
            }
        }

        Debug.Log("Chapter4: Revenge ending chosen.");
    }

    //Wired to Choice 2 of Chapter4_finalChoice
    //Only visible when foundHiddenTombstone + foundMenuSecret are both true
    //(gated by requiredFlags in the DialogueAsset)
    public void OnSecretChosen()
    {
        StoryFlags.UnlockSecretEnding();
        Debug.Log("Chapter4: Secret ending chosen.");
    }
}