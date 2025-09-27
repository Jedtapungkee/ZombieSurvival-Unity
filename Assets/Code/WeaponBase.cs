using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Common")]
    public string displayName = "Weapon";
    public float attackCooldown = 0.5f;
    protected float lastAttack;

    [Header("Grip Offset (Local to Hand)")]
    public Vector3 gripLocalPosition = Vector3.zero;
    public Vector3 gripLocalEuler = Vector3.zero;

    [Header("Debug")]
    public bool debugLogHits = false;

    public abstract void OnEquip(Transform hand);
    public abstract void OnUnequip();
    public abstract bool TryAttack(Transform owner);
}
