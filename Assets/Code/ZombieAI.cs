using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Ranges")]
    public float detectionRange = 12f;   // ระยะที่เริ่มไล่ตาม
    public float attackRange = 2f;       // ระยะโจมตี
    public float runStartDistance = 6f;  // ถ้าเข้าใกล้กว่าค่านี้ (มากกว่า attackRange) ให้ "วิ่ง"; ถ้าไกลกว่านี้ให้ "เดิน"

    [Header("Speeds")]
    public float walkSpeed = 1.6f;
    public float runSpeed = 3.2f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;  // เวลาหน่วงระหว่างโจมตี
    public float attackAnimDuration = 0.8f; // เวลาที่ถือว่าอยู่ในแอนิเมชันโจมตี (สำหรับ Bool)
    public int attackDamage = 5;          // ความเสียหายต่อการโจมตีหนึ่งครั้ง
    public float attackHitDelay = 0.3f;   // หน่วงเวลาจังหวะโดน (ซิงค์กับจังหวะมือโดนในแอนิเมชัน)

    [Header("Animator Parameters (Bool)")]
    public string walkBoolParam = "isWalking";
    public string runBoolParam = "isRunning";
    public string attackBoolParam = "isAttacking"; // ถ้า Animator ของคุณสะกดเป็น "isAttackin" ให้แก้ชื่อใน Inspector

    private Transform target;
    private NavMeshAgent agent;
    private Animator anim;
    private float lastAttackTime;
    private bool isAttackingNow;
    private bool hasWalkBool, hasRunBool, hasAttackBool;
    private Health targetHealth;
    private Health selfHealth;
    [Header("Death Handling")]
    [Tooltip("Animator bool parameter to set when dying.")]
    public string deathBoolParam = "isDead";
    [Tooltip("Seconds to keep corpse before destroy. If 0, try to use Death state's length.")]
    public float deathCleanupDelay = 2.5f;
    private bool isDead;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        selfHealth = GetComponent<Health>();

        if (agent == null)
        {
            Debug.LogError("[ZombieAI] Missing NavMeshAgent.", this);
        }
        if (anim == null)
        {
            Debug.LogError("[ZombieAI] Missing Animator.", this);
        }

        // ตรวจสอบว่ามีพารามิเตอร์ Bool ตามที่กำหนดหรือไม่ เพื่อช่วยจับเคสชื่อไม่ตรง
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
            if (!hasWalkBool && !string.IsNullOrEmpty(walkBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{walkBoolParam}'.", this);
            if (!hasRunBool && !string.IsNullOrEmpty(runBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{runBoolParam}'.", this);
            if (!hasAttackBool && !string.IsNullOrEmpty(attackBoolParam))
                Debug.LogWarning($"[ZombieAI] Animator missing Bool '{attackBoolParam}'. If your parameter is named differently (e.g. 'isAttackin'), change it in the ZombieAI inspector.", this);
        }

        // Subscribe to own death
        if (selfHealth != null)
        {
            selfHealth.Died += OnSelfDied;
        }

        // หา Player อัตโนมัติ โดยใช้ Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            targetHealth = target.GetComponent<Health>();
        }
    }

    void Update()
    {
    if (isDead || target == null || agent == null || anim == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectionRange)
        {
            agent.SetDestination(target.position);
            agent.isStopped = false;

            // เลือกเดิน/วิ่งตามระยะที่กำหนด (ภายใน detectionRange)
            bool shouldAttack = distance <= attackRange;
            bool shouldRun = !shouldAttack && distance <= Mathf.Max(runStartDistance, attackRange);
            bool shouldWalk = !shouldAttack && !shouldRun; // ไกลกว่า runStartDistance แต่ยังอยู่ใน detectionRange ให้เดิน

            // ตั้งค่า speed ของ Agent
            agent.speed = shouldRun ? runSpeed : walkSpeed;

            // อัปเดตค่า Bool ใน Animator ให้ตรงกับสถานะปัจจุบัน
            if (hasWalkBool) anim.SetBool(walkBoolParam, shouldWalk && !isAttackingNow);
            if (hasRunBool) anim.SetBool(runBoolParam, shouldRun && !isAttackingNow);

            // โจมตีเมื่อเข้าใกล้
            if (shouldAttack && !isAttackingNow && Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            // นอกระยะตรวจจับ -> Idle
            if (hasWalkBool) anim.SetBool(walkBoolParam, false);
            if (hasRunBool) anim.SetBool(runBoolParam, false);
            agent.ResetPath();
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttackingNow = true;
        lastAttackTime = Time.time;
        agent.isStopped = true; // หยุดชั่วคราวเพื่อเล่นแอนิเมชันโจมตี

        // ปิดการเดิน/วิ่ง
        if (hasWalkBool) anim.SetBool(walkBoolParam, false);
        if (hasRunBool) anim.SetBool(runBoolParam, false);

        // เปิดสถานะโจมตี (ใช้ Bool ตามที่กำหนด)
        if (hasAttackBool) anim.SetBool(attackBoolParam, true);

        // จังหวะโดนจริง
        yield return new WaitForSeconds(attackHitDelay);
        if (!isDead && targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(Mathf.Max(1, attackDamage));
        }

        // รอจนแอนิเมชันโจมตีจบลงก่อนปล่อยเดิน/วิ่งต่อ
        yield return new WaitForSeconds(Mathf.Max(0f, attackAnimDuration - attackHitDelay));

        if (hasAttackBool) anim.SetBool(attackBoolParam, false);
        if (!isDead) agent.isStopped = false;
        isAttackingNow = false;
    }

    private void OnSelfDied()
    {
        if (isDead) return;
        isDead = true;
        // Stop AI movement and attacks
        if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
        isAttackingNow = false;
        // Disable all colliders to avoid further hits
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        // Play death animation
        if (anim != null)
        {
            // Turn off locomotion bools
            if (hasWalkBool) anim.SetBool(walkBoolParam, false);
            if (hasRunBool) anim.SetBool(runBoolParam, false);
            if (hasAttackBool) anim.SetBool(attackBoolParam, false);
            if (!string.IsNullOrEmpty(deathBoolParam)) anim.SetBool(deathBoolParam, true);
        }
        // Destroy after delay (use provided delay; user can match it to clip length)
        float delay = Mathf.Max(0f, deathCleanupDelay);
        if (delay <= 0.01f)
        {
            // Fallback conservative delay
            delay = 2.5f;
        }
        Destroy(gameObject, delay);
    }

    private void OnDestroy()
    {
        if (selfHealth != null) selfHealth.Died -= OnSelfDied;
    }
}
