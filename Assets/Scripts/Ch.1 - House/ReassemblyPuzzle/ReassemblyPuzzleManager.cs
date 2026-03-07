using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ReassemblyPuzzleManager : MonoBehaviour
{
    public FragmentMovement[] fragments;
    public float snapDistance = 40f;
    public GameObject fragmentsParent;
    public Image finalResult;

    public void CheckFragmentPosition(FragmentMovement fragment)
    {
        foreach(var connection in fragment.connections)
        {
            FragmentMovement other = connection.otherFragment;
            Vector2 currentOffset = other.rectTransform.anchoredPosition - fragment.rectTransform.anchoredPosition;
            if(Vector2.Distance(currentOffset, connection.expectedOffset) < snapDistance)
            {
                SnapAndMerge(fragment, other, connection.expectedOffset);
                break;
            }
            
        }
        CheckPuzzleCompletion();
    }

    private void SnapAndMerge(FragmentMovement current, FragmentMovement other, Vector2 expectedOffset)
    {
        other.rectTransform.anchoredPosition = current.rectTransform.anchoredPosition + expectedOffset;
        RectTransform rootCurrent = current.groupRoot;
        RectTransform rootOther = other.groupRoot;

        if (rootCurrent == rootOther) return;

        Transform[] children = new Transform[rootOther.childCount];
        for(int i = 0; i < rootOther.childCount; i++)
            children[i] = rootOther.GetChild(i);
        foreach (Transform child in children)
            child.SetParent(rootCurrent, true);
        rootOther.SetParent(rootCurrent, true);
        foreach(var frag in fragments)
        {
            if(frag.transform.IsChildOf(rootCurrent))
                frag.groupRoot = rootCurrent;
        }
    }

    private void CheckPuzzleCompletion()
    {
        RectTransform firstGroup = fragments[0].groupRoot;
        foreach(var frag in fragments)
        {
            if (frag.groupRoot != firstGroup)
                return;
        }
        Debug.Log("Reassembled clue note!");
        StartCoroutine(ShowSuccessEvent());
    }

    private IEnumerator ShowSuccessEvent()
    {
        foreach (var frag in fragments)
            frag.locked = true;
        fragmentsParent.SetActive(false);
        finalResult.gameObject.SetActive(true);
        float t = 0f;
        float duration = 0.5f;

        Vector3 startScale = Vector3.one * 2.7f;
        Vector3 endScale = Vector3.one * 3f;

        finalResult.rectTransform.localScale = startScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            finalResult.rectTransform.localScale =
                Vector3.Lerp(startScale, endScale, normalized);

            yield return null;
        }
        finalResult.rectTransform.localScale = endScale;
    }
}
