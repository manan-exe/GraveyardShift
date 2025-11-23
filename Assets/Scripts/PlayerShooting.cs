using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Camera playerCamera;
    public Transform muzzlePoint;          // End of gun barrel (for reference if needed)

    [Header("Gun Settings")]
    public float fireRate = 0.2f;
    public float damage = 25f;
    public float maxShootDistance = 100f;
    [Tooltip("Layers to hit (e.g. Zombies + Environment).")]
    public LayerMask hitMask;

    [Header("Muzzle Flash (Optional)")]
    public Light muzzleLight;              // Simple light flash at muzzle
    public float muzzleFlashDuration = 0.05f;
    public ParticleSystem muzzleFlash;     // Optional particle flash (Play On Awake OFF)

    [Header("Hit Flash (Optional)")]
    public float hitFlashDuration = 0.05f;
    public float hitFlashRange = 0.3f;
    public float hitFlashIntensity = 6f;
    public Color hitFlashColor = Color.white;
    public GameObject hitImpactPrefab;     // Optional visual prefab at hit point

    [Header("Stats (read-only at runtime)")]
    public int shotsFired;
    public int shotsHit;

    private float nextFireTime = 0f;
    private bool isAiming;

    void Start() {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update() {
        HandleAimInput();
        HandleShootInput();
    }

    void HandleAimInput() {
        if (animator == null) return;

        // Hold right mouse to aim
        bool newIsAiming = Input.GetMouseButton(1);
        if (newIsAiming != isAiming)
        {
            isAiming = newIsAiming;
            animator.SetBool("IsAiming", isAiming);

            // If you're using an AimLayer with a mask:
            int aimLayerIndex = animator.GetLayerIndex("AimLayer");
            if (aimLayerIndex >= 0)
            {
                animator.SetLayerWeight(aimLayerIndex, isAiming ? 1f : 0f);
            }
        }
    }

    void HandleShootInput() {
        if (!isAiming) return;
        if (playerCamera == null) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            FireShot();
        }
    }

    void FireShot() {
        nextFireTime = Time.time + fireRate;
        shotsFired++;

        // Trigger shoot animation
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }

        // Muzzle light flash (simple)
        if (muzzleLight != null)
        {
            StartCoroutine(MuzzleFlash());
        }

        // Optional particle muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Raycast from camera center
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxShootDistance, hitMask))
        {
            shotsHit++;

            // Damage zombie if present
            ZombieTypeA zombie = hit.collider.GetComponentInParent<ZombieTypeA>(); if (zombie != null)
            {
                zombie.TakeDamage(damage);
            }

            // Optional impact prefab (e.g. tiny spark) at hit point
            if (hitImpactPrefab != null)
            {
                Instantiate(
                    hitImpactPrefab,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );
            }

            // Optional hit light flash
            StartCoroutine(HitFlash(hit.point, hit.normal));
        }
        else
        {
            // Missed: shotsFired incremented, shotsHit unchanged
        }
    }

    IEnumerator MuzzleFlash() {
        if (muzzleLight == null) yield break;

        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
    }

    IEnumerator HitFlash(Vector3 position, Vector3 normal) {
        if (playerCamera == null)
            yield break;

        // Direction from camera to hit point
        Vector3 fromCameraDir = (position - playerCamera.transform.position).normalized;

        // Move the flash slightly toward the camera so it doesn't clip into the surface
        float offset = 0.15f; // tweak this if needed
        Vector3 spawnPos = position - fromCameraDir * offset;

        GameObject flash = new GameObject("HitFlash");
        flash.transform.position = spawnPos;

        // Optional: face the camera
        flash.transform.rotation = Quaternion.LookRotation(fromCameraDir);

        Light l = flash.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = hitFlashRange;
        l.intensity = hitFlashIntensity;
        l.color = hitFlashColor;
        l.shadows = LightShadows.None;

        // (Optional debug) little sphere so you can see where it is while tuning
        /*
        GameObject viz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        viz.transform.SetParent(flash.transform, worldPositionStays: false);
        viz.transform.localScale = Vector3.one * 0.05f;
        Destroy(viz.GetComponent<Collider>());
        */

        yield return new WaitForSeconds(hitFlashDuration);

        Destroy(flash);
    }

}
