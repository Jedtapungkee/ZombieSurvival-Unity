using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerCombat : MonoBehaviour
{
    [Header("Equipment")] 
    public Transform rightHand; // จุดติดอาวุธ
    public WeaponBase currentWeapon;

    [Header("Input")] 
    public KeyCode fireKey = KeyCode.Mouse0;
    [Header("Animation (Optional)")]
    public Animator animator;
    [Tooltip("Trigger when using melee weapon; leave empty to skip")] public string meleeAttackTrigger = "Attack";
    [Tooltip("Trigger when shooting gun; leave empty to skip")] public string shootTrigger = "Shoot";
    [Tooltip("Seconds to keep shoot IK lock active after a gun shot.")]
    public float shootIkHoldTime = 0.15f;
    private float shootIkUntil;
    public float ShootIkWeight => shootIkHoldTime <= 0 ? 0f : Mathf.Clamp01((shootIkUntil - Time.time) / shootIkHoldTime);

    void Awake()
    {
        // Auto-find RightHandSocket if not assigned
        if (rightHand == null)
        {
            // Try direct find by name anywhere under this object
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("RightHandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    rightHand = t;
                    break;
                }
            }
        }

        // Auto-find Animator if not assigned
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

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (rightHand == null)
        {
            Debug.LogWarning("[PlayerCombat] rightHand is not assigned.");
            return;
        }
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }
        var go = Instantiate(weaponPrefab);
        currentWeapon = go.GetComponent<WeaponBase>();
        if (currentWeapon == null)
        {
            Debug.LogError("[PlayerCombat] Weapon prefab missing WeaponBase-derived component.");
            Destroy(go);
            return;
        }
        currentWeapon.OnEquip(rightHand);
        Debug.Log($"Equipped: {currentWeapon.displayName}");
    }

    void Update()
    {
        if (currentWeapon == null) return;
        bool fire = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            fire = Mouse.current.leftButton.isPressed;
        }
        else if (Gamepad.current != null)
        {
            fire = Gamepad.current.rightTrigger.isPressed || Gamepad.current.leftTrigger.isPressed;
        }
#else
        fire = Input.GetKey(fireKey);
#endif
        if (fire)
        {
            if (currentWeapon.TryAttack(transform))
            {
                if (animator != null)
                {
                    if (currentWeapon is MeleeWeapon)
                    {
                        if (!string.IsNullOrEmpty(meleeAttackTrigger)) animator.SetTrigger(meleeAttackTrigger);
                    }
                    else if (currentWeapon is GunWeapon)
                    {
                        if (!string.IsNullOrEmpty(shootTrigger)) animator.SetTrigger(shootTrigger);
                        // Keep a short IK lock window to stabilize hands while the shoot pose plays
                        shootIkUntil = Time.time + shootIkHoldTime;
                    }
                }
            }
        }
    }
}
