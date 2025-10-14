using UnityEngine;
using System;

public class SimpleDayNight : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Light sunLight; // ลาก Directional Light เข้ามา
    
    [Header("Time Settings")]
    public float totalGameTimeMinutes = 10f; // 10 นาที
    public float dawnStartPercent = 0.8f; // เริ่มสว่างที่ 80% ของเวลา (นาทีที่ 8)
    
    [Header("Night Settings (0-80% of game time)")]
    public Color nightLightColor = new Color(0.1f, 0.1f, 0.3f); // สีน้ำเงินเข้ม
    public float nightLightIntensity = 0.1f;
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);
    
    [Header("Dawn Settings (80-100% of game time)")]
    public Color dawnLightColor = new Color(1f, 0.8f, 0.6f); // สีส้มอ่อน
    public float dawnLightIntensity = 1.2f;
    public Color dawnAmbientColor = new Color(0.3f, 0.3f, 0.4f);
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    [Header("Time Over")]
    [Tooltip("เปิดใช้งานระบบ Game Over เมื่อหมดเวลา")]
    public bool enableTimeOver = true;
    [Tooltip("ข้อความแสดงเมื่อหมดเวลา")]
    public string timeOverMessage = "Time's Up!";
    [Tooltip("ฆ่า Player เมื่อหมดเวลา")]
    public bool killPlayerOnTimeOver = true;
    [Tooltip("Tag ของ Player")]
    public string playerTag = "Player";
    
    // Events
    public static event Action OnTimeOver; // Event เมื่อหมดเวลา
    public static event Action OnDawnStart; // Event เมื่อเริ่มเช้า
    
    private float currentTime = 0f;
    private float timeSpeed;
    private bool isDawn = false;
    private bool isTimeOver = false; // ป้องกันการเรียก Time Over หลายครั้ง
    
    void Start()
    {
        // คำนวณความเร็วเวลา (จาก 0 ถึง 1 ใน 10 นาที)
        timeSpeed = 1f / (totalGameTimeMinutes * 60f);
        
        // ตั้งค่าเริ่มต้นเป็นกลางคืน
        SetNightLighting();
        
        Debug.Log($"🌙 เริ่มเกมในโหมดกลางคืน - เวลารวม {totalGameTimeMinutes} นาที");
    }
    
    void Update()
    {
        // ถ้าเวลาหมดแล้วให้หยุดอัปเดต
        if (isTimeOver) return;
        
        // อัปเดตเวลา
        currentTime += timeSpeed * Time.deltaTime;
        
        // ตรวจสอบว่าหมดเวลาหรือยัง
        if (enableTimeOver && currentTime >= 1f)
        {
            TriggerTimeOver();
            return; // หยุดการอัปเดตเมื่อหมดเวลา
        }
        
        // ตรวจสอบว่าถึงเวลาเช้าหรือยัง
        if (!isDawn && currentTime >= dawnStartPercent)
        {
            isDawn = true;
            Debug.Log("🌅 เริ่มเข้าสู่ช่วงเช้า!");
            OnDawnStart?.Invoke(); // ส่ง Event
        }
        
        // อัปเดตแสงไฟ
        UpdateLighting();
        
        // แสดงข้อมูล Debug
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    void UpdateLighting()
    {
        if (sunLight == null) return;
        
        if (!isDawn)
        {
            // ช่วงกลางคืน (0% - 80%)
            SetNightLighting();
        }
        else
        {
            // ช่วงเช้า (80% - 100%) - ค่อยๆ เปลี่ยน
            float dawnProgress = (currentTime - dawnStartPercent) / (1f - dawnStartPercent);
            dawnProgress = Mathf.Clamp01(dawnProgress);
            
            // เปลี่ยนแสงค่อยๆ จากกลางคืนไปเช้า
            Color currentLightColor = Color.Lerp(nightLightColor, dawnLightColor, dawnProgress);
            float currentIntensity = Mathf.Lerp(nightLightIntensity, dawnLightIntensity, dawnProgress);
            Color currentAmbient = Color.Lerp(nightAmbientColor, dawnAmbientColor, dawnProgress);
            
            // ตั้งค่าแสง
            sunLight.color = currentLightColor;
            sunLight.intensity = currentIntensity;
            RenderSettings.ambientLight = currentAmbient;
            
            // หมุนแสงแดดค่อยๆ ขึ้น
            float sunAngle = Mathf.Lerp(-45f, 15f, dawnProgress); // จากใต้ขอบฟ้าขึ้นมา
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0f);
        }
    }
    
    void SetNightLighting()
    {
        if (sunLight == null) return;
        
        sunLight.color = nightLightColor;
        sunLight.intensity = nightLightIntensity;
        RenderSettings.ambientLight = nightAmbientColor;
        
        // ตั้งแสงแดดให้อยู่ใต้ขอบฟ้า
        sunLight.transform.rotation = Quaternion.Euler(-45f, 30f, 0f);
    }
    
    void ShowDebugInfo()
    {
        float remainingMinutes = (1f - currentTime) / timeSpeed / 60f;
        float progressPercent = currentTime * 100f;
        
        Debug.Log($"⏰ เวลา: {progressPercent:F1}% | เหลือ: {remainingMinutes:F1} นาที | สถานะ: {(isDawn ? "🌅 เช้า" : "🌙 กลางคืน")}");
    }
    
    void TriggerTimeOver()
    {
        if (isTimeOver) return; // ป้องกันการเรียกซ้ำ
        
        isTimeOver = true;
        currentTime = 1f; // จำกัดเวลาไม่ให้เกิน 100%
        
        Debug.Log($"⏰ {timeOverMessage} - Game Over!");
        
        // ฆ่า Player เมื่อหมดเวลา (ถ้าเปิดใช้งาน)
        if (killPlayerOnTimeOver)
        {
            KillPlayer();
        }
        
        // ส่ง Event ให้ระบบอื่นๆ รับทราบ
        OnTimeOver?.Invoke();
    }
    
    void KillPlayer()
    {
        try
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // ฆ่า Player โดยให้ damage มากๆ
                    playerHealth.TakeDamage(9999);
                    Debug.Log($"[SimpleDayNight] Player killed due to time over!");
                }
                else
                {
                    Debug.LogWarning($"[SimpleDayNight] Player found but no Health component!");
                }
            }
            else
            {
                Debug.LogWarning($"[SimpleDayNight] No GameObject with tag '{playerTag}' found!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleDayNight] Error killing player: {e.Message}");
        }
    }
    
    // Public Methods สำหรับระบบอื่นๆ
    public float GetTimeProgress()
    {
        return currentTime;
    }
    
    public bool IsNightTime()
    {
        return !isDawn;
    }
    
    public bool IsDawnTime()
    {
        return isDawn;
    }
    
    public bool IsTimeOver()
    {
        return isTimeOver;
    }
    
    public float GetRemainingMinutes()
    {
        return (1f - currentTime) / timeSpeed / 60f;
    }
    
    public float GetRemainingSeconds()
    {
        return (1f - currentTime) / timeSpeed;
    }
    
    // Methods สำหรับการควบคุมจากภายนอก
    
    /// <summary>
    /// หยุดระบบเวลา (เช่น เมื่อผู้เล่นชนะ)
    /// </summary>
    public void StopTime()
    {
        enabled = false;
        Debug.Log("[SimpleDayNight] Time system stopped");
    }
    
    /// <summary>
    /// เริ่มระบบเวลาใหม่
    /// </summary>
    public void StartTime()
    {
        enabled = true;
        Debug.Log("[SimpleDayNight] Time system started");
    }
    
    /// <summary>
    /// รีเซ็ตเวลากลับไปเริ่มต้น
    /// </summary>
    public void ResetTime()
    {
        currentTime = 0f;
        isDawn = false;
        isTimeOver = false;
        SetNightLighting();
        enabled = true;
        Debug.Log("[SimpleDayNight] Time system reset");
    }
}