using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if TMP_PRESENT
using TMPro;
#endif

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Bindings")]
    public Health playerHealth;     // อ้างอิง Health ของผู้เล่น
    public Slider healthSlider;     // UI Slider แทนแถบเลือด
#if TMP_PRESENT
    public TextMeshProUGUI hpText;  // (ทางเลือก) แสดงตัวเลข HP
#else
    public Text hpText;             // (ถ้าใช้ legacy Text)
#endif

    [Header("Options")]
    public bool showNumeric = true;

    [Header("Auto-Bind Player")] 
    public bool autoBindByTag = true;      // ให้สคริปต์หา Player อัตโนมัติด้วย Tag
    public string playerTag = "Player";    // Tag ของ Player
    [Range(0.25f, 5f)] public float rebindInterval = 1f; // ความถี่ในการลองหาใหม่เมื่อยังไม่เจอ

    [Header("Visuals")]
    public Image fillImage;                     // สีของแถบ (จะหาอัตโนมัติจาก Slider ถ้าไม่ระบุ)
    public Color fullColor = new Color(0.2f, 1f, 0.2f);   // เขียว
    public Color midColor = new Color(1f, 0.9f, 0.2f);    // เหลือง
    public Color emptyColor = new Color(1f, 0.2f, 0.2f);  // แดง

    [Header("Smoothing")]
    public bool smoothBar = true;               // เปิด/ปิดแอนิเมชันลด HP แบบนิ่มๆ
    [Range(1f, 20f)] public float smoothSpeed = 10f; // ยิ่งมากยิ่งไว
    public bool numericFollowsSmooth = true;    // ตัวเลขแสดงตามแถบลื่นไหลหรือแสดงค่าจริงทันที

    private float targetValue;   // ค่า HP เป้าหมายที่แท้จริง
    private float displayedValue; // ค่า HP ที่แสดงบนแถบ (สำหรับ smoothing)
    private Coroutine rebindRoutine;

    [Header("Damage Feedback")]
    public Image damageOverlay;                 // ภาพทับสีแดงทั้งหน้าจอ/บริเวณ HUD (ควรเป็น Image สีแดงโปร่งใส)
    [Range(0f,1f)] public float flashMaxAlpha = 0.35f;
    public float flashFadeOutTime = 0.25f;
    public RectTransform barToPulse;            // object ที่จะ pulse (มักเป็น Slider.transform หรือ Fill)
    public float pulseScale = 1.08f;
    public float pulseTime = 0.15f;
    private float damageOverlayAlpha;
    private Vector3 barBaseScale;

    void Awake()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
        }

        // หา Fill Image อัตโนมัติถ้าไม่ได้กำหนดมา
        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }

        if (barToPulse == null && healthSlider != null)
            barToPulse = healthSlider.transform as RectTransform;
        if (barToPulse != null) barBaseScale = barToPulse.localScale;
        if (damageOverlay != null)
        {
            var c = damageOverlay.color; c.a = 0f; damageOverlay.color = c;
            damageOverlay.raycastTarget = false; // ไม่ให้บังปุ่ม/คลิกของ UI อื่น
        }
    }

    void OnEnable()
    {
        if (playerHealth != null) Subscribe();
        else if (autoBindByTag) StartRebind();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Refresh();
    }

    void OnDisable()
    {
        StopRebind();
        Unsubscribe();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (playerHealth == null && autoBindByTag)
        {
            TryBindPlayerByTag();
        }
    }

    private void Subscribe()
    {
        if (playerHealth == null) return;
        playerHealth.Damaged += OnDamaged;
        playerHealth.Healed += OnHealed;
        playerHealth.Died += OnDied;
    }

    private void Unsubscribe()
    {
        if (playerHealth == null) return;
        playerHealth.Damaged -= OnDamaged;
        playerHealth.Healed -= OnHealed;
        playerHealth.Died -= OnDied;
    }

    private void StartRebind()
    {
        StopRebind();
        rebindRoutine = StartCoroutine(RebindLoop());
    }

    private void StopRebind()
    {
        if (rebindRoutine != null)
        {
            StopCoroutine(rebindRoutine);
            rebindRoutine = null;
        }
    }

    private System.Collections.IEnumerator RebindLoop()
    {
        while (playerHealth == null)
        {
            TryBindPlayerByTag();
            if (playerHealth != null) yield break;
            yield return new WaitForSeconds(rebindInterval);
        }
    }

    private void TryBindPlayerByTag()
    {
        if (!autoBindByTag) return;
        try
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                var hp = go.GetComponent<Health>();
                if (hp != null)
                {
                    playerHealth = hp;
                    Subscribe();
                    Refresh();
                }
            }
        }
        catch { /* ถ้า Tag ไม่มีหรือยังไม่พร้อม ให้ลองใหม่ภายหลัง */ }
    }

    private void OnDamaged(int dmg, int current)
    {
        Refresh();
        PlayDamageFeedback();
    }

    private void OnHealed()
    {
        Refresh();
    }

    private void OnDied()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (playerHealth == null) return;
        if (healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.maxHealth;
            targetValue = Mathf.Clamp(playerHealth.CurrentHealth, 0, playerHealth.maxHealth);
            if (!smoothBar)
            {
                displayedValue = targetValue;
                healthSlider.value = displayedValue;
            }
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (playerHealth == null || healthSlider == null) return;
        if (smoothBar)
        {
            // Lerp แบบนิ่มๆ เข้าหาเป้าหมาย
            displayedValue = Mathf.Lerp(displayedValue, targetValue, Time.deltaTime * smoothSpeed);
            // ป้องกันอาการค้างใกล้ค่าเป้าหมายด้วยการ Snap เมื่อใกล้พอ
            if (Mathf.Abs(displayedValue - targetValue) < 0.01f)
                displayedValue = targetValue;
            healthSlider.value = displayedValue;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (playerHealth == null) return;

        // อัปเดตสีตามสัดส่วน HP
        float max = Mathf.Max(1, playerHealth.maxHealth);
        float val = smoothBar ? displayedValue : (healthSlider != null ? healthSlider.value : playerHealth.CurrentHealth);
        float t = Mathf.Clamp01(val / max);

        if (fillImage != null)
        {
            // เขียว (1) -> เหลือง (0.5) -> แดง (0)
            Color c = t >= 0.5f
                ? Color.Lerp(midColor, fullColor, (t - 0.5f) / 0.5f)
                : Color.Lerp(emptyColor, midColor, t / 0.5f);
            fillImage.color = c;
        }

        // อัปเดตตัวเลข
        if (hpText != null)
        {
            if (showNumeric)
            {
                int shown = numericFollowsSmooth ? Mathf.RoundToInt(val) : playerHealth.CurrentHealth;
                hpText.text = $"HP: {shown}/{playerHealth.maxHealth}";
            }
            else
            {
                hpText.text = string.Empty;
            }
        }
    }

    private void PlayDamageFeedback()
    {
        // Flash overlay
        if (damageOverlay != null)
        {
            StopCoroutineSafely(nameof(FadeOverlayRoutine));
            StartCoroutine(FadeOverlayRoutine());
        }

        // Pulse scale
        if (barToPulse != null)
        {
            StopCoroutineSafely(nameof(PulseRoutine));
            StartCoroutine(PulseRoutine());
        }
    }

    private System.Collections.IEnumerator FadeOverlayRoutine()
    {
        // Snap to max alpha
        SetOverlayAlpha(flashMaxAlpha);
        // Fade out
        float t = 0f;
        while (t < flashFadeOutTime)
        {
            t += Time.unscaledDeltaTime; // ไม่ให้เร็วขึ้นกับเกมสปีด
            float a = Mathf.Lerp(flashMaxAlpha, 0f, t / flashFadeOutTime);
            SetOverlayAlpha(a);
            yield return null;
        }
        SetOverlayAlpha(0f);
    }

    private void SetOverlayAlpha(float a)
    {
        if (damageOverlay == null) return;
        var c = damageOverlay.color; c.a = a; damageOverlay.color = c;
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        if (barToPulse == null) yield break;
        Vector3 start = barBaseScale;
        Vector3 end = barBaseScale * pulseScale;
        float half = Mathf.Max(0.01f, pulseTime * 0.5f);
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            barToPulse.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            barToPulse.localScale = Vector3.Lerp(end, start, k);
            yield return null;
        }
        barToPulse.localScale = start;
    }

    private void StopCoroutineSafely(string routineName)
    {
        var c = GetType().GetMethod(routineName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        // No direct reference; try to stop by name if exists
        if (c != null)
        {
            try { StopCoroutine(routineName); } catch {}
        }
    }
}
