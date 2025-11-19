using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;
using UnityEditor.VersionControl;
using System.Drawing;

public class SafeControllerKeypad : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI inputText;
    public Image successLight;
    public RectTransform knob;

    [Header("Keypad Settings")]
    public int maxDigits = 6;               //6-digit code
    public string targetCode = "333333";    //adjust to desired passcode for success

    [Header("Keyhole")]
    public bool keyInserted = false;        //checks if the physical key was inserted

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

    private StringBuilder currentInput = new StringBuilder();

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
        if (currentInput.ToString() == targetCode)
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

        StartCoroutine(ClearInputAfterDelay(1f));
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

    void HandleUnlock()         //unlock on success
    {
        if (successLight != null)
            successLight.color = UnityEngine.Color.green;
        inputText.text = "UNLOCKED";
        if (knob != null)
            StartCoroutine(RotateKnob());
        onUnlock?.Invoke();
    }

    IEnumerator RotateKnob()        //rotate knob animation on success for opening safe
    {
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

    public void InsertKey()     //call when inserting key in safe interaction
    {
        keyInserted = true;
    }

    void PlayButtonSound()
    {
        if(audioSource != null && buttonPressSound != null)
            audioSource.PlayOneShot(buttonPressSound);
    }
}
