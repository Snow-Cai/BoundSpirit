using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ReturnToHouse : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (PlayerReturnSystem.Instance != null)
            {
                string sceneName = PlayerReturnSystem.Instance.returnSceneName;
                SceneManager.sceneLoaded += OnSceneLoaded;

             
                SceneManager.LoadScene(sceneName);
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        SceneManager.sceneLoaded -= OnSceneLoaded;

       
        StartCoroutine(RestorePosition());
    }

    private IEnumerator RestorePosition()
    {
        yield return null;                  
        yield return new WaitForEndOfFrame(); 

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerObj.transform.position = PlayerReturnSystem.Instance.savedPosition;
            Debug.Log("Returned player to: " + PlayerReturnSystem.Instance.savedPosition);
        }
        else
        {
            Debug.LogError("Player object not found in house scene!");
        }
    }
}
