using System.Collections;
using UnityEngine;

public class GunWeapon : WeaponBase
{
    [Header("Gun")]
    public int damage = 20;
    public float range = 50f;
    public LayerMask hitMask;
    public int ammoInClip = 10;
    public int clipSize = 10;
    public float reloadTime = 1.2f;
    private bool reloading;
    private Transform muzzle;
    private Vector3 defaultLocalPos;
    private bool recoiling;
    [Header("Aiming")]
    [Tooltip("Use camera-centered ray for TPS aiming; if false, use owner.forward.")]
    public bool useCameraAim = true;
    [Tooltip("If using camera aim, the ray will target the point at the center of screen, then direction is from muzzle to that point.")]
    public bool alignFromMuzzleToAimPoint = true;
    [Header("FX (Optional)")]
    [Tooltip("If a ParticleSystem exists under Muzzle named 'MuzzleFlash', play it on shoot.")]
    public bool playMuzzleParticle = true;
    [Tooltip("Draw a short debug ray each shot to visualize the direction (Editor only)")]
    public bool debugDrawRay = true;
    private ParticleSystem muzzleFx;

    public override void OnEquip(Transform hand)
    {
        transform.SetParent(hand, worldPositionStays: false);
        transform.localPosition = gripLocalPosition;
        transform.localRotation = Quaternion.Euler(gripLocalEuler);
        // Try find a child named "Muzzle" for shooting origin; fallback to self
        muzzle = transform.Find("Muzzle");
        if (muzzle == null) muzzle = transform;
        defaultLocalPos = transform.localPosition;

        if (playMuzzleParticle && muzzle != null)
        {
            var fx = muzzle.GetComponentInChildren<ParticleSystem>(true);
            if (fx != null && fx.name.ToLower().Contains("muzzle"))
            {
                muzzleFx = fx;
            }
        }
    }

    public override void OnUnequip()
    {
        transform.SetParent(null);
    }

    public override bool TryAttack(Transform owner)
    {
        if (reloading) return false;
        if (Time.time - lastAttack < attackCooldown) return false;
        if (ammoInClip <= 0) { StartCoroutine(Reload()); return false; }
        lastAttack = Time.time;
        ammoInClip--;

        Vector3 origin = muzzle != null ? muzzle.position : owner.position + Vector3.up * 1.2f;
        Vector3 dir = owner.forward;

        if (useCameraAim && Camera.main != null)
        {
            var cam = Camera.main;
            Ray camRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Find the first hit that is NOT part of the owner (skip body/collider of player)
            var camHits = Physics.RaycastAll(camRay, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(camHits, (a, b) => a.distance.CompareTo(b.distance));
            Vector3 aimPoint = camRay.origin + cam.transform.forward * range;
            foreach (var h in camHits)
            {
                if (owner != null && h.collider.transform.IsChildOf(owner))
                    continue; // skip self
                aimPoint = h.point;
                break;
            }

            // Direction from muzzle to aim point (prevents shooting backwards)
            if (alignFromMuzzleToAimPoint)
            {
                dir = (aimPoint - origin).normalized;
            }
            else
            {
                dir = cam.transform.forward;
            }

            // Safety: if something made dir point opposite camera forward, clamp to camera forward
            if (Vector3.Dot(dir, cam.transform.forward) < 0f)
            {
                dir = cam.transform.forward;
            }
        }

        // Small forward bias from muzzle to avoid self-intersection with near colliders (e.g., shoulder)
        origin += dir * 0.02f;

        // Visualize shot direction briefly in editor
        if (debugDrawRay)
        {
            Debug.DrawRay(origin, dir * range, Color.yellow, 0.1f);
        }

        // Layer mask fallback: if not set in inspector, use default
        int maskVal = hitMask.value != 0 ? hitMask.value : Physics.DefaultRaycastLayers;
        if (debugLogHits && hitMask.value == 0)
        {
            Debug.Log("[GunWeapon] hitMask not set; using Physics.DefaultRaycastLayers", this);
        }

        // Raycast and ignore the owner colliders to prevent self-hit in TPS; include triggers
        var hits = Physics.RaycastAll(origin, dir, range, maskVal, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(owner))
            {
                continue; // skip self
            }

            var hp = h.collider.GetComponentInParent<Health>() ?? h.collider.GetComponent<Health>();
            if (hp != null && !hp.IsDead)
            {
                hp.TakeDamage(damage);
                if (debugLogHits)
                {
                    Debug.Log($"[GunWeapon] Hit {h.collider.name} for {damage}. Target HP: {hp.CurrentHealth}");
                }
                // play muzzle flash if available
                if (muzzleFx != null && playMuzzleParticle)
                {
                    muzzleFx.Play(true);
                }
            }
            // stop at the first non-owner collider we hit (wall/enemy)
            break;
        }

        if (debugLogHits && (hits == null || hits.Length == 0))
        {
            Debug.Log("[GunWeapon] No hit detected. Check hitMask includes Enemy layer and that enemy has Collider.", this);
        }

        // Simple procedural recoil
        if (!recoiling) StartCoroutine(Recoil());
        return true;
    }

    private IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammoInClip = clipSize;
        reloading = false;
    }

    private IEnumerator Recoil()
    {
        recoiling = true;
        float t = 0f;
        float dur = 0.06f;
        Vector3 kick = new Vector3(0, 0, -0.05f);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.Lerp(defaultLocalPos, defaultLocalPos + kick, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.Lerp(defaultLocalPos + kick, defaultLocalPos, t);
            yield return null;
        }
        transform.localPosition = defaultLocalPos;
        recoiling = false;
    }
}
