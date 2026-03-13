using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Spawns small white dot twinkles that fade in and out across a defined area.
//handles the crystal ball aura shrinking on hover - not hooked up yet cause aura png has black bg

public class MenuTwinkleSpawner : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Twinkle Spawn Area")]
    public RectTransform twinkleParent;

    public Sprite twinkleSprite;

    [Header("Twinkle Settings")]
    public int maxTwinkles = 18;
    public float minSize = 4f;
    public float maxSize = 12f;
    public float minFadeDuration = 0.4f;
    public float maxFadeDuration = 1.0f;
    public float minLifetime = 0.6f;
    public float maxLifetime = 2.0f;
    public float spawnInterval = 0.18f;

    [Header("Aura (Purple Glow Behind Ball)")]
    public RectTransform auraRect;

    public Vector2 auraNormalSize = new Vector2(420f, 420f);

    public Vector2 auraHoverSize = new Vector2(280f, 280f);

    public float auraSizeSpeed = 5f;

    [Header("Hover Detection")]
    public RectTransform crystalBallRect;

    //Internal state
    private bool isHovering = false;
    private Vector2 auraTargetSize;
    private bool twinklesActive = false;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (auraRect != null)
        {
            auraRect.sizeDelta = auraNormalSize;
        }

        auraTargetSize = auraNormalSize;

        //Twinkles only appear on hover start inactive
        twinklesActive = false;
    }

    private void Update()
    {
        if (auraRect != null)
        {
            auraRect.sizeDelta = Vector2.Lerp(
                auraRect.sizeDelta,
                auraTargetSize,
                Time.unscaledDeltaTime * auraSizeSpeed
            );
        }

        //manual hover check via RectTransform (works with the interface methods)
        if (crystalBallRect != null)
        {
            bool mouseOver = RectTransformUtility.RectangleContainsScreenPoint(
                crystalBallRect,
                Input.mousePosition,
                null
            );

            if (mouseOver && !isHovering)
                OnHoverEnter();
            else if (!mouseOver && isHovering)
                OnHoverExit();
        }
    }

    //Called when mouse enters crystal ball area
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }

    private void OnHoverEnter()
    {
        if (isHovering) return;
        isHovering = true;

        //Shrink aura
        auraTargetSize = auraHoverSize;

        //Start spawning twinkles
        if (!twinklesActive)
        {
            twinklesActive = true;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = StartCoroutine(SpawnTwinkles());
        }
    }

    private void OnHoverExit()
    {
        if (!isHovering) return;
        isHovering = false;

        //Restore aura
        auraTargetSize = auraNormalSize;

        //Stop spawning new twinkles (existing ones fade out naturally)
        twinklesActive = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnTwinkles()
    {
        while (twinklesActive)
        {
            SpawnOneTwinkle();
            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private void SpawnOneTwinkle()
    {
        if (twinkleParent == null) return;

        //Create a new Image for the twinkle
        GameObject twinkleGO = new GameObject("Twinkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        twinkleGO.transform.SetParent(twinkleParent, false);

        Image img = twinkleGO.GetComponent<Image>();
        img.raycastTarget = false;

        if (twinkleSprite != null)
        {
            img.sprite = twinkleSprite;
        }
        else
        {
            //Fallback: plain white dot using Unity's built-in white sprite
            img.color = Color.white;
        }

        //Random position within the twinkleParent rect
        RectTransform rt = twinkleGO.GetComponent<RectTransform>();
        float halfW = twinkleParent.rect.width * 0.5f;
        float halfH = twinkleParent.rect.height * 0.5f;
        rt.anchoredPosition = new Vector2(
            Random.Range(-halfW, halfW),
            Random.Range(-halfH, halfH)
        );

        //Random size
        float size = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(size, size);

        StartCoroutine(TwinkleLifecycle(twinkleGO, img));
    }

    private IEnumerator TwinkleLifecycle(GameObject go, Image img)
    {
        if (go == null) yield break;

        float fadeIn = Random.Range(minFadeDuration, maxFadeDuration);
        float lifetime = Random.Range(minLifetime, maxLifetime);
        float fadeOut = Random.Range(minFadeDuration, maxFadeDuration);

        //fade in
        float t = 0f;
        while (t < fadeIn && go != null)
        {
            t += Time.unscaledDeltaTime;
            if (img != null)
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t / fadeIn));
            yield return null;
        }

        //Stay visible
        float elapsed = 0f;
        while (elapsed < lifetime && go != null)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        //Fade out
        t = 0f;
        while (t < fadeOut && go != null)
        {
            t += Time.unscaledDeltaTime;
            if (img != null)
                img.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / fadeOut));
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }
}