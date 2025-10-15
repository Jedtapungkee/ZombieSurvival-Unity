using UnityEngine;

/// <summary>
/// อาวุธประชิด (ดาบ, ค้อน, ฯลฯ) - ใช้ SphereCast และ OverlapCapsule เพื่อตรวจจับเป้าหมาย
/// รองรับ Animation Swing และ Hit Delay เพื่อซิงค์กับ Animation
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Melee")]
    [Min(1)] public int damage = 25;     // ความเสียหายต่อการโจมตี
    public float range = 1.8f;           // ระยะโจมตี (เมตร)
    public float radius = 0.5f;          // รัศมีการโจมตี (ความกว้าง)
    public LayerMask hitMask;            // Layer ที่จะโดน
    [Tooltip("Delay before hit is applied to sync with animation.")]
    public float hitDelay = 0.1f;        // หน่วงเวลาก่อนสร้างดาเมจ (ซิงค์กับ Animation)

    [Header("Procedural Swing (Optional)")]
    public float swingAngle = 30f;       // มุมการแกว่งอาวุธ (องศา)
    public float swingTime = 0.12f;      // เวลาในการแกว่ง (วินาที)
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Curve การแกว่ง
    
    private Transform attach;            // มือที่ติดอาวุธ
    private Quaternion defaultLocalRot;  // มุมเริ่มต้นของอาวุธ
    private bool attacking;              // สถานะกำลังโจมตีอยู่หรือไม่
    
    [Header("Hit Origin (Optional)")]
    public Transform attackOrigin;       // จุดเริ่มต้นการโจมตี (ถ้าไม่ใส่ใช้ตัวอาวุธเอง)
    [Tooltip("If true, cast forward from the player instead of the weapon – useful if weapon forward isn't aligned yet.")]
    public bool useOwnerForward = true;  // ใช้ทิศทางผู้เล่นแทนทิศทางอาวุธ

    /// <summary>
    /// เรียกเมื่อติดอาวุธเข้ากับมือ - ตั้งค่าตำแหน่งและบันทึกมุมเริ่มต้น
    /// </summary>
    public override void OnEquip(Transform hand)
    {
        attach = hand;
        transform.SetParent(hand, worldPositionStays: false);
        transform.localPosition = gripLocalPosition;
        transform.localRotation = Quaternion.Euler(gripLocalEuler);
        defaultLocalRot = transform.localRotation;  // บันทึกมุมเริ่มต้น (สำหรับ Swing Animation)
    }

    /// <summary>
    /// เรียกเมื่อถอดอาวุธออก
    /// </summary>
    public override void OnUnequip()
    {
        transform.SetParent(null);
    }

    /// <summary>
    /// พยายามโจมตี - ตรวจสอบ Cooldown และสถานะ แล้วเริ่ม Coroutine
    /// </summary>
    public override bool TryAttack(Transform owner)
    {
        if (Time.time - lastAttack < attackCooldown) return false;  // ยังไม่ถึงเวลาโจมตีครั้งต่อไป
        if (attacking) return false;  // กำลังโจมตีอยู่แล้ว
        
        lastAttack = Time.time;
        attach = attach == null ? owner : attach;
        if (debugLogHits) Debug.Log("[MeleeWeapon] Attack started");
        
        StartCoroutine(AttackRoutine(owner));
        return true;
    }

    /// <summary>
    /// Coroutine สำหรับการโจมตี - แกว่งอาวุธ, รอ Hit Delay, แล้วตรวจจับและสร้างดาเมจ
    /// </summary>
    private System.Collections.IEnumerator AttackRoutine(Transform owner)
    {
        attacking = true;
        float startTime = Time.time;
        try
        {
            // === Phase 1: เล่น Swing Animation ===
            yield return StartCoroutine(DoSwing());

            // === Phase 2: รอจนถึงจังหวะที่อาวุธโดน (Hit Delay) ===
            if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);

            // === Phase 3: ตรวจจับและสร้างดาเมจ ===
            Transform src = attackOrigin != null ? attackOrigin : transform;
            
            // คำนวณจุดเริ่มต้นและทิศทาง
            Vector3 origin = useOwnerForward 
                ? owner.position + Vector3.up * 1.0f + owner.forward * 0.3f  // ใช้ตำแหน่งผู้เล่น
                : src.position;                                                // ใช้ตำแหน่งอาวุธ
            Vector3 dir = useOwnerForward ? owner.forward : src.forward;
            
            // ตั้งค่า Layer Mask
            int maskVal = hitMask.value != 0 ? hitMask.value : Physics.DefaultRaycastLayers;
            if (debugLogHits)
            {
                string maskInfo = hitMask.value != 0 ? hitMask.value.ToString() : $"(fallback default) {maskVal}";
                Debug.Log($"[MeleeWeapon] SphereCast origin={origin} dir={dir} range={range} radius={radius} mask={maskInfo}");
            }
            
            // วิธีที่ 1: ใช้ SphereCast (ตรวจจับตามทิศทาง)
            var sphereHits = Physics.SphereCastAll(origin, radius, dir, range, maskVal, QueryTriggerInteraction.Collide);

            // วิธีที่ 2: ใช้ OverlapCapsule (สำหรับเป้าหมายที่อยู่ใกล้มาก)
            Collider[] overlapHits = null;
            if (sphereHits == null || sphereHits.Length == 0)
            {
                Vector3 start = origin;
                Vector3 end = origin + dir * Mathf.Max(range, 0.05f);
                overlapHits = Physics.OverlapCapsule(start, end, radius, maskVal, QueryTriggerInteraction.Collide);
                if (debugLogHits)
                {
                    Debug.Log($"[MeleeWeapon] SphereCast had no hits; OverlapCapsule found {(overlapHits != null ? overlapHits.Length : 0)} candidates.");
                }
            }

            // === Phase 4: สร้างดาเมจให้เป้าหมาย (ไม่ซ้ำกัน) ===
            System.Collections.Generic.HashSet<Health> damaged = new System.Collections.Generic.HashSet<Health>();

            // ประมวลผล SphereCast Hits
            if (sphereHits != null)
                foreach (var rh in sphereHits)
                {
                    var col = rh.collider;
                    var hp = col.GetComponentInParent<Health>() ?? col.GetComponent<Health>();
                    if (hp != null && !hp.IsDead)
                    {
                        // ข้ามตัวผู้เล่นเอง
                        var ownerHp = owner.GetComponentInParent<Health>() ?? owner.GetComponent<Health>();
                        if (hp == ownerHp) continue;
                        if (damaged.Contains(hp)) continue;  // ป้องกันโดนซ้ำ

                        hp.TakeDamage(damage);
                        damaged.Add(hp);
                        if (debugLogHits)
                        {
                            Debug.Log($"[MeleeWeapon] Hit {col.name} for {damage}. Target HP: {hp.CurrentHealth}");
                        }
                    }
                }

            // ประมวลผล Overlap Hits (Fallback)
            if (overlapHits != null)
            {
                foreach (var col in overlapHits)
                {
                    var hp = col.GetComponentInParent<Health>() ?? col.GetComponent<Health>();
                    if (hp != null && !hp.IsDead)
                    {
                        var ownerHp = owner.GetComponentInParent<Health>() ?? owner.GetComponent<Health>();
                        if (hp == ownerHp) continue;
                        if (damaged.Contains(hp)) continue;  // ป้องกันโดนซ้ำ
                        
                        hp.TakeDamage(damage);
                        damaged.Add(hp);
                        if (debugLogHits)
                        {
                            Debug.Log($"[MeleeWeapon] (Overlap) Hit {col.name} for {damage}. Target HP: {hp.CurrentHealth}");
                        }
                    }
                }
            }

            // Safety Check: ถ้าใช้เวลานานเกินไป แสดง Warning
            if (Time.time - startTime > Mathf.Max(0.5f, attackCooldown * 3f) && debugLogHits)
                Debug.LogWarning("[MeleeWeapon] Attack took unusually long; forcing unlock.");
        }
        finally
        {
            // ปลดล็อคสถานะโจมตี (ไม่ว่าจะสำเร็จหรือเกิด Error)
            attacking = false;
        }
    }

    /// <summary>
    /// เรียกเมื่อ Component ถูก Disable - รีเซ็ตสถานะ
    /// </summary>
    private void OnDisable()
    {
        attacking = false;  // ป้องกันติดสถานะโจมตี
    }

    /// <summary>
    /// เรียกเมื่อ Component ถูกทำลาย - รีเซ็ตสถานะ
    /// </summary>
    private void OnDestroy()
    {
        attacking = false;  // ป้องกันติดสถานะโจมตี
    }

#if UNITY_EDITOR
    /// <summary>
    /// แสดง Gizmo ในโหมด Editor เพื่อดูพื้นที่โจมตี
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);  // สีส้ม
        Transform src = attackOrigin != null ? attackOrigin : transform;
        Vector3 origin = src.position;
        Vector3 dir = src.forward;
        
        // วาดวงกลมจุดเริ่มต้นและจุดสิ้นสุด + เส้นเชื่อม
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawWireSphere(origin + dir * range, radius);
        Gizmos.DrawLine(origin, origin + dir * range);
    }
#endif

    /// <summary>
    /// Coroutine สำหรับ Animation แกว่งอาวุธ (Procedural Swing)
    /// </summary>
    private System.Collections.IEnumerator DoSwing()
    {
        if (swingTime <= 0.01f || swingAngle == 0f) yield break;  // ถ้าไม่ต้องการ Swing ให้ข้าม
        
        float t = 0f;
        var start = defaultLocalRot;
        var end = defaultLocalRot * Quaternion.Euler(-swingAngle, 0f, 0f);
        
        // Phase 1: แกว่งไปข้างหน้า
        while (t < 1f)
        {
            t += Time.deltaTime / swingTime;
            float k = swingCurve.Evaluate(Mathf.Clamp01(t));
            transform.localRotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        
        // Phase 2: แกว่งกลับ
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / swingTime;
            float k = swingCurve.Evaluate(Mathf.Clamp01(t));
            transform.localRotation = Quaternion.Slerp(end, start, k);
            yield return null;
        }
        
        transform.localRotation = start;  // กลับมาที่มุมเริ่มต้น
    }
}
