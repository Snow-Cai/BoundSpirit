using UnityEngine;

/// <summary>
/// Sparkle-only highlight effect for collectible objects.
/// </summary>
public class InteractableGlow : MonoBehaviour
{
    public enum HighlightStyle
    {
        CollectibleSparkle,
        InteractableSparkle
    }

    [Header("Sparkle")]
    [SerializeField] private bool showSparkle = true;
    [SerializeField] private Color sparkleColor = new(234f / 255f, 233f / 255f, 173f / 255f, 1f);
    [SerializeField] private float sparkleScale = 0.5f;
    [SerializeField] private Vector2 sparkleIntervalRange = new(0.5f, 2f);
    [SerializeField] private float sparkleAlphaPulseSpeed = 3.2f;
    [SerializeField] private float sparkleSizePulseSpeed = 2.2f;
    [SerializeField] private float companionScaleMultiplier = 0.5f;
    [SerializeField] private float companionOffsetRadius = 0.28f;

    private SpriteRenderer[] spriteRenderers;
    private float highlight;

    private GameObject sparkleRoot;
    private SpriteRenderer sparkleRenderer;
    private SpriteRenderer sparkleDiamondRenderer;
    private GameObject[] companionRoots;
    private SpriteRenderer[] companionRenderers;
    private SpriteRenderer[] companionDiamondRenderers;
    private float nextSparkleTime;
    private static Sprite cachedSparkleSprite;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        BuildSparkle();
        SetSparkleVisible(false);
    }

    private void Update()
    {
        UpdateSparkle();
    }

    public void SetHighlighted(bool isHighlighted)
    {
        highlight = isHighlighted ? 1f : 0f;
        if (!isHighlighted)
        {
            SetSparkleVisible(false);
        }
    }

    public void ApplyStyle(HighlightStyle style)
    {
        if (style == HighlightStyle.CollectibleSparkle)
        {
            showSparkle = true;
            sparkleColor = new Color(234f / 255f, 233f / 255f, 173f / 255f, 1f);
            sparkleScale = 0.5f;
            companionScaleMultiplier = 0.5f;
            sparkleIntervalRange = new Vector2(0.5f, 2f);
            return;
        }

        if (style == HighlightStyle.InteractableSparkle)
        {
            showSparkle = true;
            sparkleColor = new Color(199f / 255f, 175f / 255f, 92f / 255f, 1f);
            sparkleScale = 0.5f;
            companionScaleMultiplier = 0.5f;
            sparkleIntervalRange = new Vector2(0.5f, 2f);
            return;
        }
    }

    private void BuildSparkle()
    {
        sparkleRoot = new GameObject("__InteractSparkle");
        sparkleRoot.transform.SetParent(transform, false);
        sparkleRenderer = sparkleRoot.AddComponent<SpriteRenderer>();
        sparkleRenderer.sprite = GetSparkleSprite();
        sparkleRenderer.color = sparkleColor;
        if (spriteRenderers != null && spriteRenderers.Length > 0 && spriteRenderers[0] != null)
        {
            sparkleRenderer.sortingLayerID = spriteRenderers[0].sortingLayerID;
            sparkleRenderer.sortingOrder = spriteRenderers[0].sortingOrder + 10;
        }
        else
        {
            sparkleRenderer.sortingOrder = 10;
        }
        sparkleRenderer.enabled = false;

        GameObject diamondLayer = new GameObject("__InteractSparkleDiamond");
        diamondLayer.transform.SetParent(sparkleRoot.transform, false);
        sparkleDiamondRenderer = diamondLayer.AddComponent<SpriteRenderer>();
        sparkleDiamondRenderer.sprite = sparkleRenderer.sprite;
        sparkleDiamondRenderer.sortingLayerID = sparkleRenderer.sortingLayerID;
        sparkleDiamondRenderer.sortingOrder = sparkleRenderer.sortingOrder + 1;
        sparkleDiamondRenderer.color = sparkleColor;
        sparkleDiamondRenderer.enabled = false;
        diamondLayer.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        diamondLayer.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        companionRoots = new GameObject[2];
        companionRenderers = new SpriteRenderer[2];
        companionDiamondRenderers = new SpriteRenderer[2];
        for (int i = 0; i < companionRoots.Length; i++)
        {
            GameObject companionRoot = new GameObject("__InteractSparkleCompanion");
            companionRoot.transform.SetParent(transform, false);
            SpriteRenderer companion = companionRoot.AddComponent<SpriteRenderer>();
            companion.sprite = sparkleRenderer.sprite;
            companion.sortingLayerID = sparkleRenderer.sortingLayerID;
            companion.sortingOrder = sparkleRenderer.sortingOrder - 1;
            companion.color = sparkleColor;
            companion.enabled = false;

            GameObject companionDiamond = new GameObject("__InteractSparkleCompanionDiamond");
            companionDiamond.transform.SetParent(companionRoot.transform, false);
            SpriteRenderer companionDiamondRendererLocal = companionDiamond.AddComponent<SpriteRenderer>();
            companionDiamondRendererLocal.sprite = sparkleRenderer.sprite;
            companionDiamondRendererLocal.sortingLayerID = companion.sortingLayerID;
            companionDiamondRendererLocal.sortingOrder = companion.sortingOrder + 1;
            companionDiamondRendererLocal.color = sparkleColor;
            companionDiamondRendererLocal.enabled = false;
            companionDiamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            companionDiamond.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

            companionRoots[i] = companionRoot;
            companionRenderers[i] = companion;
            companionDiamondRenderers[i] = companionDiamondRendererLocal;
        }
    }

    private void UpdateSparkle()
    {
        if (sparkleRenderer == null)
        {
            return;
        }

        if (!showSparkle || highlight <= 0.01f)
        {
            SetSparkleVisible(false);
            return;
        }

        if (!sparkleRenderer.enabled || Time.time >= nextSparkleTime)
        {
            TriggerSparkle();
        }

        float t = Time.time;
        float alphaPulse = 0.5f + 0.5f * Mathf.Sin(t * sparkleAlphaPulseSpeed);
        Color color = sparkleColor;
        color.a *= Mathf.Lerp(0.65f, 1f, alphaPulse);
        sparkleRenderer.color = color;
        if (sparkleDiamondRenderer != null)
        {
            Color d = color;
            d.a *= 0.82f;
            sparkleDiamondRenderer.color = d;
        }

        float sizePulse = 0.96f + 0.08f * Mathf.Sin(t * sparkleSizePulseSpeed);
        SetSparkleRootWorldScale(sparkleRoot, sparkleScale * sizePulse);

        if (companionRenderers != null)
        {
            for (int i = 0; i < companionRenderers.Length; i++)
            {
                if (companionRenderers[i] == null)
                {
                    continue;
                }

                float phase = t + (i + 1) * 1.3f;
                float companionAlphaPulse = 0.52f + 0.48f * Mathf.Sin(phase * sparkleAlphaPulseSpeed);
                Color c = sparkleColor;
                c.a *= Mathf.Lerp(0.35f, 0.8f, companionAlphaPulse);
                companionRenderers[i].color = c;

                if (companionDiamondRenderers != null && companionDiamondRenderers[i] != null)
                {
                    Color cd = c;
                    cd.a *= 0.8f;
                    companionDiamondRenderers[i].color = cd;
                }

                float companionSizePulse = 0.95f + 0.1f * Mathf.Sin(phase * sparkleSizePulseSpeed);
                SetSparkleRootWorldScale(companionRoots[i], sparkleScale * companionScaleMultiplier * companionSizePulse);
            }
        }
    }

    private void TriggerSparkle()
    {
        Bounds bounds = ComputeSparkleBounds();
        Vector3 anchor = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            transform.position.z
        );

        sparkleRoot.transform.position = anchor;
        SetSparkleRootWorldScale(sparkleRoot, sparkleScale);
        sparkleRenderer.color = sparkleColor;
        if (sparkleDiamondRenderer != null)
        {
            Color d = sparkleColor;
            d.a *= 0.85f;
            sparkleDiamondRenderer.color = d;
        }

        if (companionRoots != null)
        {
            for (int i = 0; i < companionRoots.Length; i++)
            {
                if (companionRoots[i] == null)
                {
                    continue;
                }

                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero)
                {
                    dir = Vector2.right;
                }

                float radius = companionOffsetRadius * Random.Range(0.9f, 1.8f);
                Vector3 offset = new Vector3(dir.x * radius, dir.y * radius, 0f);
                companionRoots[i].transform.position = anchor + offset;
                SetSparkleRootWorldScale(companionRoots[i], sparkleScale * companionScaleMultiplier);

                if (companionRenderers != null && companionRenderers[i] != null)
                {
                    Color c = sparkleColor;
                    c.a *= 0.7f;
                    companionRenderers[i].color = c;
                }

                if (companionDiamondRenderers != null && companionDiamondRenderers[i] != null)
                {
                    Color cd = sparkleColor;
                    cd.a *= 0.55f;
                    companionDiamondRenderers[i].color = cd;
                }
            }
        }

        SetSparkleVisible(true);
        float min = Mathf.Max(0.1f, Mathf.Min(sparkleIntervalRange.x, sparkleIntervalRange.y));
        float max = Mathf.Max(min, Mathf.Max(sparkleIntervalRange.x, sparkleIntervalRange.y));
        nextSparkleTime = Time.time + Random.Range(min, max);
    }

    private Bounds ComputeSparkleBounds()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            Bounds b = collider2D.bounds;
            b.Expand(new Vector3(-Mathf.Min(0.1f, b.size.x * 0.25f), -Mathf.Min(0.1f, b.size.y * 0.25f), 0f));
            return b;
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one * 0.5f);
        }

        bool hasBounds = false;
        Bounds combined = new Bounds(transform.position, Vector3.one * 0.5f);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null || sr.sprite == null || !sr.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combined = sr.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(sr.bounds);
            }
        }

        return hasBounds ? combined : new Bounds(transform.position, Vector3.one);
    }

    private void SetSparkleVisible(bool visible)
    {
        if (sparkleRenderer != null)
        {
            sparkleRenderer.enabled = visible;
        }
        if (sparkleDiamondRenderer != null)
        {
            sparkleDiamondRenderer.enabled = visible;
        }
        if (companionRenderers != null)
        {
            for (int i = 0; i < companionRenderers.Length; i++)
            {
                if (companionRenderers[i] != null)
                {
                    companionRenderers[i].enabled = visible;
                }
                if (companionDiamondRenderers != null && companionDiamondRenderers[i] != null)
                {
                    companionDiamondRenderers[i].enabled = visible;
                }
            }
        }
    }

    private void SetSparkleRootWorldScale(GameObject root, float desiredWorldScale)
    {
        if (root == null)
        {
            return;
        }

        Vector3 parentLossy = transform.lossyScale;
        float x = SafeDivide(desiredWorldScale, parentLossy.x);
        float y = SafeDivide(desiredWorldScale, parentLossy.y);
        root.transform.localScale = new Vector3(x, y, 1f);
    }

    private static float SafeDivide(float value, float by)
    {
        if (Mathf.Abs(by) < 0.0001f)
        {
            return value;
        }

        return value / by;
    }

    private static Sprite GetSparkleSprite()
    {
        if (cachedSparkleSprite != null)
        {
            return cachedSparkleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int center = size / 2;
        float maxRadius = size * 0.28f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                float radial = Mathf.Sqrt(dx * dx + dy * dy);

                bool diamond = manhattan <= maxRadius * 1.45f;
                bool verticalArm = Mathf.Abs(dx) <= 2f && Mathf.Abs(dy) <= maxRadius * 1.25f;
                bool horizontalArm = Mathf.Abs(dy) <= 2f && Mathf.Abs(dx) <= maxRadius * 1.25f;
                bool diagonalArmA = Mathf.Abs(dx - dy) <= 2f && radial <= maxRadius * 1.1f;
                bool diagonalArmB = Mathf.Abs(dx + dy) <= 2f && radial <= maxRadius * 1.1f;
                bool brightCore = radial <= maxRadius * 0.2f;

                bool on = diamond || verticalArm || horizontalArm || diagonalArmA || diagonalArmB || brightCore;
                texture.SetPixel(x, y, on ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        cachedSparkleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
        return cachedSparkleSprite;
    }

    private void OnDisable()
    {
        SetSparkleVisible(false);
    }
}
