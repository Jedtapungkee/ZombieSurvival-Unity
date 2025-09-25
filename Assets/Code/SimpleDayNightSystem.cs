using UnityEngine;

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
    
    private float currentTime = 0f;
    private float timeSpeed;
    private bool isDawn = false;
    
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
        // อัปเดตเวลา
        currentTime += timeSpeed * Time.deltaTime;
        
        // ตรวจสอบว่าถึงเวลาเช้าหรือยัง
        if (!isDawn && currentTime >= dawnStartPercent)
        {
            isDawn = true;
            Debug.Log("🌅 เริ่มเข้าสู่ช่วงเช้า!");
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
    
    public float GetRemainingMinutes()
    {
        return (1f - currentTime) / timeSpeed / 60f;
    }
}