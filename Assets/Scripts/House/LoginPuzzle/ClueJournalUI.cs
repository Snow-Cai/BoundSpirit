using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClueJournal : MonoBehaviour
{
    public TextMeshProUGUI journalText;

    private List<Clue> clues = new List<Clue>();

    public void AddClue(Clue clue)
    {
        if (!clues.Contains(clue))
        {
            clues.Add(clue);
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        journalText.text = "";
        foreach (Clue clue in clues)
        {
            journalText.text += "• " + clue.clueText + "\n\n";
        }
    }
}
