using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    public GameObject journalPanel;

    public TMP_Text leftPageText;
    public TMP_Text rightPageText;

    public Image leftImage;
    public Image rightImage;

    public JournalPage[] pages;

    private int currentPage = 0;

    public static JournalUI Instance;

    void Awake()
    {
        Instance = this; 
    }
    void Start()
    {
        journalPanel.SetActive(false);
        DisplayPage();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (
                (InputLock.Instance.GameplayInputEnabled &&
                !GameInputState.DialogueActive)
                || journalPanel.activeSelf
               )
            {
                ToggleJournal();
            }
        }
    }

    void ToggleJournal()
    {
        bool isOpen = !journalPanel.activeSelf;

        journalPanel.SetActive(isOpen);

        // lock gameplay input when journal is open
        InputLock.Instance.GameplayInputEnabled = !isOpen;

    }

    public void OpenJournal()
    {
        if (!journalPanel.activeSelf)
        {
            journalPanel.SetActive(true);
            InputLock.Instance.GameplayInputEnabled = false; // stop player from moving
        }
    }

    public void CloseJournal()
    {
        if (journalPanel.activeSelf)
        {
            journalPanel.SetActive(false);
            InputLock.Instance.GameplayInputEnabled = true; // restore gameplay input
        }
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            DisplayPage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayPage();
        }
    }

    void DisplayPage()
    {
        JournalPage page = pages[currentPage];

        leftPageText.text = page.leftPageText;
        rightPageText.text = page.rightPageText;

        leftImage.sprite = page.leftImage;
        rightImage.sprite = page.rightImage;
    }
}