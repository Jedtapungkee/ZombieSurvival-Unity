using UnityEngine;

public enum PickupType { Melee, Gun, Medkit }

public class Pickup : MonoBehaviour
{
    public PickupType type;
    [Tooltip("Prefab of the weapon to equip (for Melee/Gun). For Medkit, leave null.")]
    public GameObject weaponPrefab;
    [Tooltip("Amount to heal for Medkit.")]
    public int healAmount = 25;
    [Header("Pickup Settings")]
    public float rotateSpeed = 60f;
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 2f;

    [Header("Visual (Optional)")]
    [Tooltip("Parent transform that will rotate/bob. If null, this object will be used.")]
    public Transform visualRoot;
    [Tooltip("Automatically spawn a non-interactive visual using the weaponPrefab.")]
    public bool autoCreateVisualFromWeapon = true;
    public Vector3 visualLocalOffset = new Vector3(0, 0.25f, 0);
    public Vector3 visualLocalEuler;
    public Vector3 visualLocalScale = Vector3.one;

    private Vector3 basePos;
    private Vector3 visualBaseLocalPos;

    void Start()
    {
        basePos = transform.position;
        if (visualRoot == null) visualRoot = transform;
        visualBaseLocalPos = visualRoot != transform ? visualRoot.localPosition : Vector3.zero;

        if (autoCreateVisualFromWeapon && visualRoot != null && visualRoot.childCount == 0 && weaponPrefab != null && (type == PickupType.Melee || type == PickupType.Gun))
        {
            var vis = Instantiate(weaponPrefab, visualRoot);
            // Remove interactive components from the display clone
            var wb = vis.GetComponent<WeaponBase>(); if (wb) Destroy(wb);
            var rb = vis.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
            foreach (var col in vis.GetComponentsInChildren<Collider>(true)) Destroy(col);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            vis.transform.localScale = Vector3.one;
        }

        // Apply initial visual offsets
        if (visualRoot != null)
        {
            visualRoot.localPosition = visualBaseLocalPos + visualLocalOffset;
            if (visualLocalEuler != Vector3.zero) visualRoot.localRotation = Quaternion.Euler(visualLocalEuler);
            if (visualLocalScale != Vector3.one) visualRoot.localScale = visualLocalScale;
        }
    }

    void Update()
    {
        var target = visualRoot != null ? visualRoot : transform;
        target.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        if (target == transform)
        {
            // If using self as visual, move the whole object
            transform.position = basePos + Vector3.up * bob;
        }
        else
        {
            // Keep root (trigger) fixed and only bob the visual
            target.localPosition = visualBaseLocalPos + visualLocalOffset + new Vector3(0f, bob, 0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var combat = other.GetComponentInChildren<PlayerCombat>();
        if (combat == null) combat = other.GetComponent<PlayerCombat>();
        if (combat == null) return;

        switch (type)
        {
            case PickupType.Melee:
                if (weaponPrefab != null) combat.EquipWeapon(weaponPrefab);
                break;
            case PickupType.Gun:
                if (weaponPrefab != null) combat.EquipWeapon(weaponPrefab);
                break;
            case PickupType.Medkit:
                var hp = other.GetComponent<Health>();
                if (hp != null && !hp.IsDead)
                {
                    hp.Heal(Mathf.Max(1, healAmount));
                }
                break;
        }

        Destroy(gameObject);
    }
}
