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
        if (journalPanel != null)
        {
            journalPanel.SetActive(false);
        }

        DisplayPage();
    }

    void Update()
    {
        if (journalPanel == null || !Input.GetKeyDown(KeyCode.H))
        {
            return;
        }

        bool canToggle =
            (InputLock.Instance != null && InputLock.Instance.GameplayInputEnabled &&
             !GameInputState.DialogueActive)
            || journalPanel.activeSelf;

        if (canToggle)
        {
            ToggleJournal();
        }
    }

    void ToggleJournal()
    {
        if (journalPanel == null)
        {
            return;
        }

        bool isOpen = !journalPanel.activeSelf;

        journalPanel.SetActive(isOpen);

        if (InputLock.Instance != null)
        {
            InputLock.Instance.GameplayInputEnabled = !isOpen;
        }
    }

    public void OpenJournal()
    {
        if (journalPanel == null)
        {
            return;
        }

        if (!journalPanel.activeSelf)
        {
            journalPanel.SetActive(true);
            if (InputLock.Instance != null)
            {
                InputLock.Instance.GameplayInputEnabled = false;
            }
        }
    }

    public void CloseJournal()
    {
        if (journalPanel == null)
        {
            return;
        }

        if (journalPanel.activeSelf)
        {
            journalPanel.SetActive(false);
            if (InputLock.Instance != null)
            {
                InputLock.Instance.GameplayInputEnabled = true;
            }
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
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (currentPage < 0 || currentPage >= pages.Length)
        {
            currentPage = Mathf.Clamp(currentPage, 0, pages.Length - 1);
        }

        JournalPage page = pages[currentPage];
        if (page == null)
        {
            return;
        }

        if (leftPageText != null)
        {
            leftPageText.text = page.leftPageText;
        }

        if (rightPageText != null)
        {
            rightPageText.text = page.rightPageText;
        }

        if (leftImage != null)
        {
            leftImage.sprite = page.leftImage;
        }

        if (rightImage != null)
        {
            rightImage.sprite = page.rightImage;
        }
    }
}