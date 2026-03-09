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

    void Start()
    {
        tintedDefaultCursor = TintCursor(defaultCursor, defaultCursorColor);
        tintedTextCursor = TintCursor(textCursor, textCursorColor);
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            TMP_InputField input = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();

            if (input != null)
            {
                Cursor.SetCursor(tintedTextCursor, textHotspot, CursorMode.Auto);
                return;
            }
        }

        Cursor.SetCursor(tintedDefaultCursor, defaultHotspot, CursorMode.Auto);
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