using UnityEngine;
using System.Collections;

public class StairsManager : MonoBehaviour
{
    [Tooltip("If set, stairs up are disabled until this dialogue ID has been viewed (Mom primary dialogue in Ch.1).")]
    public string requiredDialogueIdToGoUp = "";

    [Tooltip("Played when the player tries to go up before requiredDialogueIdToGoUp is met. Leave dialogueID empty on the asset so it can repeat.")]
    public DialogueAsset blockedUpstairsDialogue;

    [Tooltip("Minimum seconds between blocked-stairs dialogue plays.")]
    public float blockedUpstairsDialogueCooldown = 1.5f;

    public bool isOnSecondFloor = false;
    public bool isTransitioning = false;
    public Collider2D upStairCollider;
    public Collider2D downStairCollider;
    public FloorTransition floorTransition;
    public SpriteRenderer stairsRenderer;
    public int belowPlayerOrder = 0;
    public int belowFloorOrder = -10;

    private float lastBlockedUpstairsDialogueTime = -1000f;

    public bool CanGoUp()
    {
        if (string.IsNullOrEmpty(requiredDialogueIdToGoUp))
            return true;
        if (SaveSystem.Instance == null)
            return true;
        return SaveSystem.Instance.HasViewedDialogue(requiredDialogueIdToGoUp);
    }

    public void TryPlayBlockedUpstairsDialogue()
    {
        if (blockedUpstairsDialogue == null)
            return;
        if (DialogueSystem.Instance == null)
            return;
        if (DialogueSystem.Instance.IsDialogueActive())
            return;
        if (Time.time - lastBlockedUpstairsDialogueTime < blockedUpstairsDialogueCooldown)
            return;

        lastBlockedUpstairsDialogueTime = Time.time;
        DialogueSystem.Instance.QueueDialogue(blockedUpstairsDialogue);
    }

    void Start()
    {
        //Load which floor player should be on when scene starts, true means the player is on the second floor
       isOnSecondFloor = floorTransition.LoadFloorState();
        UpdateStairCollider();
    }

    public void UseStairs(bool goingUp)
    {
        if (isTransitioning) return;
        if (goingUp && !isOnSecondFloor && !CanGoUp())
            return;

        if ((goingUp && !isOnSecondFloor) || (!goingUp && isOnSecondFloor))
        {
            StartCoroutine(HandleTransition(goingUp));
        }
    }
    private IEnumerator HandleTransition(bool goingUp)      //handle transition with global lock to ensure player does not go back and forth on stairs
    {
        isTransitioning = true;
        floorTransition.TriggerTransition();
        isOnSecondFloor = goingUp;          //update floor state
        UpdateStairCollider();
        yield return new WaitForSeconds(floorTransition.fadeDuration);  //finish fade animation before ending transition
        UpdateStairVisual();
        isTransitioning = false;
    }

    void UpdateStairCollider()
    {
        if (upStairCollider != null)
        {
            upStairCollider.enabled = !isOnSecondFloor;
            downStairCollider.enabled = !isOnSecondFloor;
        }
    }

    void UpdateStairVisual()
    {
        if (stairsRenderer == null) return;
        if (!isOnSecondFloor)
        {
            stairsRenderer.sortingLayerName = "Environment";
            stairsRenderer.sortingOrder = belowPlayerOrder;
        }
        else
        {
            stairsRenderer.sortingLayerName = "Background";
            stairsRenderer.sortingOrder = belowFloorOrder;
        }
    }
}
