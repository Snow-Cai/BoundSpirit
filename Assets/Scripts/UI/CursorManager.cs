using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CursorManager : MonoBehaviour
{
    public Texture2D defaultCursor;
    public Texture2D textCursor;

    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 textHotspot = new Vector2(16, 16);

    [Header("Cursor Tint")]
    public Color defaultCursorColor = Color.white;
    public Color textCursorColor = Color.white;

    Texture2D tintedDefaultCursor;
    Texture2D tintedTextCursor;

    bool usingTextCursor = false;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        tintedDefaultCursor = TintCursor(defaultCursor, defaultCursorColor);
        tintedTextCursor = TintCursor(textCursor, textCursorColor);

        Cursor.SetCursor(tintedDefaultCursor, defaultHotspot, CursorMode.Auto);
    }

    void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null)
        {
            TMP_InputField input = selected.GetComponent<TMP_InputField>();

            if (input != null)
            {
                if (!usingTextCursor)
                {
                    Cursor.SetCursor(tintedTextCursor, textHotspot, CursorMode.Auto);
                    usingTextCursor = true;
                }
                return;
            }
        }

        if (usingTextCursor)
        {
            Cursor.SetCursor(tintedDefaultCursor, defaultHotspot, CursorMode.Auto);
            usingTextCursor = false;
        }
    }

    Texture2D TintCursor(Texture2D original, Color tint)
    {
        Texture2D newTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);

        Color[] pixels = original.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] *= tint;
        }

        newTexture.SetPixels(pixels);
        newTexture.Apply();

        return newTexture;
    }
}