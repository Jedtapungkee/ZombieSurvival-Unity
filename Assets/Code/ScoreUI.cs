using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class ScoreUI : MonoBehaviour
{
    [Header("UI References")]
#if TMP_PRESENT
    [Tooltip("TextMeshPro component สำหรับแสดงคะแนน (แนะนำ)")]
    public TextMeshProUGUI scoreTextTMP;
#endif
    [Tooltip("Text component สำหรับแสดงคะแนน (สำรอง)")]
    public Text scoreTextLegacy;

    [Header("Display Settings")]
    [Tooltip("ข้อความที่แสดงหน้าคะแนน")]
    public string scorePrefix = "Score: ";
    [Tooltip("รูปแบบการแสดงตัวเลข (N0 = มีจุลภาค, 0 = ไม่มี)")]
    public string numberFormat = "N0";
    
    [Header("Animation (Optional)")]
    [Tooltip("เล่นแอนิเมชันเมื่อคะแนนเปลี่ยน")]
    public bool animateOnChange = true;
    [Tooltip("เวลาในการเล่นแอนิเมชัน")]
    public float animationDuration = 0.3f;
    [Tooltip("สีเมื่อได้คะแนน")]
    public Color highlightColor = Color.yellow;
    
    [Header("Auto Setup")]
    [Tooltip("หา ScoreManager อัตโนมัติเมื่อเริ่มเกม")]
    public bool autoFindScoreManager = true;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Private variables
    private Color originalColor;
    private bool isInitialized = false;
    private bool isUIVisible = true; // เก็บสถานะการแสดงผล
    
    void Start()
    {
        InitializeUI();
        
        if (autoFindScoreManager)
        {
            ConnectToScoreManager();
        }
    }
    
    void InitializeUI()
    {
        // หา Text component ถ้าไม่ได้กำหนด
        if (GetActiveTextComponent() == null)
        {
            AttemptAutoFindTextComponent();
        }
        
        // เก็บสีเดิม
        var textComp = GetActiveTextComponent();
        if (textComp != null)
        {
            originalColor = GetTextColor();
            if (debugLog) Debug.Log($"[ScoreUI] UI initialized with text component: {textComp.GetType().Name}", this);
        }
        else
        {
            Debug.LogError("[ScoreUI] No Text component found! Please assign scoreTextTMP or scoreTextLegacy.", this);
            return;
        }
        
        isInitialized = true;
        
        // แสดงคะแนนเริ่มต้น
        UpdateScoreDisplay(0);
    }
    
    void AttemptAutoFindTextComponent()
    {
#if TMP_PRESENT
        if (scoreTextTMP == null)
            scoreTextTMP = GetComponent<TextMeshProUGUI>();
#endif
        
        if (scoreTextLegacy == null)
            scoreTextLegacy = GetComponent<Text>();
            
        if (debugLog) Debug.Log("[ScoreUI] Auto-found text components", this);
    }
    
    void ConnectToScoreManager()
    {
        // Subscribe to ScoreManager events
        ScoreManager.OnScoreChanged += OnScoreChanged;
        ScoreManager.OnZombieKilled += OnZombieKilled;
        
        // อัปเดตคะแนนปัจจุบัน (ถ้า ScoreManager มีอยู่แล้ว)
        if (ScoreManager.Instance != null)
        {
            UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
            if (debugLog) Debug.Log("[ScoreUI] Connected to ScoreManager", this);
        }
        else
        {
            if (debugLog) Debug.LogWarning("[ScoreUI] ScoreManager not found yet", this);
        }
    }
    
    private void OnScoreChanged(int newScore)
    {
        if (!isInitialized) return;
        
        UpdateScoreDisplay(newScore);
        
        if (animateOnChange)
        {
            PlayScoreChangeAnimation();
        }
        
        if (debugLog) Debug.Log($"[ScoreUI] Score updated to: {newScore}", this);
    }
    
    private void OnZombieKilled(int points)
    {
        if (debugLog) Debug.Log($"[ScoreUI] Zombie killed! +{points} points", this);
        
        // เล่นเอฟเฟกต์พิเศษสำหรับการฆ่าซอมบี้ (ถ้าต้องการ)
        if (animateOnChange)
        {
            PlayScoreChangeAnimation();
        }
    }
    
    void UpdateScoreDisplay(int score)
    {
        if (!isInitialized || !isUIVisible) return;
        
        string displayText = scorePrefix + score.ToString(numberFormat);
        
#if TMP_PRESENT
        if (scoreTextTMP != null)
        {
            scoreTextTMP.text = displayText;
            return;
        }
#endif
        
        if (scoreTextLegacy != null)
        {
            scoreTextLegacy.text = displayText;
        }
    }
    
    void PlayScoreChangeAnimation()
    {
        if (!animateOnChange || animationDuration <= 0) return;
        
        // ยกเลิกแอนิเมชันเดิม (ถ้ามี)
        StopAllCoroutines();
        
        // เริ่มแอนิเมชันใหม่
        StartCoroutine(ScoreChangeAnimationCoroutine());
    }
    
    System.Collections.IEnumerator ScoreChangeAnimationCoroutine()
    {
        // เปลี่ยนสีเป็น highlight
        SetTextColor(highlightColor);
        
        // รอครึ่งเวลา
        yield return new WaitForSeconds(animationDuration * 0.5f);
        
        // ค่อยๆ เปลี่ยนกลับเป็นสีเดิม
        float elapsed = 0f;
        float halfDuration = animationDuration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            Color currentColor = Color.Lerp(highlightColor, originalColor, t);
            SetTextColor(currentColor);
            
            yield return null;
        }
        
        // ตั้งสีเดิม
        SetTextColor(originalColor);
    }
    
    private Component GetActiveTextComponent()
    {
#if TMP_PRESENT
        if (scoreTextTMP != null) return scoreTextTMP;
#endif
        return scoreTextLegacy;
    }
    
    private Color GetTextColor()
    {
#if TMP_PRESENT
        if (scoreTextTMP != null) return scoreTextTMP.color;
#endif
        if (scoreTextLegacy != null) return scoreTextLegacy.color;
        return Color.white;
    }
    
    private void SetTextColor(Color color)
    {
#if TMP_PRESENT
        if (scoreTextTMP != null)
        {
            scoreTextTMP.color = color;
            return;
        }
#endif
        if (scoreTextLegacy != null)
        {
            scoreTextLegacy.color = color;
        }
    }
    
    // Public methods สำหรับควบคุมจากภายนอก
    
    /// <summary>
    /// ซ่อน UI Score
    /// </summary>
    public void HideUI()
    {
        isUIVisible = false;
        var textComp = GetActiveTextComponent();
        if (textComp != null)
        {
            ((MonoBehaviour)textComp).gameObject.SetActive(false);
        }
        
        if (debugLog) Debug.Log("[ScoreUI] UI Hidden", this);
    }
    
    /// <summary>
    /// แสดง UI Score
    /// </summary>
    public void ShowUI()
    {
        isUIVisible = true;
        var textComp = GetActiveTextComponent();
        if (textComp != null)
        {
            ((MonoBehaviour)textComp).gameObject.SetActive(true);
        }
        
        if (debugLog) Debug.Log("[ScoreUI] UI Shown", this);
    }
    
    /// <summary>
    /// ตรวจสอบว่า UI แสดงอยู่หรือไม่
    /// </summary>
    /// <returns>true ถ้าแสดงอยู่</returns>
    public bool IsUIVisible()
    {
        return isUIVisible;
    }
    
    /// <summary>
    /// เชื่อมต่อกับ ScoreManager ด้วยตนเอง
    /// </summary>
    public void ManualConnectToScoreManager()
    {
        ConnectToScoreManager();
    }
    
    /// <summary>
    /// อัปเดตการแสดงผลด้วยตนเอง
    /// </summary>
    /// <param name="score">คะแนนที่ต้องการแสดง</param>
    public void ManualUpdateScore(int score)
    {
        UpdateScoreDisplay(score);
    }
    
    void OnDestroy()
    {
        // Unsubscribe จาก events เพื่อป้องกัน memory leak
        ScoreManager.OnScoreChanged -= OnScoreChanged;
        ScoreManager.OnZombieKilled -= OnZombieKilled;
    }
    
    // สำหรับ Debug ใน Inspector
    void OnValidate()
    {
        if (animationDuration < 0) animationDuration = 0;
    }
}