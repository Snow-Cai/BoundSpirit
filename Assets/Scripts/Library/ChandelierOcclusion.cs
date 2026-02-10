using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ChandelierOcclusion : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public SpriteRenderer chandelier, chandelierChain;
    public Light2D chandelierLight;

    [Header("Reveal Distances")]
    public float shadeDistanceAbove = 2f;
    public float shadeDistanceBelow = 1f;

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    public float shadedBrightness = 0.3f;
    public float normalBrightness = 1f;

    [Header("Fade Speed")]
    public float fadeSpeed = 5f;

    private Color targetColor;

    private void Reset()
    {
        chandelier = GetComponent<SpriteRenderer>();
        Transform chainTransform = transform.Find("ChandelierChain_0");
        if(chainTransform != null)
            chandelierChain = chainTransform.GetComponent<SpriteRenderer>();
        chandelierLight = GetComponentInChildren<Light2D>();
    }

    private void Update()
    {
        if (player == null || chandelier == null)
            return;
        float diff = player.position.y - transform.position.y;
        float t = Mathf.InverseLerp(shadeDistanceAbove, -shadeDistanceBelow, diff);                     //Calculate distance fade to have gradual shadow fade
        float brightness = Mathf.Lerp(shadedBrightness, normalBrightness, t);                           //Smooth the brightness change
        targetColor = new Color(brightness, brightness, brightness);
        chandelier.color = Color.Lerp(chandelier.color, targetColor, Time.deltaTime * fadeSpeed);       //Fade the sprite smoothly
        chandelierChain.color = Color.Lerp(chandelierChain.color, targetColor, Time.deltaTime * fadeSpeed);
    }
}
