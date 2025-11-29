using UnityEngine;

public class ReassemblyPuzzleManager : MonoBehaviour
{
    public Transform[] snapPoints;      //final target positions for the paper fragments
    public FragmentMovement[] fragments;
    public float snapDistance = 0.2f;

    public void CheckFragmentPosition(FragmentMovement fragment)
    {
        for(int i = 0; i < fragments.Length; i++)
        {
            if(fragments[i] == fragment)
            {
                if(Vector2.Distance(fragment.transform.position, snapPoints[i].position) < snapDistance)        //check if the fragment position is within distance of its target location
                {
                    fragment.transform.position = snapPoints[i].position;       //snap to the correct position
                    fragment.GetComponent<Collider2D>().enabled = false;        //lock in the position and prevent further movement
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
            if (!frag.GetComponent<Collider2D>().enabled)
                continue;       //check if all fragments have reached the target position
            else
                return;
        }
        Debug.Log("Reassembled clue note!");
    }
}
