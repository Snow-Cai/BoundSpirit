using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

    void Awake()
    {
        EnsureCursorTextures();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        EnsureCursorTextures();
        ApplyDefaultCursor();
    }

    void Update()
    {
        if (EventSystem.current == null)
        {
            if (usingTextCursor)
                ApplyDefaultCursor();
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null)
        {
            TMP_InputField input = selected.GetComponentInParent<TMP_InputField>();

            if (input != null)
            {
                if (!usingTextCursor)
                    ApplyTextCursor();
                return;
            }
        }

        if (usingTextCursor)
            ApplyDefaultCursor();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<TMP_InputField>() != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        ApplyDefaultCursor();
    }

    void ApplyDefaultCursor()
    {
        EnsureCursorTextures();
        Cursor.SetCursor(tintedDefaultCursor, defaultHotspot, CursorMode.Auto);
        usingTextCursor = false;
    }

    void ApplyTextCursor()
    {
        EnsureCursorTextures();
        Cursor.SetCursor(tintedTextCursor, textHotspot, CursorMode.Auto);
        usingTextCursor = true;
    }

    void EnsureCursorTextures()
    {
        if (tintedDefaultCursor == null && defaultCursor != null)
            tintedDefaultCursor = TintCursor(defaultCursor, defaultCursorColor);

        if (tintedTextCursor == null && textCursor != null)
            tintedTextCursor = TintCursor(textCursor, textCursorColor);
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
