using UnityEngine;

public class PlayerReturnSystem : MonoBehaviour
{
    public static PlayerReturnSystem Instance;

    public Vector3 savedPosition;
    public string returnSceneName = "Chapter1_Home";  

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
