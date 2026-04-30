using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;

public class SafeControllerKeypad : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI inputText;
    public Image successLight;
    public RectTransform knob;
    public CanvasGroup safeCanvas;
    public CanvasGroup weaponCanvas;
    public Image weaponObject;

    [Header("Keypad Settings")]
    public int maxDigits = 6;               //6-digit code
    public string targetCode = "333333";    //adjust to desired passcode for success

    [Header("Keyhole")]
    public bool keyInserted = false;        //checks if the physical key was inserted
    [SerializeField] private ItemData itemToConsume;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonPressSound;
    public AudioClip knobTurnSound;
    public AudioClip keyholeEmptySound;
    public AudioClip keyInsertSound;
    public AudioClip keyPickupSound;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onUnlock;
    public UnityEngine.Events.UnityEvent onFail;

    [Header("Save")]
    [Tooltip("If set, SaveSystem.UnlockPuzzle is called when the safe opens (for door gates / progression).")]
    public string savePuzzleIdWhenUnlocked;

    [Header("Dialogue")]
    [Tooltip("Played once when the correct code succeeds (after knob turn), not when opening the safe.")]
    public DialogueAsset dialogueOnUnlock;

    [Header("Solve Bridge")]
    [SerializeField] private InteractableObject puzzleInteractable;

    private StringBuilder currentInput = new StringBuilder();
    private bool hasUnlockedSuccessfully;
    private SafeInteraction cachedSafeInteraction;

    private void Awake()
    {
        if (puzzleInteractable == null)
        {
            SafeInteraction safeInteraction = FindFirstObjectByType<SafeInteraction>();
            if (safeInteraction != null)
            {
                puzzleInteractable = safeInteraction.GetComponent<InteractableObject>();
            }
        }

        cachedSafeInteraction = FindFirstObjectByType<SafeInteraction>();
    }

    public void OnDigitPressed(string digit)
    {
        if (currentInput.Length >= maxDigits) return;
        currentInput.Append(digit);
        UpdateInputText();
        PlayButtonSound();
    }

    public void OnClearPressed()            //when pressing *
    {
        currentInput.Clear();
        UpdateInputText();
        PlayButtonSound();
    }

    public void OnEnterPressed()            //when pressing #
    {
        PlayButtonSound();
        if (!keyInserted)
        {
            FailMessage("Insert Key First!");
            return;
        }
        if (CheckSafeUnlockRequirements())
            HandleUnlock();
        else
            FailMessage("Wrong Code!");
    }

    void UpdateInputText()          //update with user input
    {
        if (inputText != null)
            inputText.text = currentInput.ToString();
    }

    void FailMessage(string message)        //flashes appropriate error message
    {
        if(inputText != null)
            inputText.text = message;

        if (successLight != null)               //red fail light
            StartCoroutine(FlashLight(3, 0.2f, UnityEngine.Color.red));     //3 flashes, 0.2s each

        StartCoroutine(ClearInputAfterDelay(1f));       //keep error message on for a delay
        onFail?.Invoke();
    }

    IEnumerator ClearInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentInput.Clear();
        UpdateInputText();
    }

    IEnumerator FlashLight(int flashCount, float duration, UnityEngine.Color color)         //light up depending on success/failure
    {
        UnityEngine.Color original = successLight.color;
        for(int i = 0; i < flashCount; i++)
        {
            successLight.color = color;
            yield return new WaitForSeconds(duration);
            successLight.color = original;
            yield return new WaitForSeconds(duration);
        }
    }

    public bool CheckSafeUnlockRequirements()
    {
        return currentInput.ToString() == targetCode && keyInserted;
    }

    void HandleUnlock()         //unlock on success
    {
        if (hasUnlockedSuccessfully)
            return;
        hasUnlockedSuccessfully = true;
        SetSafeTransitionInputLocked(true);

        if (successLight != null)
            successLight.color = UnityEngine.Color.green;
        inputText.text = "UNLOCKED";

        if (puzzleInteractable != null && !string.IsNullOrEmpty(puzzleInteractable.puzzleID))
        {
            puzzleInteractable.OnPuzzleSolved();
        }
        else if (!string.IsNullOrEmpty(savePuzzleIdWhenUnlocked) && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UnlockPuzzle(savePuzzleIdWhenUnlocked);
        }

        onUnlock?.Invoke();

        if (knob != null)
            StartCoroutine(UnlockAfterKnobRoutine());
        else
            StartCoroutine(UnlockTransitionSequence());
    }

    IEnumerator UnlockAfterKnobRoutine()
    {
        yield return StartCoroutine(RotateKnob());
        StartCoroutine(UnlockTransitionSequence());
    }

    IEnumerator UnlockTransitionSequence()
    {
        yield return StartCoroutine(FadeCanvas(safeCanvas, 1f, 0f, 0.5f));
        if (weaponObject != null) weaponObject.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvas(weaponCanvas, 0f, 1f, 0.5f));
        yield return StartCoroutine(FadeImage(weaponObject, 0f, 1f, 0.5f));
        yield return new WaitForSeconds(0.3f);
        PlaySolveDialogueIfConfigured();
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while(t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t /  duration);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if(img == null) yield break;
        Color c = img.color;
        float t = 0f;

        c.a = from;
        img.color = c;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        img.color = new Color(c.r, c.g, c.b, to);
    }

    void PlaySolveDialogueIfConfigured()
    {
        if (dialogueOnUnlock == null || DialogueSystem.Instance == null)
            return;
        DialogueSystem.Instance.StartDialogue(dialogueOnUnlock);
        StartCoroutine(WaitForDialogueThenClose());
    }

    IEnumerator WaitForDialogueThenClose()
    {
        yield return new WaitUntil(() => DialogueSystem.Instance == null || !DialogueSystem.Instance.IsDialogueActive());
        if (safeCanvas != null) safeCanvas.gameObject.SetActive(false);
        if (weaponObject != null) weaponObject.gameObject.SetActive(false);
        if (weaponCanvas != null) weaponCanvas.alpha = 0f;
        if (safeCanvas != null)
        {
            safeCanvas.interactable = false;
            safeCanvas.blocksRaycasts = false;
        }

        if (cachedSafeInteraction == null)
            cachedSafeInteraction = FindFirstObjectByType<SafeInteraction>();

        if (cachedSafeInteraction != null)
        {
            cachedSafeInteraction.SetOpen();
            cachedSafeInteraction.SetSolved();
            cachedSafeInteraction.SetTransitionLocked(false);
        }

        //Queue the weapon reaction clarity choice
        DialogueAsset weaponReaction = Resources.Load<DialogueAsset>("Chapter1_weaponReaction");
        if (weaponReaction != null && DialogueSystem.Instance != null)
            DialogueSystem.Instance.QueueDialogue(weaponReaction);
        SetSafeTransitionInputLocked(false);
    }

    IEnumerator RotateKnob()        //rotate knob animation on success for opening safe
    {
        SetSafeTransitionInputLocked(true);
        if (audioSource && knobTurnSound)
            audioSource.PlayOneShot(knobTurnSound);
        float duration = 0.5f;
        float time = 0f;
        Quaternion startRotation = knob.localRotation;
        Quaternion endRotation = Quaternion.Euler(0, 0, 0);
        while(time < duration)
        {
            knob.localRotation = Quaternion.Slerp(startRotation, endRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        knob.localRotation = endRotation;
    }

    private void SetSafeTransitionInputLocked(bool locked)
    {
        if (InputLock.Instance != null)
        {
            InputLock.Instance.CanToggleInventory = !locked;
            InputLock.Instance.GameplayInputEnabled = !locked;
            InputLock.Instance.InteractEnabled = !locked;
        }

        if (cachedSafeInteraction == null)
            cachedSafeInteraction = FindFirstObjectByType<SafeInteraction>();

        if (cachedSafeInteraction != null)
            cachedSafeInteraction.SetTransitionLocked(locked);
    }

    public void InsertKey()     //call when inserting key in safe interaction
    {
        keyInserted = true;
        if (itemToConsume != null)
        {
            PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
            if (inv != null)
                inv.RemoveItem(itemToConsume);
        }
    }

    void PlayButtonSound()          //plays for all keypad buttons
    {
        if(audioSource != null && buttonPressSound != null)
            audioSource.PlayOneShot(buttonPressSound);
    }
}
