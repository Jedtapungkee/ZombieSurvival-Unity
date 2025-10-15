using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ระบบการต่อสู้ของผู้เล่น - จัดการการติดอาวุธ, การโจมตี, และ Animation
/// รองรับทั้ง Input System แบบใหม่และแบบเก่า
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Equipment")] 
    public Transform rightHand;        // Transform ของมือขวา (RightHandSocket) สำหรับติดอาวุธ
    public WeaponBase currentWeapon;   // อาวุธที่กำลังถืออยู่ในขณะนี้

    [Header("Input")] 
    public KeyCode fireKey = KeyCode.Mouse0;  // ปุ่มสำหรับโจมตี (แบบเก่า)
    
    [Header("Animation (Optional)")]
    public Animator animator;                                          // Animator Component
    [Tooltip("Trigger when using melee weapon; leave empty to skip")] 
    public string meleeAttackTrigger = "Attack";                       // Animator Trigger สำหรับอาวุธประชิด
    [Tooltip("Trigger when shooting gun; leave empty to skip")] 
    public string shootTrigger = "Shoot";                              // Animator Bool สำหรับยิงปืน
    [Tooltip("Seconds to keep shoot IK lock active after a gun shot.")]
    public float shootIkHoldTime = 0.15f;                              // เวลาที่ใช้ IK หลังยิง (สำหรับมือวางปืน)
    private float shootIkUntil;                                        // เวลาที่จะสิ้นสุด IK
    
    // Property สำหรับคำนวณน้ำหนัก IK (0-1)
    public float ShootIkWeight => shootIkHoldTime <= 0 ? 0f : Mathf.Clamp01((shootIkUntil - Time.time) / shootIkHoldTime);
    
    // สถานะการยิง
    private bool isShooting = false;
    public bool IsShooting => isShooting;  // Property สำหรับให้ Script อื่นเช็คว่ากำลังยิงอยู่หรือไม่

    /// <summary>
    /// เรียกตอนเริ่มต้น - หา RightHandSocket และ Animator อัตโนมัติถ้ายังไม่ได้กำหนด
    /// </summary>
    void Awake()
    {
        // หา RightHandSocket อัตโนมัติถ้ายังไม่ได้ใส่ใน Inspector
        if (rightHand == null)
        {
            // ค้นหา Transform ที่ชื่อ "RightHandSocket" ใน Children ทั้งหมด
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("RightHandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    rightHand = t;
                    break;
                }
            }
        }

        // หา Animator อัตโนมัติถ้ายังไม่ได้ใส่
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (rightHand == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("RightHandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    rightHand = t;
                    break;
                }
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
#endif

    /// <summary>
    /// ติดอาวุธใหม่ - ถอดอาวุธเก่าออกก่อน แล้วสร้างอาวุธใหม่และติดเข้ากับมือ
    /// </summary>
    /// <param name="weaponPrefab">Prefab ของอาวุธที่ต้องการติด</param>
    public void EquipWeapon(GameObject weaponPrefab)
    {
        // ตรวจสอบว่ามี RightHand หรือไม่
        if (rightHand == null)
        {
            Debug.LogWarning("[PlayerCombat] rightHand is not assigned.");
            return;
        }
        
        // ถอดอาวุธเก่าออกก่อน (ถ้ามี)
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }
        
        // สร้างอาวุธใหม่จาก Prefab
        var go = Instantiate(weaponPrefab);
        currentWeapon = go.GetComponent<WeaponBase>();
        
        // ตรวจสอบว่า Prefab มี Component WeaponBase หรือไม่
        if (currentWeapon == null)
        {
            Debug.LogError("[PlayerCombat] Weapon prefab missing WeaponBase-derived component.");
            Destroy(go);
            return;
        }
        
        // ติดอาวุธเข้ากับมือขวา
        currentWeapon.OnEquip(rightHand);
        Debug.Log($"Equipped: {currentWeapon.displayName}");
    }

    /// <summary>
    /// Update ทุกเฟรม - รับ Input และสั่งโจมตี พร้อมกับจัดการ Animation
    /// </summary>
    void Update()
    {
        if (currentWeapon == null) return; // ไม่มีอาวุธ ไม่ต้องทำอะไร
        
        // === ตรวจจับ Input การโจมตี/ยิง ===
        bool fire = false;
#if ENABLE_INPUT_SYSTEM
        // ใช้ Input System แบบใหม่
        if (Mouse.current != null)
        {
            fire = Mouse.current.leftButton.isPressed;
        }
        else if (Gamepad.current != null)
        {
            fire = Gamepad.current.rightTrigger.isPressed || Gamepad.current.leftTrigger.isPressed;
        }
#else
        // ใช้ Input System แบบเก่า
        fire = Input.GetKey(fireKey);
#endif
        
        // อัพเดทสถานะการยิง (สำหรับ WeaponIK หรือ Script อื่นที่ต้องรู้ว่ากำลังยิงอยู่)
        isShooting = fire && (currentWeapon is GunWeapon);
        
        // === ถ้ากดปุ่มโจมตี ===
        if (fire)
        {
            // เรียก TryAttack() จากอาวุธ
            if (currentWeapon.TryAttack(transform))
            {
                // โจมตีสำเร็จ -> เล่น Animation
                if (animator != null)
                {
                    // ถ้าเป็นอาวุธประชิด -> ใช้ Trigger
                    if (currentWeapon is MeleeWeapon)
                    {
                        if (!string.IsNullOrEmpty(meleeAttackTrigger)) 
                            animator.SetTrigger(meleeAttackTrigger);
                    }
                    // ถ้าเป็นปืน -> ใช้ Bool (เพราะยิงต่อเนื่องได้)
                    else if (currentWeapon is GunWeapon)
                    {
                        if (!string.IsNullOrEmpty(shootTrigger)) 
                            animator.SetBool(shootTrigger, true);
                        
                        // ตั้งเวลา IK (เพื่อให้มือยึดปืนแน่นขณะยิง)
                        shootIkUntil = Time.time + shootIkHoldTime;
                    }
                }
            }
        }
        else
        {
            // === ปล่อยปุ่มยิง -> หยุด Animation ===
            if (animator != null && !string.IsNullOrEmpty(shootTrigger))
            {
                animator.SetBool(shootTrigger, false);
            }
        }
    }
}
