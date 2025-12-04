using UnityEngine;

public class ReassemblyPuzzleManager : MonoBehaviour
{
    public RectTransform[] snapPoints;      //final target positions for the paper fragments
    public FragmentMovement[] fragments;
    public float snapDistance = 40f;

    public void CheckFragmentPosition(FragmentMovement fragment)
    {
        for(int i = 0; i < fragments.Length; i++)
        {
            if(fragments[i] == fragment)
            {
                float dist = Vector2.Distance(fragment.rectTransform.anchoredPosition, snapPoints[i].anchoredPosition);
                if(dist < snapDistance)        //check if the fragment position is within distance of its target location
                {
                    fragment.rectTransform.anchoredPosition = snapPoints[i].anchoredPosition;       //snap to the correct position
                    fragment.locked = true;        //lock in the position and prevent further movement
                }
                break;
            }
        }
        CheckPuzzleCompletion();
    }

    private void CheckPuzzleCompletion()
    {
        foreach(var frag in fragments)
        {
            if (!frag.locked)
                return;
        }
        Debug.Log("Reassembled clue note!");
    }
}
