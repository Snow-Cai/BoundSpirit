using UnityEngine;

public class FragmentMovement : MonoBehaviour
{
    private bool dragging = false;
    private Vector3 offset;

    private void OnMouseDown()      //when clicking down
    {
        dragging = true;
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDrag()
    {
        if(dragging)
        {
            Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
            newPos.z = 0;       //maintain z position
            transform.position = newPos;
        }
    }

    private void OnMouseUp()        //when letting go of click
    {
        dragging = false;
        FindFirstObjectByType<ReassemblyPuzzleManager>().CheckFragmentPosition(this);
    }
}
