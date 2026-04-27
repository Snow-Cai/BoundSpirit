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

    [Header("Navigation (optional)")]
    [Tooltip("If unset, a child named Prev under the journal panel is used (HelpBookCanvas).")]
    [SerializeField] private GameObject prevPageButton;
    [Tooltip("If unset, a child named Next under the journal panel is used (HelpBookCanvas).")]
    [SerializeField] private GameObject nextPageButton;

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
            InputLock.Instance.CanToggleInventory = !isOpen;
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
                InputLock.Instance.CanToggleInventory = false;
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
                InputLock.Instance.CanToggleInventory = true;
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
            UpdateNavButtonVisibility();
            return;
        }

        if (currentPage < 0 || currentPage >= pages.Length)
        {
            currentPage = Mathf.Clamp(currentPage, 0, pages.Length - 1);
        }

        JournalPage page = pages[currentPage];
        if (page == null)
        {
            UpdateNavButtonVisibility();
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

        UpdateNavButtonVisibility();
    }

    private void UpdateNavButtonVisibility()
    {
        ResolveNavButtons(out GameObject prev, out GameObject next);

        if (pages == null || pages.Length == 0)
        {
            if (prev != null)
            {
                prev.SetActive(false);
            }

            if (next != null)
            {
                next.SetActive(false);
            }

            return;
        }

        int lastIndex = pages.Length - 1;
        if (prev != null)
        {
            prev.SetActive(currentPage > 0);
        }

        if (next != null)
        {
            next.SetActive(currentPage < lastIndex);
        }
    }

    private void ResolveNavButtons(out GameObject prev, out GameObject next)
    {
        prev = prevPageButton;
        next = nextPageButton;

        if (journalPanel == null)
        {
            return;
        }

        if (prev == null)
        {
            Transform t = journalPanel.transform.Find("Prev");
            if (t != null)
            {
                prev = t.gameObject;
            }
        }

        if (next == null)
        {
            Transform t = journalPanel.transform.Find("Next");
            if (t != null)
            {
                next = t.gameObject;
            }
        }
    }
}