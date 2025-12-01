using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public AudioClip voiceClip; //sound effect maybe?
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public int nextDialogueIndex; //which dialogue this leads to
}

[System.Serializable]
public class Dialogue
{
    public string dialogueID; //Unique ID for saving
    public List<DialogueLine> lines;
    public List<DialogueChoice> choices; //Empty if no choices
    public bool saveAfterDialogue = true;
}

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialogueBox;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choiceButtonContainer;
    public GameObject choiceButtonPrefab;

    [Header("Animation")]
    public float textSpeed = 0.05f;
    public bool skipTypingOnClick = true;

    [Header("Audio")]
    public AudioClip dialogueOpenSound;
    public AudioClip dialogueCloseSound;
    public AudioClip textBlipSound;

    private Queue<DialogueLine> dialogueQueue;
    private List<DialogueChoice> currentChoices;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Dialogue currentDialogue;
    private int currentLineIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        dialogueQueue = new Queue<DialogueLine>();
    }

    void Start()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }
    }

    void Update()
    {
        if (dialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping && skipTypingOnClick)
            {
                StopAllCoroutines();
                CompleteText();
            }
            else if (!isTyping)
            {
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogueActive) return;

        currentDialogue = dialogue;
        currentLineIndex = 0;
        dialogueActive = true;

        //Check if already viewed
        if (SaveSystem.Instance != null)
        {
            if (!SaveSystem.Instance.HasViewedDialogue(dialogue.dialogueID))
            {
                SaveSystem.Instance.MarkDialogueViewed(dialogue.dialogueID);
            }
        }

        //Show dialogue box
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        //Play open sound
        if (dialogueOpenSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(dialogueOpenSound);
        }

        //Clear queue and add all lines
        dialogueQueue.Clear();
        foreach (DialogueLine line in dialogue.lines)
        {
            dialogueQueue.Enqueue(line);
        }

        currentChoices = dialogue.choices;

        DisplayNextLine();
    }

    void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        currentLineIndex++;

        //Update speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        //Play voice clip if exists
        if (line.voiceClip != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(line.voiceClip);
        }

        //Type out text
        StopAllCoroutines();
        StartCoroutine(TypeText(line.dialogueText));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;

            //Play text blip sound
            if (textBlipSound != null && UIAudioManager.Instance != null && letter != ' ')
            {
                UIAudioManager.Instance.PlayOneShot(textBlipSound, 0.3f);
            }

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    void CompleteText()
    {
        StopAllCoroutines();
        isTyping = false;
        //Show full text immediately
    }

    void EndDialogue()
    {
        //Check if there are choices
        if (currentChoices != null && currentChoices.Count > 0)
        {
            ShowChoices();
        }
        else
        {
            CloseDialogue();
        }
    }

    void ShowChoices()
    {
        if (choiceButtonContainer != null && choiceButtonPrefab != null)
        {
            choiceButtonContainer.SetActive(true);

            //Clear existing buttons
            foreach (Transform child in choiceButtonContainer.transform)
            {
                Destroy(child.gameObject);
            }

            //Create choice buttons
            for (int i = 0; i < currentChoices.Count; i++)
            {
                int choiceIndex = i; //Capture for lambda
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = currentChoices[i].choiceText;
                }

                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }
            }
        }
    }

    void OnChoiceSelected(int choiceIndex)
    {
        //Save choice
        if (SaveSystem.Instance != null && currentDialogue != null)
        {
            SaveSystem.Instance.SaveDialogueChoice(currentDialogue.dialogueID, choiceIndex);
        }

        //Play button sound
        if (UIAudioManager.Instance != null)
        {
            UIButtonSound buttonSound = FindObjectOfType<UIButtonSound>();
            if (buttonSound != null && buttonSound.clickSound != null)
            {
                UIAudioManager.Instance.PlayOneShot(buttonSound.clickSound);
            }
        }

        //Hide choices
        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }

        //Handle choice result (you can expand this based on your needs)
        Debug.Log("Choice selected: " + currentChoices[choiceIndex].choiceText);

        CloseDialogue();
    }

    void CloseDialogue()
    {
        dialogueActive = false;

        //Save game if needed
        if (currentDialogue != null && currentDialogue.saveAfterDialogue && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }

        //Play close sound
        if (dialogueCloseSound != null && UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayOneShot(dialogueCloseSound);
        }

        //Hide dialogue box
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.SetActive(false);
        }
    }

    public bool IsDialogueActive()
    {
        return dialogueActive;
    }
}