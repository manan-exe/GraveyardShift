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

        // Muzzle flash light
        if (muzzleLight != null)
            StartCoroutine(MuzzleFlash());

        // Muzzle flash particles
        if (muzzleFlash != null)
            muzzleFlash.Play();

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

            // Spawn hit impact and move to VFX layer
            if (hitImpactPrefab != null)
            {
                GameObject impact = Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                SetLayerRecursively(impact, LayerMask.NameToLayer("VFX"));
            }

            StartCoroutine(HitFlash(hit.point, hit.normal));
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

    IEnumerator HitFlash(Vector3 position, Vector3 normal) {
        if (playerCamera == null)
            yield break;

        Vector3 fromCameraDir = (position - playerCamera.transform.position).normalized;
        float offset = 0.15f;
        Vector3 spawnPos = position - fromCameraDir * offset;

        GameObject flash = new GameObject("HitFlash");
        flash.layer = LayerMask.NameToLayer("VFX");  // Ensure it never gets hit
        flash.transform.position = spawnPos;
        flash.transform.rotation = Quaternion.LookRotation(fromCameraDir);

        Light l = flash.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = hitFlashRange;
        l.intensity = hitFlashIntensity;
        l.color = hitFlashColor;
        l.shadows = LightShadows.None;

        yield return new WaitForSeconds(hitFlashDuration);

        Destroy(flash);
    }

    // Utility: put hit effects & all children on VFX layer
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
