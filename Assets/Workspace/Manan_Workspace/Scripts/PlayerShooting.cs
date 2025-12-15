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

    [Header("Accuracy (Optional)")]
    [Tooltip("0 = perfectly accurate. Small values like 0.5-1.5 feel more natural.")]
    public float spreadDegrees = 0f;

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

    private float nextFireTime;
    private bool isAiming;

    private Coroutine muzzleFlashRoutine;

    private GameObject hitFlashObject;
    private Light hitFlashLight;
    private Coroutine hitFlashRoutine;

    private int vfxLayer = -1;

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioClip shootSfx;
    [Range(0f, 1f)] public float shootVolume = 1f;

    void Start() {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        vfxLayer = LayerMask.NameToLayer("VFX"); // returns -1 if not found (safe)

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

        if (animator != null)
            animator.SetTrigger("Shoot");

        if (gunAudioSource != null && shootSfx != null)
            gunAudioSource.PlayOneShot(shootSfx, shootVolume);

        PlayMuzzleFlash();

        // Build ray
        Vector3 origin = (muzzlePoint != null) ? muzzlePoint.position : playerCamera.transform.position;

        Vector3 dir = playerCamera.transform.forward;
        if (spreadDegrees > 0f)
            dir = ApplySpread(dir, spreadDegrees);

        Ray ray = new Ray(origin, dir);

        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            shotsHit++;

            DealDamage(hit.collider, damage);

            if (hitImpactPrefab != null)
            {
                GameObject impact = Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (vfxLayer != -1) SetLayerRecursively(impact, vfxLayer);
            }

            PlayHitFlash(hit.point);
        }
    }

    void DealDamage(Collider col, float amount) {
        // Preferred: your new shared zombie base class
        ZombieAIBase ai = col.GetComponentInParent<ZombieAIBase>();
        if (ai != null)
        {
            ai.TakeDamage(amount);
            return;
        }

        // Fallback: your old zombies (so nothing breaks mid-transition)
        ZombieOld oldA = col.GetComponentInParent<ZombieOld>();
        if (oldA != null)
        {
            oldA.TakeDamage(amount);
            return;
        }

        // If you still have ZombieTypeB as a separate script later, add here.
        // ZombieTypeB oldB = col.GetComponentInParent<ZombieTypeB>();
        // if (oldB != null) oldB.TakeDamage(amount);
    }

    void PlayMuzzleFlash() {
        if (muzzleLight != null)
        {
            if (vfxLayer != -1) muzzleLight.gameObject.layer = vfxLayer;

            if (muzzleFlashRoutine != null)
                StopCoroutine(muzzleFlashRoutine);

            muzzleFlashRoutine = StartCoroutine(MuzzleFlash());
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }
    }

    IEnumerator MuzzleFlash() {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
    }

    void PlayHitFlash(Vector3 position) {
        if (playerCamera == null) return;

        Vector3 fromCameraDir = (position - playerCamera.transform.position).normalized;
        Vector3 spawnPos = position - fromCameraDir * 0.15f;

        if (hitFlashObject == null)
        {
            hitFlashObject = new GameObject("HitFlash");
            if (vfxLayer != -1) hitFlashObject.layer = vfxLayer;

            hitFlashLight = hitFlashObject.AddComponent<Light>();
            hitFlashLight.type = LightType.Point;
            hitFlashLight.shadows = LightShadows.None;
            hitFlashLight.enabled = false;
        }

        hitFlashObject.transform.position = spawnPos;
        hitFlashObject.transform.rotation = Quaternion.LookRotation(fromCameraDir);

        hitFlashLight.range = hitFlashRange;
        hitFlashLight.intensity = hitFlashIntensity;
        hitFlashLight.color = hitFlashColor;

        if (hitFlashRoutine != null)
            StopCoroutine(hitFlashRoutine);

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine() {
        hitFlashLight.enabled = true;
        yield return new WaitForSeconds(hitFlashDuration);
        hitFlashLight.enabled = false;
    }

    Vector3 ApplySpread(Vector3 direction, float degrees) {
        // Random small rotation around up/right for simple cone spread
        float yaw = Random.Range(-degrees, degrees);
        float pitch = Random.Range(-degrees, degrees);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return rot * direction;
    }

    void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
