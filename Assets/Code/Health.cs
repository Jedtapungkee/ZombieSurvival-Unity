using System;
using UnityEngine;

/// <summary>
/// ระบบ HP สำหรับตัวละคร - จัดการพลังชีวิต, การรับดาเมจ, การฟื้นพลังชีวิต, และการตาย
/// ใช้ Events เพื่อแจ้งเตือน Script อื่นเมื่อมีการเปลี่ยนแปลง
/// </summary>
public class Health : MonoBehaviour
{
    [Min(1)] public int maxHealth = 100;                    // พลังชีวิตเต็ม
    [SerializeField] private bool destroyOnDeath = false;   // ทำลาย GameObject เมื่อตายหรือไม่
    [Header("Debug")] public bool debugLog;                 // แสดง Log สำหรับ Debug

    public int CurrentHealth { get; private set; }  // พลังชีวิตปัจจุบัน (อ่านได้อย่างเดียว)
    public bool IsDead => CurrentHealth <= 0;       // ตรวจสอบว่าตายแล้วหรือยัง

    // Events - สามารถ Subscribe จาก Script อื่นได้
    public event Action<int, int> Damaged;   // (damage ที่ได้รับ, HP ปัจจุบัน)
    public event Action Healed;              // เมื่อฟื้น HP
    public event Action Died;                // เมื่อตาย

    /// <summary>
    /// เรียกตอนเริ่มต้น - ตั้งค่า HP เป็นค่าเต็ม
    /// </summary>
    void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (debugLog) Debug.Log($"[Health] Awake {name} HP={CurrentHealth}/{maxHealth}", this);
    }

    /// <summary>
    /// รีเซ็ต HP กลับเป็นค่าเต็ม (เช่น เมื่อเริ่มเกมใหม่)
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    /// <summary>
    /// รับความเสียหาย - ลด HP และตรวจสอบว่าตายหรือไม่
    /// </summary>
    /// <param name="damage">จำนวนความเสียหายที่ได้รับ</param>
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;  // ถ้าตายแล้ว หรือ damage ไม่มี ไม่ต้องทำอะไร
        
        int before = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);  // ลด HP (ไม่ต่ำกว่า 0)
        
        if (debugLog) 
            Debug.Log($"[Health] {name} took {damage}. {before}->{CurrentHealth}", this);
        
        // แจ้ง Event Damaged (สำหรับ UI หรือ Script อื่นๆ)
        Damaged?.Invoke(damage, CurrentHealth);

        // ถ้า HP เหลือ 0 -> ตาย
        if (CurrentHealth <= 0)
        {
            if (debugLog) Debug.Log($"[Health] {name} died.", this);
            
            Died?.Invoke();  // แจ้ง Event Died
            
            // ทำลาย GameObject ถ้าเปิด destroyOnDeath
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// ฟื้นพลังชีวิต - เพิ่ม HP (ไม่เกินค่าเต็ม)
    /// </summary>
    /// <param name="amount">จำนวน HP ที่จะเพิ่ม</param>
    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;  // ไม่สามารถฟื้นคนตายได้
        
        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);  // เพิ่ม HP (ไม่เกินค่าเต็ม)
        
        if (debugLog) 
            Debug.Log($"[Health] {name} healed {amount}. {before}->{CurrentHealth}", this);
        
        Healed?.Invoke();  // แจ้ง Event Healed
    }
}
