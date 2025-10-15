using System.Collections;
using UnityEngine;

/// <summary>
/// คลาสสำหรับอาวุธปืน - รับผิดชอบการยิง, รีโหลด, และเอฟเฟกต์การยิง
/// ใช้ Raycast เพื่อตรวจจับเป้าหมาย และคำนวณทิศทางจากกล้อง
/// </summary>
public class GunWeapon : WeaponBase
{
    [Header("Gun")]
    public int damage = 20;              // ความเสียหายต่อนัดยิง
    public float range = 50f;            // ระยะยิงสูงสุด (เมตร)
    public LayerMask hitMask;            // Layer ที่กระสุนจะโดน (เช่น Enemy, Environment)
    public int ammoInClip = 10;          // กระสุนในแม็กปัจจุบัน
    public int clipSize = 10;            // ขนาดแม็กเต็ม
    public float reloadTime = 1.2f;      // เวลาในการรีโหลด (วินาที)
    private bool reloading;              // สถานะกำลังรีโหลดอยู่หรือไม่
    private Transform muzzle;            // จุดที่กระสุนออก (ปลายปืน)
    private Vector3 defaultLocalPos;     // ตำแหน่งเริ่มต้นของปืน (สำหรับ Recoil)
    private bool recoiling;              // สถานะกำลัง Recoil อยู่หรือไม่
    [Header("Aiming")]
    [Tooltip("Use camera-centered ray for TPS aiming; if false, use owner.forward (ตัวละคร).")]
    public bool useCameraAim = false;            // false = กระสุนออกจากตัวละคร, true = ออกจากกล้อง
    [Tooltip("If using camera aim, the ray will target the point at the center of screen, then direction is from muzzle to that point.")]
    public bool alignFromMuzzleToAimPoint = true; // จัดทิศทางจากปลายปืนไปยังจุดเล็ง
    
    [Header("FX (Optional)")]
    [Tooltip("If a ParticleSystem exists under Muzzle named 'MuzzleFlash', play it on shoot.")]
    public bool playMuzzleParticle = true;       // เปิด/ปิด Particle Effect เวลายิง
    [Tooltip("Draw a short debug ray each shot to visualize the direction (Editor only)")]
    public bool debugDrawRay = true;             // แสดงเส้น Debug เวลายิง (เฉพาะ Editor)
    private ParticleSystem muzzleFx;            // Reference ไปยัง Particle System

    /// <summary>
    /// เรียกเมื่อติดปืนเข้ากับมือ - ตั้งค่าตำแหน่ง, หา Muzzle และ Particle Effect
    /// </summary>
    public override void OnEquip(Transform hand)
    {
        // ผูกปืนเข้ากับมือ (rightHand)
        transform.SetParent(hand, worldPositionStays: false);
        transform.localPosition = gripLocalPosition;
        transform.localRotation = Quaternion.Euler(gripLocalEuler);
        
        // หา Transform ชื่อ "Muzzle" (ปลายปืน) ถ้าไม่มีให้ใช้ตัวปืนเอง
        muzzle = transform.Find("Muzzle");
        if (muzzle == null) muzzle = transform;
        defaultLocalPos = transform.localPosition;

        // หา Particle System สำหรับ Muzzle Flash (ไฟออกจากปลายปืน)
        if (playMuzzleParticle && muzzle != null)
        {
            var fx = muzzle.GetComponentInChildren<ParticleSystem>(true);
            if (fx != null && fx.name.ToLower().Contains("muzzle"))
            {
                muzzleFx = fx;
            }
        }
    }

    /// <summary>
    /// เรียกเมื่อถอดปืนออก
    /// </summary>
    public override void OnUnequip()
    {
        transform.SetParent(null);
    }

    /// <summary>
    /// ฟังก์ชันหลักสำหรับการยิง - ตรวจสอบเงื่อนไข, คำนวณทิศทาง, ยิง Raycast และสร้างความเสียหาย
    /// </summary>
    public override bool TryAttack(Transform owner)
    {
        // ตรวจสอบเงื่อนไขก่อนยิง
        if (reloading) return false;                                    // กำลังรีโหลดอยู่
        if (Time.time - lastAttack < attackCooldown) return false;     // ยังไม่ถึงเวลายิงครั้งต่อไป
        if (ammoInClip <= 0) { StartCoroutine(Reload()); return false; } // กระสุนหมด เริ่มรีโหลด
        
        // อัพเดทสถานะ
        lastAttack = Time.time;
        ammoInClip--;

        // === คำนวณจุดเริ่มต้นและทิศทางการยิง ===
        // จุดเริ่มต้น: ใช้ปลายปืน (muzzle) หรือตำแหน่งเหนือศีรษะตัวละคร
        Vector3 origin = muzzle != null ? muzzle.position : owner.position + Vector3.up * 1.2f;
        Vector3 dir = owner.forward; // ทิศทาง Default = ทิศที่ตัวละครหัน

        // === ระบบ Third-Person Shooting: ใช้กล้องหาจุดเล็ง แล้วยิงจากปืนไปยังจุดนั้น ===
        if (Camera.main != null)
        {
            var cam = Camera.main;
            // สร้าง Ray จากกึ่งกลางหน้าจอ (0.5, 0.5)
            Ray camRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // ยิง Raycast จากกล้องเพื่อหาจุดที่เล็งอยู่
            var camHits = Physics.RaycastAll(camRay, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(camHits, (a, b) => a.distance.CompareTo(b.distance)); // เรียงตามระยะใกล้-ไกล
            
            // Default: ถ้าไม่โดนอะไรให้เล็งไปข้างหน้าไกลๆ
            Vector3 aimPoint = camRay.origin + cam.transform.forward * range;
            
            // หาจุดแรกที่โดน (ไม่รวมตัวผู้เล่นเอง)
            foreach (var h in camHits)
            {
                if (owner != null && h.collider.transform.IsChildOf(owner))
                    continue; // ข้ามตัวผู้เล่น (ป้องกันยิงตัวเอง)
                aimPoint = h.point; // ใช้จุดนี้เป็นจุดเล็ง
                break;
            }

            // คำนวณทิศทางจากปลายปืนไปยังจุดเล็ง
            dir = (aimPoint - origin).normalized;

            // Safety Check: ป้องกันการยิงไปทางหลัง (ถ้าทิศทางตรงข้ามกับกล้อง)
            if (Vector3.Dot(dir, cam.transform.forward) < 0f)
            {
                dir = cam.transform.forward;
            }
        }
        else if (useCameraAim)
        {
            // Fallback: ถ้าไม่มีกล้อง ให้ยิงตามทิศทางตัวละคร
            dir = owner.forward;
        }

        // เลื่อนจุดเริ่มต้นไปข้างหน้าเล็กน้อย เพื่อป้องกันโดนตัว Collider ของผู้เล่นเอง
        origin += dir * 0.02f;

        // แสดงเส้น Debug สีเหลืองในโหมด Editor (ช่วยดู Debug)
        if (debugDrawRay)
        {
            Debug.DrawRay(origin, dir * range, Color.yellow, 0.1f);
        }

        // ตั้งค่า Layer Mask (ถ้าไม่ได้กำหนดใน Inspector ให้ใช้ค่า Default)
        int maskVal = hitMask.value != 0 ? hitMask.value : Physics.DefaultRaycastLayers;
        if (debugLogHits && hitMask.value == 0)
        {
            Debug.Log("[GunWeapon] hitMask not set; using Physics.DefaultRaycastLayers", this);
        }

        // === ยิง Raycast เพื่อตรวจจับเป้าหมาย ===
        var hits = Physics.RaycastAll(origin, dir, range, maskVal, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // เรียงตามระยะใกล้-ไกล
        
        foreach (var h in hits)
        {
            // ข้ามตัวผู้เล่นเอง (ป้องกันยิงตัวเอง)
            if (h.collider.transform.IsChildOf(owner))
            {
                continue;
            }

            // หา Component Health ของเป้าหมาย
            var hp = h.collider.GetComponentInParent<Health>() ?? h.collider.GetComponent<Health>();
            if (hp != null && !hp.IsDead)
            {
                // สร้างความเสียหาย
                hp.TakeDamage(damage);
                
                if (debugLogHits)
                {
                    Debug.Log($"[GunWeapon] Hit {h.collider.name} for {damage}. Target HP: {hp.CurrentHealth}");
                }
                
                // เล่น Muzzle Flash Effect
                if (muzzleFx != null && playMuzzleParticle)
                {
                    muzzleFx.Play(true);
                }
            }
            
            // หยุดที่เป้าหมายแรกที่โดน (กระสุนไม่ทะลุ)
            break;
        }

        // Debug Log เมื่อยิงไม่โดนอะไรเลย
        if (debugLogHits && (hits == null || hits.Length == 0))
        {
            Debug.Log("[GunWeapon] No hit detected. Check hitMask includes Enemy layer and that enemy has Collider.", this);
        }

        // เล่น Recoil Animation (ปืนถอยหลังเล็กน้อย)
        if (!recoiling) StartCoroutine(Recoil());
        return true;
    }

    /// <summary>
    /// Coroutine สำหรับรีโหลดกระสุน - รอตามเวลาที่กำหนด แล้วเติมกระสุนเต็ม
    /// </summary>
    private IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime); // รอตามเวลา Reload
        ammoInClip = clipSize;                       // เติมกระสุนเต็ม
        reloading = false;
    }

    /// <summary>
    /// Coroutine สำหรับ Recoil Effect - ปืนถอยหลังแล้วกลับมาที่เดิม
    /// ทำให้ดูสมจริงเวลายิง
    /// </summary>
    private IEnumerator Recoil()
    {
        recoiling = true;
        float t = 0f;
        float dur = 0.06f; // ระยะเวลา Recoil
        Vector3 kick = new Vector3(0, 0, -0.05f); // ถอยหลัง 0.05 เมตร
        
        // Phase 1: ปืนถอยหลัง
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.Lerp(defaultLocalPos, defaultLocalPos + kick, t);
            yield return null;
        }
        
        // Phase 2: ปืนกลับมาที่เดิม
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.Lerp(defaultLocalPos + kick, defaultLocalPos, t);
            yield return null;
        }
        
        transform.localPosition = defaultLocalPos; // ตรวจสอบให้แน่ใจว่ากลับมาตำแหน่งเดิมพอดี
        recoiling = false;
    }
}
