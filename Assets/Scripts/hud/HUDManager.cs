using UnityEngine;
using UnityEngine.SceneManagement;

//manages the persistent gameplay HUD (puzzle tracker, future hint button, etc.)
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("HUD Root")]
    [Tooltip("The CanvasGroup on the HUD root. Used for smooth fade in/out.")]
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Visibility")]
    [Tooltip("Scenes where the HUD should never appear (e.g. MenuScene, cutscene scenes).")]
    [SerializeField] private string[] hiddenInScenes = { "MenuScene" };

    [Tooltip("Fade speed when showing/hiding the HUD.")]
    [SerializeField] private float fadeSpeed = 8f;

    // ------------------------------------------------------------------
    private float targetAlpha = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //keep HUD alive across scene loads so it doesn't need re-wiring each scene
        DontDestroyOnLoad(gameObject);

        if (hudCanvasGroup == null)
            hudCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //PuzzleProgressUI refresh its cached puzzle list for the new scene
        PuzzleProgressUI tracker = GetComponentInChildren<PuzzleProgressUI>(true);
        if (tracker != null)
            tracker.RefreshSceneData();
    }

    private void Update()
    {
        targetAlpha = ShouldShowHUD() ? 1f : 0f;

        if (hudCanvasGroup == null) return;

        hudCanvasGroup.alpha = Mathf.MoveTowards(
            hudCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);

        bool visible = hudCanvasGroup.alpha > 0.01f;
        hudCanvasGroup.interactable = visible;
        hudCanvasGroup.blocksRaycasts = false; //HUD never blocks gameplay clicks
    }

    private bool ShouldShowHUD()
    {
        //hide in excluded scenes (main menu, etc.)
        string sceneName = SceneManager.GetActiveScene().name;
        for (int i = 0; i < hiddenInScenes.Length; i++)
        {
            if (string.Equals(sceneName, hiddenInScenes[i], System.StringComparison.Ordinal))
                return false;
        }

        //hide during any active dialogue
        if (GameInputState.DialogueActive)
            return false;

        //hide during cutscenes
        if (GameInputState.MovementLocked)
            return false;

        //hide when gameplay input is fully blocked (puzzle UIs, safe, database, etc.)
        if (InputLock.Instance != null && !InputLock.Instance.GameplayInputEnabled)
            return false;

        //hide when pause menu is open (Time.timeScale == 0 is a reliable proxy)
        if (Time.timeScale == 0f)
            return false;

        return true;
    }
}