using UnityEngine;
using UnityEngine.UI;

// Simple floating HP text that appears above an object when it takes damage
[RequireComponent(typeof(Health))]
public class HealthDebugLabel : MonoBehaviour
{
    public GameObject worldSpaceCanvasPrefab; // optional prefab with Text component
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float showSeconds = 1.2f;
    [Tooltip("Optional font to use for the HP label. If not set, a safe builtin will be used.")]
    public Font fallbackFont;
    [Header("Visual Settings")]
    [Tooltip("Overall scale of the world-space canvas. Reduce if the label looks too big in world.")]
    public float worldScale = 0.01f;
    [Tooltip("Pixels per world unit for the world-space canvas. Higher = smaller in world.")]
    public float dynamicPixelsPerUnit = 100f;
    [Tooltip("Size of the label in pixels (WorldSpace)")]
    public Vector2 labelSize = new Vector2(140, 32);
    [Tooltip("Font size for the label text")]
    public int fontSize = 22;
    [Tooltip("Text color")]
    public Color textColor = Color.red;

    private Health hp;
    private Canvas canvas;
    private Text text;
    private float hideAt;

    void Awake()
    {
        hp = GetComponent<Health>();
        hp.Damaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (hp != null) hp.Damaged -= OnDamaged;
    }

    void EnsureUI()
    {
        if (canvas != null) return;
        if (worldSpaceCanvasPrefab != null)
        {
            var go = Instantiate(worldSpaceCanvasPrefab, transform);
            canvas = go.GetComponentInChildren<Canvas>();
            text = go.GetComponentInChildren<Text>();
        }
        else
        {
            var go = new GameObject("HPLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = offset;
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = Mathf.Max(10f, dynamicPixelsPerUnit);
            var tgo = new GameObject("Text");
            tgo.transform.SetParent(go.transform);
            tgo.transform.localPosition = Vector3.zero;
            text = tgo.AddComponent<Text>();
            // Unity 2022+/6000: Arial.ttf is no longer a valid builtin; use LegacyRuntime.ttf or a provided font
            Font useFont = fallbackFont;
            if (useFont == null)
            {
                useFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (useFont == null)
                {
                    // Last resort: use GUI skin font if available
                    useFont = GUI.skin != null ? GUI.skin.font : null;
                }
            }
            if (useFont == null)
            {
                Debug.LogWarning("[HealthDebugLabel] No font available for label. Please assign a Font in the inspector.", this);
            }
            else
            {
                text.font = useFont;
            }
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.fontSize = Mathf.Max(10, fontSize);
            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = labelSize;
            // Apply world scale to the canvas root
            canvas.transform.localScale = Vector3.one * Mathf.Max(0.0001f, worldScale);
        }
        canvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (canvas == null || !canvas.gameObject.activeSelf) return;
        canvas.transform.position = transform.position + offset;
        var cam = Camera.main;
        if (cam) canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.transform.position);
        if (Time.time >= hideAt) canvas.gameObject.SetActive(false);
    }

    private void OnDamaged(int dmg, int current)
    {
        try
        {
            EnsureUI();
            if (text != null) text.text = $"-{dmg} (HP {current})";
            if (canvas != null) canvas.gameObject.SetActive(true);
            hideAt = Time.time + showSeconds;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthDebugLabel] Error showing HP label: {ex.Message}", this);
        }
    }
}
