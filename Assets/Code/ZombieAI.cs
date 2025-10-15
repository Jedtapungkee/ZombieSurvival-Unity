using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI ของซอมบี้ - จัดการการเดิน, วิ่ง, โจมตี และตาย
/// ใช้ NavMeshAgent สำหรับการเคลื่อนที่ และ Animator สำหรับ Animation
/// </summary>
public class ZombieAI : MonoBehaviour
{
    [Header("Ranges")]
    public float detectionRange = 12f;   // ระยะที่เริ่มตรวจจับและไล่ตามผู้เล่น (เมตร)
    public float attackRange = 2f;       // ระยะโจมตีผู้เล่น (เมตร)
    public float runStartDistance = 6f;  // ถ้าอยู่ใกล้กว่าค่านี้จะวิ่ง, ไกลกว่านี้จะเดิน

    [Header("Speeds")]
    public float walkSpeed = 1.6f;       // ความเร็วเดิน (m/s)
    public float runSpeed = 3.2f;        // ความเร็ววิ่ง (m/s)

    [Header("Attack")]
    public float attackCooldown = 1.5f;      // ช่วงเวลาระหว่างการโจมตีแต่ละครั้ง (วินาที)
    public float attackAnimDuration = 0.8f;  // ระยะเวลา Animation โจมตี (วินาที)
    public int attackDamage = 5;             // ความเสียหายต่อการโจมตีหนึ่งครั้ง
    public float attackHitDelay = 0.3f;      // หน่วงเวลาก่อนสร้างความเสียหาย (ซิงค์กับ Animation)

    [Header("Animator Parameters (Bool)")]
    public string walkBoolParam = "isWalking";      // ชื่อ Bool Parameter ใน Animator สำหรับเดิน
    public string runBoolParam = "isRunning";       // ชื่อ Bool Parameter ใน Animator สำหรับวิ่ง
    public string attackBoolParam = "isAttacking";  // ชื่อ Bool Parameter ใน Animator สำหรับโจมตี

    // Variables ภายใน
    private Transform target;            // เป้าหมาย (ผู้เล่น)
    private NavMeshAgent agent;          // Component สำหรับการเคลื่อนที่
    private Animator anim;               // Component สำหรับ Animation
    private float lastAttackTime;        // เวลาที่โจมตีครั้งล่าสุด
    private bool isAttackingNow;         // กำลังอยู่ในขั้นตอนโจมตีหรือไม่
    private bool hasWalkBool, hasRunBool, hasAttackBool;  // ตรวจสอบว่ามี Parameter ใน Animator หรือไม่
    private Health targetHealth;         // Health Component ของเป้าหมาย
    private Health selfHealth;           // Health Component ของตัวเอง
    
    [Header("Death Handling")]
    [Tooltip("Animator bool parameter to set when dying.")]
    public string deathBoolParam = "isDead";        // ชื่อ Bool Parameter สำหรับตาย
    [Tooltip("Seconds to keep corpse before destroy. If 0, try to use Death state's length.")]
    public float deathCleanupDelay = 2.5f;          // เวลาก่อนลบซากออกจากเกม (วินาที)
    private bool isDead;                            // สถานะตายแล้วหรือยัง

    /// <summary>
    /// เรียกตอนเริ่มต้น - หา Components, ตรวจสอบ Animator Parameters, และหาผู้เล่น
    /// </summary>
    void Start()
    {
        // หา Components ที่จำเป็น
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        selfHealth = GetComponent<Health>();

        // ตรวจสอบว่ามี Components ครบหรือไม่
        if (agent == null)
        {
            Debug.LogError("[ZombieAI] Missing NavMeshAgent.", this);
        }
        if (anim == null)
        {
            Debug.LogError("[ZombieAI] Missing Animator.", this);
        }

        // ตรวจสอบว่า Animator มี Parameters ที่ต้องการหรือไม่ (ป้องกัน typo)
        if (anim != null)
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool)
                {
                    if (p.name == walkBoolParam) hasWalkBool = true;
                    if (p.name == runBoolParam) hasRunBool = true;
                    if (p.name == attackBoolParam) hasAttackBool = true;
                }
            }
            
            // แสดง Warning ถ้าไม่เจอ Parameter
            if (!hasWalkBool && !string.IsNullOrEmpty(walkBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{walkBoolParam}'.", this);
            if (!hasRunBool && !string.IsNullOrEmpty(runBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{runBoolParam}'.", this);
            if (!hasAttackBool && !string.IsNullOrEmpty(attackBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{attackBoolParam}'.", this);
        }

        // Subscribe to own death event (เมื่อตาย จะเรียก OnSelfDied)
        if (selfHealth != null)
        {
            selfHealth.Died += OnSelfDied;
        }

        // หาผู้เล่นอัตโนมัติจาก Tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            targetHealth = target.GetComponent<Health>();
        }
    }

    /// <summary>
    /// Update ทุกเฟรม - คำนวณระยะ, เลือกสถานะ (เดิน/วิ่ง/โจมตี), และอัพเดท Animation
    /// </summary>
    void Update()
    {
        // ถ้าตายแล้ว หรือไม่มี target/agent/anim ไม่ต้องทำอะไร
        if (isDead || target == null || agent == null || anim == null) return;

        // คำนวณระยะห่างระหว่างซอมบี้กับผู้เล่น
        float distance = Vector3.Distance(transform.position, target.position);

        // === อยู่ในระยะตรวจจับ -> ไล่ตามผู้เล่น ===
        if (distance <= detectionRange)
        {
            agent.SetDestination(target.position);  // ตั้งจุดหมายปลายทางเป็นตำแหน่งผู้เล่น
            agent.isStopped = false;                // เปิดการเคลื่อนที่

            // กำหนดสถานะตามระยะห่าง
            bool shouldAttack = distance <= attackRange;  // ใกล้มากพอ -> โจมตี
            bool shouldRun = !shouldAttack && distance <= Mathf.Max(runStartDistance, attackRange);  // ใกล้ปานกลาง -> วิ่ง
            bool shouldWalk = !shouldAttack && !shouldRun;  // ไกล -> เดิน

            // ตั้งค่าความเร็วของ NavMeshAgent
            agent.speed = shouldRun ? runSpeed : walkSpeed;

            // อัพเดท Animator Parameters
            if (hasWalkBool) anim.SetBool(walkBoolParam, shouldWalk && !isAttackingNow);
            if (hasRunBool) anim.SetBool(runBoolParam, shouldRun && !isAttackingNow);

            // โจมตีเมื่อเข้าใกล้และถึงเวลาโจมตีครั้งต่อไป
            if (shouldAttack && !isAttackingNow && Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            // === นอกระยะตรวจจับ -> Idle (หยุดเคลื่อนที่) ===
            if (hasWalkBool) anim.SetBool(walkBoolParam, false);
            if (hasRunBool) anim.SetBool(runBoolParam, false);
            agent.ResetPath();  // ยกเลิกเส้นทางการเดิน
        }
    }

    /// <summary>
    /// Coroutine สำหรับการโจมตี - หยุดเคลื่อนที่, เล่น Animation, สร้างความเสียหาย, แล้วกลับมาเคลื่อนที่ต่อ
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        if (isDead) yield break;  // ถ้าตายแล้ว ไม่ต้องโจมตี
        
        // เริ่มโจมตี
        isAttackingNow = true;
        lastAttackTime = Time.time;
        agent.isStopped = true;  // หยุดการเคลื่อนที่ชั่วคราว

        // ปิด Animation เดิน/วิ่ง
        if (hasWalkBool) anim.SetBool(walkBoolParam, false);
        if (hasRunBool) anim.SetBool(runBoolParam, false);

        // เปิด Animation โจมตี
        if (hasAttackBool) anim.SetBool(attackBoolParam, true);

        // รอจนถึงจังหวะที่มือโดนในแอนิเมชัน (เช่น 0.3 วินาที)
        yield return new WaitForSeconds(attackHitDelay);
        
        // สร้างความเสียหายให้เป้าหมาย
        if (!isDead && targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(Mathf.Max(1, attackDamage));
        }

        // รอให้แอนิเมชันโจมตีเล่นจบ
        yield return new WaitForSeconds(Mathf.Max(0f, attackAnimDuration - attackHitDelay));

        // ปิด Animation โจมตี และกลับมาเคลื่อนที่ต่อ
        if (hasAttackBool) anim.SetBool(attackBoolParam, false);
        if (!isDead) agent.isStopped = false;
        isAttackingNow = false;
    }

    /// <summary>
    /// เรียกเมื่อซอมบี้ตาย - หยุด AI, เพิ่มคะแนน, เล่น Animation ตาย, และลบออกจากเกม
    /// </summary>
    private void OnSelfDied()
    {
        if (isDead) return;  // ป้องกันเรียกซ้ำ
        isDead = true;
        
        // เพิ่มคะแนนให้ผู้เล่นผ่าน ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddZombieKillScore();
        }
        else
        {
            Debug.LogWarning("[ZombieAI] ScoreManager not found! No points awarded.", this);
        }
        
        // หยุดการเคลื่อนที่และโจมตี
        if (agent != null) 
        { 
            agent.isStopped = true; 
            agent.ResetPath(); 
        }
        isAttackingNow = false;
        
        // ปิด Colliders ทั้งหมด (เพื่อไม่ให้โดนอีก)
        foreach (var col in GetComponentsInChildren<Collider>()) 
            col.enabled = false;
        
        // เล่น Animation ตาย
        if (anim != null)
        {
            // ปิด Animation การเคลื่อนที่และโจมตี
            if (hasWalkBool) anim.SetBool(walkBoolParam, false);
            if (hasRunBool) anim.SetBool(runBoolParam, false);
            if (hasAttackBool) anim.SetBool(attackBoolParam, false);
            
            // เปิด Animation ตาย
            if (!string.IsNullOrEmpty(deathBoolParam)) 
                anim.SetBool(deathBoolParam, true);
        }
        
        // ลบ GameObject หลังจากผ่านเวลาที่กำหนด (ให้เวลาเล่น Animation ตาย)
        float delay = Mathf.Max(0f, deathCleanupDelay);
        if (delay <= 0.01f)
        {
            delay = 2.5f;  // ค่า Default ถ้าไม่ได้กำหนด
        }
        Destroy(gameObject, delay);
    }

    /// <summary>
    /// เรียกเมื่อ GameObject ถูกทำลาย - ยกเลิก Event Subscription
    /// </summary>
    private void OnDestroy()
    {
        if (selfHealth != null) 
            selfHealth.Died -= OnSelfDied;  // ยกเลิก Subscribe Event
    }
}