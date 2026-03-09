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

    void Start()
    {
        journalPanel.SetActive(false);
        DisplayPage();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleJournal();
        }
    }

    void ToggleJournal()
    {
        journalPanel.SetActive(!journalPanel.activeSelf);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = journalPanel.activeSelf;
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