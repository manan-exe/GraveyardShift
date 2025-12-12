using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Camera playerCamera;
    public Transform muzzlePoint;

    [Header("Gun Settings")]
    public float fireRate = 0.2f;
    public float damage = 25f;
    public float maxShootDistance = 100f;
    public LayerMask hitMask;

    [Header("Muzzle Flash (Optional)")]
    public Light muzzleLight;
    public float muzzleFlashDuration = 0.05f;
    public ParticleSystem muzzleFlash;

    [Header("Hit Flash (Optional)")]
    public float hitFlashDuration = 0.05f;
    public float hitFlashRange = 0.3f;
    public float hitFlashIntensity = 6f;
    public Color hitFlashColor = Color.white;
    public GameObject hitImpactPrefab;

    [Header("Stats (read-only at runtime)")]
    public int shotsFired;
    public int shotsHit;

    private float nextFireTime = 0f;
    private bool isAiming;

    // --- NEW: tracking for muzzle flash coroutine ---
    private Coroutine muzzleFlashRoutine;

    // --- NEW: reusable hit flash light & coroutine ---
    private GameObject hitFlashObject;
    private Light hitFlashLight;
    private Coroutine hitFlashRoutine;

    void Start() {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        // Keep flashes reasonably short vs fire rate
        if (fireRate > 0f)
        {
            muzzleFlashDuration = Mathf.Min(muzzleFlashDuration, fireRate * 0.5f);
            hitFlashDuration = Mathf.Min(hitFlashDuration, fireRate * 0.5f);
        }
    }

    void Update() {
        HandleAimInput();
        HandleShootInput();
    }

    void HandleAimInput() {
        if (animator == null) return;

        bool newIsAiming = Input.GetMouseButton(1);
        if (newIsAiming != isAiming)
        {
            isAiming = newIsAiming;
            animator.SetBool("IsAiming", isAiming);

            int aimLayerIndex = animator.GetLayerIndex("AimLayer");
            if (aimLayerIndex >= 0)
                animator.SetLayerWeight(aimLayerIndex, isAiming ? 1f : 0f);
        }
    }

    void HandleShootInput() {
        if (!isAiming) return;
        if (playerCamera == null) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            FireShot();
    }

    void FireShot() {
        nextFireTime = Time.time + fireRate;
        shotsFired++;

        // Animation
        if (animator != null)
            animator.SetTrigger("Shoot");

        // --- MUZZLE FLASH LIGHT (no stacking) ---
        if (muzzleLight != null)
        {
            if (muzzleFlashRoutine != null)
                StopCoroutine(muzzleFlashRoutine);

            muzzleFlashRoutine = StartCoroutine(MuzzleFlash());
        }

        // --- MUZZLE FLASH PARTICLES (single non-looping burst) ---
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

        // Raycast shot
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxShootDistance, hitMask))
        {
            shotsHit++;

            // Damage ZombieTypeA
            ZombieTypeA zombieA = hit.collider.GetComponentInParent<ZombieTypeA>();
            if (zombieA != null)
                zombieA.TakeDamage(damage);

            // Damage ZombieTypeB
            ZombieTypeB zombieB = hit.collider.GetComponentInParent<ZombieTypeB>();
            if (zombieB != null)
                zombieB.TakeDamage(damage);

            // Spawn hit impact VFX (decals/particles)
            if (hitImpactPrefab != null)
            {
                GameObject impact = Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                SetLayerRecursively(impact, LayerMask.NameToLayer("VFX"));
            }

            // Reusable timed hit flash at impact
            PlayHitFlash(hit.point);
        }
    }

    IEnumerator MuzzleFlash() {
        if (muzzleLight == null) yield break;

        // Put muzzle flash on VFX layer so raycasts ignore it
        muzzleLight.gameObject.layer = LayerMask.NameToLayer("VFX");

        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
    }

    // --- REUSABLE HIT FLASH LOGIC ---

    void PlayHitFlash(Vector3 position) {
        if (playerCamera == null)
            return;

        // Slightly pull the flash towards the camera so it doesn't clip into walls
        Vector3 fromCameraDir = (position - playerCamera.transform.position).normalized;
        float offset = 0.15f;
        Vector3 spawnPos = position - fromCameraDir * offset;

        // Create the flash object once, then reuse it
        if (hitFlashObject == null)
        {
            hitFlashObject = new GameObject("HitFlash");
            hitFlashObject.layer = LayerMask.NameToLayer("VFX");
            hitFlashLight = hitFlashObject.AddComponent<Light>();
            hitFlashLight.type = LightType.Point;
            hitFlashLight.shadows = LightShadows.None;
        }

        hitFlashObject.transform.position = spawnPos;
        hitFlashObject.transform.rotation = Quaternion.LookRotation(fromCameraDir);

        hitFlashLight.range = hitFlashRange;
        hitFlashLight.intensity = hitFlashIntensity;
        hitFlashLight.color = hitFlashColor;

        // Restart the flash timer so it lines up with the current shot
        if (hitFlashRoutine != null)
            StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine() {
        if (hitFlashLight == null) yield break;

        hitFlashLight.enabled = true;
        yield return new WaitForSeconds(hitFlashDuration);
        hitFlashLight.enabled = false;
    }

    // Utility: put hit effects & all children on VFX layer
    void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
