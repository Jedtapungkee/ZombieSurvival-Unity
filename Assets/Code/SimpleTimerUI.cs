using UnityEngine;
using UnityEngine.UI;

public class SimpleTimerUI : MonoBehaviour
{
    [Header("UI Reference")]
    public Text timerText; // ลาก UI Text เข้ามา
    
    [Header("Color Settings")]
    public Color normalColor = Color.white; // สีปกติ
    public Color warningColor = Color.red;  // สีเตือน
    public float warningTimeMinutes = 2f;   // เริ่มเป็นสีแดงเมื่อเหลือ 2 นาที
    
    [Header("Display Settings")]
    public bool showMinutesAndSeconds = true; // แสดงทั้งนาทีและวินาที
    public int fontSize = 24;
    public FontStyle fontStyle = FontStyle.Bold;
    
    // Private variables
    private SimpleDayNight dayNightSystem;
    private float lastRemainingTime = -1f;
    
    void Start()
    {
        InitializeUI();
        FindDayNightSystem();
    }
    
    void InitializeUI()
    {
        if (timerText == null)
        {
            Debug.LogError("❌ กรุณาลาก UI Text ใส่ในช่อง Timer Text!");
            enabled = false;
            return;
        }
        
        // ตั้งค่าฟอนต์
        timerText.fontSize = fontSize;
        timerText.fontStyle = fontStyle;
        timerText.color = normalColor;
        
        // ตั้งค่าตำแหน่งมุมขวาบน
        RectTransform rectTransform = timerText.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // ตั้งค่า Anchor เป็นมุมขวาบน
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);

            // ตั้งตำแหน่งให้ชิดมุมมากขึ้น (ห่างจากขอบ 5 pixels)
            rectTransform.anchoredPosition = new Vector2(-5, -5);
        }
        
        Debug.Log("✅ SimpleTimerUI เตรียมพร้อมแล้ว");
    }
    
    void FindDayNightSystem()
    {
        dayNightSystem = FindFirstObjectByType<SimpleDayNight>();
        
        if (dayNightSystem == null)
        {
            Debug.LogError("❌ ไม่พบ SimpleDayNight ในฉาก! Timer UI จะไม่ทำงาน");
            if (timerText != null)
                timerText.text = "No Timer System";
            enabled = false;
            return;
        }
        
        Debug.Log("✅ เชื่อมต่อกับ SimpleDayNight สำเร็จ");
    }
    
    void Update()
    {
        if (dayNightSystem == null || timerText == null) return;
        
        UpdateTimerDisplay();
        UpdateTimerColor();
    }
    
    void UpdateTimerDisplay()
    {
        float remainingMinutes = dayNightSystem.GetRemainingMinutes();
        
        // อัปเดตทุกเฟรมสำหรับ real-time display
        lastRemainingTime = remainingMinutes;
        
        string timeText;
        
        if (showMinutesAndSeconds)
        {
            // แสดงทั้งนาทีและวินาที
            int minutes = Mathf.FloorToInt(remainingMinutes);
            int seconds = Mathf.FloorToInt((remainingMinutes - minutes) * 60f);
            
            if (minutes > 0)
            {
                timeText = string.Format("{0}:{1:D2}", minutes, seconds);
            }
            else
            {
                // เมื่อเหลือน้อยกว่า 1 นาที แสดงเฉพาะวินาที
                timeText = string.Format("{0}s", seconds);
            }
        }
        else
        {
            // แสดงเฉพาะนาที (ทศนิยม 1 ตำแหน่ง)
            timeText = string.Format("{0:F1}m", remainingMinutes);
        }
        
        timerText.text = timeText;
    }
    
    void UpdateTimerColor()
    {
        float remainingMinutes = dayNightSystem.GetRemainingMinutes();
        
        // อัปเดตสีทุกเฟรมเพื่อให้เปลี่ยนแบบ smooth
        
        if (remainingMinutes <= warningTimeMinutes)
        {
            // คำนวณระดับความเข้มของสีแดง
            float warningIntensity = 1f - (remainingMinutes / warningTimeMinutes);
            warningIntensity = Mathf.Clamp01(warningIntensity);
            
            // ค่อยๆ เปลี่ยนจากสีปกติเป็นสีแดง
            Color currentColor = Color.Lerp(normalColor, warningColor, warningIntensity);
            timerText.color = currentColor;
            
            // เพิ่มเอฟเฟกต์กระพริบเมื่อเวลาใกล้หมดมาก (เหลือน้อยกว่า 30 วินาที)
            if (remainingMinutes <= 0.5f)
            {
                float blinkSpeed = 4f;
                float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed);
                currentColor.a = alpha;
                timerText.color = currentColor;
            }
        }
        else
        {
            // เวลายังเหลือเยอะ ใช้สีปกติ
            timerText.color = normalColor;
        }
    }
    
    // Public methods สำหรับการปรับแต่งจากภายนอก
    public void SetWarningTime(float minutes)
    {
        warningTimeMinutes = minutes;
        Debug.Log($"🔔 เปลี่ยนเวลาเตือนเป็น {minutes} นาที");
    }
    
    public void SetNormalColor(Color color)
    {
        normalColor = color;
    }
    
    public void SetWarningColor(Color color)
    {
        warningColor = color;
    }
    
    public void ToggleMinutesSeconds()
    {
        showMinutesAndSeconds = !showMinutesAndSeconds;
        Debug.Log($"🔄 เปลี่ยนรูปแบบการแสดงเวลา: {(showMinutesAndSeconds ? "นาที:วินาที" : "นาทีเท่านั้น")}");
    }
    
    // Method สำหรับเช็คสถานะ
    public bool IsInWarningMode()
    {
        if (dayNightSystem == null) return false;
        return dayNightSystem.GetRemainingMinutes() <= warningTimeMinutes;
    }
    
    public float GetRemainingTime()
    {
        if (dayNightSystem == null) return 0f;
        return dayNightSystem.GetRemainingMinutes();
    }
}