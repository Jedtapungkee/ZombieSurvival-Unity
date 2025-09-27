using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee")]
    [Min(1)] public int damage = 25;
    public float range = 1.8f;
    public float radius = 0.5f;
    public LayerMask hitMask;
    [Tooltip("Delay before hit is applied to sync with animation.")]
    public float hitDelay = 0.1f;

    [Header("Procedural Swing (Optional)")]
    public float swingAngle = 30f;
    public float swingTime = 0.12f;
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Transform attach;
    private Quaternion defaultLocalRot;
    private bool attacking;
    [Header("Hit Origin (Optional)")]
    public Transform attackOrigin; // if null, use this transform
    [Tooltip("If true, cast forward from the player instead of the weapon – useful if weapon forward isn't aligned yet.")]
    public bool useOwnerForward = true;

    public override void OnEquip(Transform hand)
    {
        attach = hand;
        transform.SetParent(hand, worldPositionStays: false);
        transform.localPosition = gripLocalPosition;
        transform.localRotation = Quaternion.Euler(gripLocalEuler);
        defaultLocalRot = transform.localRotation;
    }

    public override void OnUnequip()
    {
        transform.SetParent(null);
    }

    public override bool TryAttack(Transform owner)
    {
        if (Time.time - lastAttack < attackCooldown) return false;
        if (attacking) return false;
        lastAttack = Time.time;
        attach = attach == null ? owner : attach;
        if (debugLogHits) Debug.Log("[MeleeWeapon] Attack started");
        StartCoroutine(AttackRoutine(owner));
        return true;
    }

    private System.Collections.IEnumerator AttackRoutine(Transform owner)
    {
        attacking = true;
        float startTime = Time.time;
        try
        {
            // Start swing
            yield return StartCoroutine(DoSwing());

            // Apply damage after delay (can be 0)
            if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);

            Transform src = attackOrigin != null ? attackOrigin : transform;
            Vector3 origin = useOwnerForward ? owner.position + Vector3.up * 1.0f + owner.forward * 0.3f : src.position;
            Vector3 dir = useOwnerForward ? owner.forward : src.forward;
            int maskVal = hitMask.value != 0 ? hitMask.value : Physics.DefaultRaycastLayers;
            if (debugLogHits)
            {
                string maskInfo = hitMask.value != 0 ? hitMask.value.ToString() : $"(fallback default) {maskVal}";
                Debug.Log($"[MeleeWeapon] SphereCast origin={origin} dir={dir} range={range} radius={radius} mask={maskInfo}");
            }
                // First try: SphereCast along the direction
                var sphereHits = Physics.SphereCastAll(origin, radius, dir, range, maskVal, QueryTriggerInteraction.Collide);

                // Fallback: OverlapCapsule along the path (handles targets already overlapping the start)
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

                // Apply damage (dedupe colliders by Health)
                System.Collections.Generic.HashSet<Health> damaged = new System.Collections.Generic.HashSet<Health>();

                if (sphereHits != null)
                foreach (var rh in sphereHits)
            {
                var col = rh.collider;
                var hp = col.GetComponentInParent<Health>() ?? col.GetComponent<Health>();
                if (hp != null && !hp.IsDead)
                {
                    // Skip self hit (owner)
                    var ownerHp = owner.GetComponentInParent<Health>() ?? owner.GetComponent<Health>();
                    if (hp == ownerHp) continue;
                        if (damaged.Contains(hp)) continue;

                    hp.TakeDamage(damage);
                        damaged.Add(hp);
                    if (debugLogHits)
                    {
                        Debug.Log($"[MeleeWeapon] Hit {col.name} for {damage}. Target HP: {hp.CurrentHealth}");
                    }
                }
            }

                if (overlapHits != null)
                {
                    foreach (var col in overlapHits)
                    {
                        var hp = col.GetComponentInParent<Health>() ?? col.GetComponent<Health>();
                        if (hp != null && !hp.IsDead)
                        {
                            var ownerHp = owner.GetComponentInParent<Health>() ?? owner.GetComponent<Health>();
                            if (hp == ownerHp) continue;
                            if (damaged.Contains(hp)) continue;
                            hp.TakeDamage(damage);
                            damaged.Add(hp);
                            if (debugLogHits)
                            {
                                Debug.Log($"[MeleeWeapon] (Overlap) Hit {col.name} for {damage}. Target HP: {hp.CurrentHealth}");
                            }
                        }
                    }
                }

            // Safety timeout: if something goes wrong, break the attack lock
            if (Time.time - startTime > Mathf.Max(0.5f, attackCooldown * 3f) && debugLogHits)
                Debug.LogWarning("[MeleeWeapon] Attack took unusually long; forcing unlock.");
        }
        finally
        {
            attacking = false;
        }
    }

    private void OnDisable()
    {
        // Safety: ensure we never remain stuck in attacking state across disable
        attacking = false;
    }

    private void OnDestroy()
    {
        // Safety: ensure we never remain stuck in attacking state across destroy
        attacking = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);
        Transform src = attackOrigin != null ? attackOrigin : transform;
        Vector3 origin = src.position;
        Vector3 dir = src.forward;
        // Draw start and end spheres and a line
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawWireSphere(origin + dir * range, radius);
        Gizmos.DrawLine(origin, origin + dir * range);
    }
#endif

    private System.Collections.IEnumerator DoSwing()
    {
        if (swingTime <= 0.01f || swingAngle == 0f) yield break;
        float t = 0f;
        var start = defaultLocalRot;
        var end = defaultLocalRot * Quaternion.Euler(-swingAngle, 0f, 0f);
        while (t < 1f)
        {
            t += Time.deltaTime / swingTime;
            float k = swingCurve.Evaluate(Mathf.Clamp01(t));
            transform.localRotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        // Return
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / swingTime;
            float k = swingCurve.Evaluate(Mathf.Clamp01(t));
            transform.localRotation = Quaternion.Slerp(end, start, k);
            yield return null;
        }
        transform.localRotation = start;
    }
}
