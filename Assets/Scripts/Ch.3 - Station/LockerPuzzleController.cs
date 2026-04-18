using PlasticPipe.PlasticProtocol.Messages;
using TMPro;
using UnityEngine;

public class LockerPuzzleController : MonoBehaviour
{
    [Header("UI")]
    public GameObject lockerUI;
    public TextMeshProUGUI[] digitTexts;

    [Header("Code")]
    public int[] correctCode = new int[] { 2, 9, 5, 3, 7, 4 };

    private int[] currentCode = new int[6];

    public void Open()
    {
        lockerUI.SetActive(true);

        for (int i = 0; i < currentCode.Length; i++)
            currentCode[i] = 0;
        RefreshUI();
    }

    public void Close()
    {
        lockerUI.SetActive(false);
    }

    public void CycleDigit(int index)
    {
        currentCode[index] = (currentCode[index] + 1) % 10;
        RefreshUI();
    }

    public void Submit()
    {
        for(int i = 0; i < 6; i++)
        {
            if (currentCode[i] != correctCode[i])
            {
                Debug.Log("Wrong Code!");
                return;
            }
        }
        Unlock();
    }

    void Unlock()
    {
        Debug.Log("LOCKER UNLOCKED: Police file obtained!");
        Close();
    }

    void RefreshUI()
    {
        for(int i = 0; i <digitTexts.Length; i++)
        {
            digitTexts[i].text = currentCode[i].ToString();
        }
    }
}
