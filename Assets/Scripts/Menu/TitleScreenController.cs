using UnityEngine;

public class TitleScreenController : MonoBehaviour
{
    [Header("Key Rows")]
    public TitleKey[] boundKeys;        //5 keys: B O U N D
    public TitleKey[] spiritKeys;       //6 keys: S P I R I T

    [Header("Layout")]
    public float keySize = 32f;         //width of each key
    public float keySpacing = 4f;       //gap between keys
    public float upOffset = 10f;        //how far up even keys go
    public float downOffset = 10f;      //how far down odd keys go
    public float rowGap = 16f;          //vertical gap between BOUND and SPIRIT rows

    private void Start()
    {
        LayoutRow(boundKeys, 0f);
        LayoutRow(spiritKeys, 0f);
        AssignController(boundKeys);
        AssignController(spiritKeys);
    }

    private void LayoutRow(TitleKey[] keys, float rowY)
    {
        if (keys == null || keys.Length == 0) return;

        float totalWidth = keys.Length * keySize + (keys.Length - 1) * keySpacing;
        float startX = -totalWidth / 2f + keySize / 2f;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == null) continue;

            RectTransform rt = keys[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            float x = startX + i * (keySize + keySpacing);
            float y = rowY + (i % 2 == 0 ? upOffset : -downOffset);

            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(keySize, keySize);

            //capture position after setting it
            keys[i].CapturePosition();
        }
    }

    private void AssignController(TitleKey[] keys)
    {
        if (keys == null) return;
        foreach (var key in keys)
            if (key != null)
                key.titleController = this;
    }
}