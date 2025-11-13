using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
public class SafeInteraction : MonoBehaviour
{
    public GameObject safeUIPanel;
    public Camera mainCamera;
    public float zoomSize = 3f;
    public float zoomDuration = 0.5f;
    public float interactionRadius = 1.5f;
    public Transform playerTransform;

    private float originalSize;
    private Vector3 originalPosition;
    private CanvasGroup cg;
    private bool isOpen = false;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        originalSize = mainCamera.orthographicSize;
        originalPosition = mainCamera.transform.position;
        cg = safeUIPanel.GetComponent<CanvasGroup>();
        if(cg == null)
            cg = safeUIPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        safeUIPanel.SetActive(false);
    }
    private void Update()
    {
        if(playerTransform != null)
        {
            float dist = Vector2.Distance(playerTransform.position, transform.position);
            if(!isOpen && dist <= interactionRadius && Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(OpenSafe());
            }
            if(isOpen && Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(CloseRoutine());
            }
        }
    }
    private IEnumerator OpenSafe()
    {
        isOpen = true;
        safeUIPanel.SetActive(true);
        //disable player movement
        CharMovement movement = playerTransform.GetComponent<CharMovement>();
        if(movement != null) movement.enabled = false;
        //camera zoom in on player
        float t = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y, startPos.z);
        float startSize = mainCamera.orthographicSize;
        while(t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, zoomSize, t / zoomDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t / zoomDuration);
            yield return null;
        }
        mainCamera.orthographicSize = zoomSize;
        mainCamera.transform.position = targetPos;
        //panel appears and disable movement
        cg.alpha = 1;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
    public void CloseSafe()
    {
        StartCoroutine(CloseRoutine());
    }
    private IEnumerator CloseRoutine()
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = 0;
        safeUIPanel.SetActive(false);
        //zoom back out
        float t = 0f;
        float startSize = mainCamera.orthographicSize;
        Vector3 startPos = mainCamera.transform.position;
        while(t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, originalSize, t / zoomDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, originalPosition, t / zoomDuration);
            yield return null;
        }
        mainCamera.orthographicSize = originalSize;
        mainCamera.transform.position = originalPosition;
        //enable player movement again
        CharMovement movement = FindFirstObjectByType<CharMovement>();
        if (movement != null) movement.enabled = true;
        isOpen = false;
    }
}
