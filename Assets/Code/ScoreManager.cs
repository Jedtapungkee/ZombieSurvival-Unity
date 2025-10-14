using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    // Singleton Pattern
    public static ScoreManager Instance { get; private set; }
    
    [Header("Score Settings")]
    [Tooltip("คะแนนที่ได้จากการสังหารซอมบี้ 1 ตัว")]
    public int zombieKillScore = 10;
    
    [Header("Debug")]
    public bool debugLog = true;
    
    // คะแนนปัจจุบัน
    private int currentScore = 0;
    // คะแนนสุดท้ายก่อน reset (สำหรับแสดงใน Game Over)
    private int finalScore = 0;
    
    // Properties
    public int CurrentScore => currentScore;
    public int FinalScore => finalScore;
    
    // Events สำหรับแจ้งเตือนเมื่อคะแนนเปลี่ยน
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnZombieKilled; // Event เฉพาะการฆ่าซอมบี้
    public static event Action<int> OnGameOver; // Event เมื่อเกมจบพร้อมคะแนนสุดท้าย
    
    void Awake()
    {
        // Singleton Setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (debugLog) Debug.Log("[ScoreManager] Initialized", this);
        }
        else
        {
            if (debugLog) Debug.LogWarning("[ScoreManager] Duplicate instance destroyed", this);
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // เริ่มต้นด้วยคะแนน 0
        ResetScore();
    }
    
    /// <summary>
    /// เพิ่มคะแนนทั่วไป
    /// </summary>
    /// <param name="points">จำนวนคะแนนที่เพิ่ม</param>
    public void AddScore(int points)
    {
        if (points <= 0) return;
        
        int oldScore = currentScore;
        currentScore += points;
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Score +{points}: {oldScore} → {currentScore}", this);
        
        // ส่ง Event
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// เพิ่มคะแนนจากการฆ่าซอมบี้
    /// </summary>
    public void AddZombieKillScore()
    {
        AddScore(zombieKillScore);
        
        // ส่ง Event เฉพาะการฆ่าซอมบี้
        OnZombieKilled?.Invoke(zombieKillScore);
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Zombie killed! +{zombieKillScore} points", this);
    }
    
    /// <summary>
    /// เรียกเมื่อ Player ตาย - เก็บคะแนนสุดท้ายแล้ว reset
    /// </summary>
    public void OnPlayerDied()
    {
        // เก็บคะแนนสุดท้าย
        finalScore = currentScore;
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Player died! Final Score: {finalScore}", this);
        
        // ส่ง Event Game Over พร้อมคะแนนสุดท้าย
        OnGameOver?.Invoke(finalScore);
        
        // Reset คะแนนปัจจุบัน
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// รีเซ็ตคะแนนเป็น 0
    /// </summary>
    public void ResetScore()
    {
        int oldScore = currentScore;
        currentScore = 0;
        finalScore = 0; // reset คะแนนสุดท้ายด้วย
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Score reset: {oldScore} → 0", this);
        
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// ตั้งคะแนนเป็นค่าที่กำหนด (สำหรับการโหลดเซฟ)
    /// </summary>
    /// <param name="score">คะแนนที่ต้องการตั้ง</param>
    public void SetScore(int score)
    {
        int oldScore = currentScore;
        currentScore = Mathf.Max(0, score); // ป้องกันคะแนนติดลบ
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Score set: {oldScore} → {currentScore}", this);
        
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// ลบคะแนน (ถ้าต้องการระบบลงโทษ)
    /// </summary>
    /// <param name="points">จำนวนคะแนนที่ลบ</param>
    public void SubtractScore(int points)
    {
        if (points <= 0) return;
        
        int oldScore = currentScore;
        currentScore = Mathf.Max(0, currentScore - points); // ป้องกันคะแนนติดลบ
        
        if (debugLog) 
            Debug.Log($"[ScoreManager] Score -{points}: {oldScore} → {currentScore}", this);
        
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// ดูคะแนนปัจจุบัน (สำหรับ Debug หรือ UI)
    /// </summary>
    /// <returns>คะแนนปัจจุบัน</returns>
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    void OnDestroy()
    {
        // ทำความสะอาด Event เมื่อ Object ถูกทำลาย
        if (Instance == this)
        {
            OnScoreChanged = null;
            OnZombieKilled = null;
            OnGameOver = null;
        }
    }
    
    // สำหรับ Debug ใน Inspector
    void OnValidate()
    {
        if (zombieKillScore < 0)
            zombieKillScore = 0;
    }
}