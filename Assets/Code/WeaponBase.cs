using UnityEngine;

/// <summary>
/// Base Class สำหรับอาวุธทั้งหมด (ปืน, ดาบ, ฯลฯ)
/// กำหนดคุณสมบัติพื้นฐานและ Methods ที่ต้อง Implement
/// Class นี้เป็น Abstract ไม่สามารถสร้าง Instance ได้โดยตรง
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Common")]
    public string displayName = "Weapon";    // ชื่ออาวุธ (แสดงใน UI)
    public float attackCooldown = 0.5f;      // ช่วงเวลาระหว่างการโจมตี (วินาที)
    protected float lastAttack;              // เวลาที่โจมตีครั้งล่าสุด

    [Header("Grip Offset (Local to Hand)")]
    public Vector3 gripLocalPosition = Vector3.zero;  // ตำแหน่งจับอาวุธ (Local to Hand)
    public Vector3 gripLocalEuler = Vector3.zero;     // มุมของอาวุธเมื่อจับ (Local Rotation)

    [Header("Debug")]
    public bool debugLogHits = false;  // แสดง Debug Log เมื่อโจมตีโดน
    
    /// <summary>
    /// เรียกเมื่อติดอาวุธเข้ากับมือ - ต้อง Override ใน Class ลูก
    /// </summary>
    public abstract void OnEquip(Transform hand);

    /// <summary>
    /// เรียกเมื่อถอดอาวุธออก - ต้อง Override ใน Class ลูก
    /// </summary>
    public abstract void OnUnequip();

    /// <summary>
    /// พยายามโจมตี - ตรวจสอบ Cooldown และดำเนินการโจมตี
    /// </summary>
    /// <param name="owner">ผู้ที่ถืออาวุธ (Player/Enemy)</param>
    /// <returns>true = โจมตีสำเร็จ, false = ยังโจมตีไม่ได้</returns>
    public abstract bool TryAttack(Transform owner);
}
