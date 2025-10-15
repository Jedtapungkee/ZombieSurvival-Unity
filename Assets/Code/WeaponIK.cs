using UnityEngine;

// Keeps the weapon in the right hand (via PlayerCombat) and uses IK to place the left hand
// on an IK target defined inside the weapon prefab (e.g., a child named "LeftHandIK").
// Enable IK Pass on the Base Layer of the Animator for this to work.
public class WeaponIK : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;          // Auto-find from children if null
    public PlayerCombat combat;        // Auto-find on this object if null

    [Header("Targets by Name inside Weapon Prefab")] 
    public string leftHandIKName = "LeftHandIK";   // empty child placed on foregrip
    public string rightHandIKName = "RightHandIK"; // optional

    [Header("Left Hand IK Settings")] 
    [Tooltip("ให้มือซ้ายจับปืนเฉพาะตอนยิงเท่านั้น")]
    public bool onlyUseLeftHandIKWhenShooting = true;
    [Range(0f,1f)] public float leftPosWeight = 0.9f;
    [Range(0f,1f)] public float leftRotWeight = 0.7f;
    [Tooltip("ความเร็วในการเปลี่ยน IK Weight (ยิ่งสูงยิ่งเร็ว)")]
    public float leftHandIKTransitionSpeed = 12f;
    [Tooltip("ลด Weight เมื่อไม่ยิง (0-1, 0=ปิด IK สนิท, 0.3=เหลือ IK เล็กน้อย)")]
    [Range(0f,1f)] public float leftHandIdleWeight = 0.2f;
    
    [Header("Right Hand IK Settings")] 
    [Tooltip("Use IK for right hand too (usually not needed if weapon is parented to socket)")] 
    public bool useRightHandIK;
    [Range(0f,1f)] public float rightPosWeight = 1f;
    [Range(0f,1f)] public float rightRotWeight = 1f;
    
    [Header("Shoot Stabilization")]
    [Tooltip("Keep right hand tightly on weapon for a short time after shooting to avoid the gun raising too high.")]
    public bool stabilizeRightHandOnShoot = true;
    [Range(0f,1f)] public float shootRightPosWeight = 1f;
    [Range(0f,1f)] public float shootRightRotWeight = 1f;
    [Tooltip("Optional extra roll offset for left hand while shooting (degrees)")]
    public float leftHandShootRoll = 0f;

    private Transform currentWeaponRoot;
    private Transform leftTarget;
    private Transform rightTarget;
    private float currentLeftHandIKWeight = 0f; // น้ำหนัก IK ปัจจุบันของมือซ้าย

    void Awake()
    {
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void UpdateTargets()
    {
        currentWeaponRoot = null;
        leftTarget = null; rightTarget = null;
        if (combat == null || combat.currentWeapon == null) return;
        currentWeaponRoot = combat.currentWeapon.transform;
        // Find by name anywhere under weapon
        foreach (var t in currentWeaponRoot.GetComponentsInChildren<Transform>(true))
        {
            if (leftTarget == null && !string.IsNullOrEmpty(leftHandIKName) && t.name.Equals(leftHandIKName, System.StringComparison.OrdinalIgnoreCase))
                leftTarget = t;
            if (rightTarget == null && !string.IsNullOrEmpty(rightHandIKName) && t.name.Equals(rightHandIKName, System.StringComparison.OrdinalIgnoreCase))
                rightTarget = t;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;
        if (combat == null || combat.currentWeapon == null)
        {
            // no weapon -> zero IK
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        if (currentWeaponRoot != combat.currentWeapon.transform || leftTarget == null && !string.IsNullOrEmpty(leftHandIKName))
        {
            UpdateTargets();
        }

        // คำนวณว่าควรใช้ IK มือซ้ายหรือไม่
        float targetLeftIKWeight = 0f;
        if (leftTarget != null)
        {
            if (onlyUseLeftHandIKWhenShooting)
            {
                // ใช้ IK เฉพาะตอนยิง (ตรวจสอบว่ากำลังกดปุ่มยิงอยู่)
                bool isShooting = combat != null && combat.IsShooting;
                targetLeftIKWeight = isShooting ? 1f : leftHandIdleWeight;
            }
            else
            {
                // ใช้ IK ตลอดเวลา
                targetLeftIKWeight = 1f;
            }
        }

        // ค่อยๆ เปลี่ยน IK Weight แบบ smooth
        currentLeftHandIKWeight = Mathf.Lerp(currentLeftHandIKWeight, targetLeftIKWeight, Time.deltaTime * leftHandIKTransitionSpeed);

        // Left hand IK
        if (leftTarget != null && currentLeftHandIKWeight > 0.01f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftPosWeight * currentLeftHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftRotWeight * currentLeftHandIKWeight);
            Vector3 lpos = leftTarget.position;
            Quaternion lrot = leftTarget.rotation;
            // slight roll tweak when shooting
            if (combat != null && combat.ShootIkWeight > 0f && Mathf.Abs(leftHandShootRoll) > 0.01f)
            {
                lrot = lrot * Quaternion.Euler(0f, 0f, leftHandShootRoll);
            }
            animator.SetIKPosition(AvatarIKGoal.LeftHand, lpos);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, lrot);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }

        // Right hand IK (optional)
        float rhPosW = 0f, rhRotW = 0f;
        if (useRightHandIK && rightTarget != null)
        {
            rhPosW = rightPosWeight; rhRotW = rightRotWeight;
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightTarget.rotation);
        }
        
        // Short stabilization after shooting (even if not using explicit right-hand IK target)
        if (stabilizeRightHandOnShoot && combat != null && combat.ShootIkWeight > 0f)
        {
            rhPosW = Mathf.Max(rhPosW, shootRightPosWeight * combat.ShootIkWeight);
            rhRotW = Mathf.Max(rhRotW, shootRightRotWeight * combat.ShootIkWeight);
        }

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rhPosW);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rhRotW);
    }
}
