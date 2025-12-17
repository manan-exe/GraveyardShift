using System.Collections;
using UnityEngine;

//using raycasts for shooting instead of 3D projectiles
public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    //player animator
    public Animator animator;
    //player camera
    public Camera playerCamera;
    //tip of gun for flash when shooting
    public Transform muzzlePoint;

    [Header("Gun Settings")]
    //firing cooldown
    public float fireRate = 0.2f;
    //damage per bullet
    public float damage = 25f;
    //max shooting distance
    public float maxShootDistance = 100f;
    //what layers can get hit. had to do this because shooting raycast was somehow hitting the player
    public LayerMask hitMask;

    //abandoned feature for accuracy. too much stuff cluttering the UI
    [Header("Accuracy")]
    [Tooltip("0 = perfectly accurate. Small values like 0.5-1.5 feel more natural.")]
    public float spreadDegrees = 0f;

    //flash from the gun firing
    [Header("Muzzle Flash")]
    public Light muzzleLight;
    public float muzzleFlashDuration = 0.05f;
    public ParticleSystem muzzleFlash;

    //flash where the bullet lands
    [Header("Hit Flash")]
    public float hitFlashDuration = 0.05f;
    public float hitFlashRange = 0.3f;
    public float hitFlashIntensity = 6f;
    public Color hitFlashColor = Color.white;
    public GameObject hitImpactPrefab;

    //summary statistics. abandoned
    [Header("Stats (read-only at runtime)")]
    public int shotsFired;
    public int shotsHit;

    //cooldown for firing
    private float nextFireTime;
    //cannot shoot unless first aiming
    private bool isAiming;

    private Coroutine muzzleFlashRoutine;
    //created to display hit flash
    private GameObject hitFlashObject;
    //component for flash
    private Light hitFlashLight;
    //muzzle flash and hit flash are different things
    //muzzle flash is at origin
    //hit flash is where the bullet lands
    private Coroutine hitFlashRoutine;

    //layer for effects
    private int vfxLayer = -1;

    [Header("Audio")]
    //shooting audio source, audio, and volume control
    public AudioSource gunAudioSource;
    public AudioClip shootSfx;
    [Range(0f, 1f)] public float shootVolume = 1f;

    void Start() {
        //get animator if not already given
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        //get camera
        if (playerCamera == null)
            playerCamera = Camera.main;

        //create layer for effects
        vfxLayer = LayerMask.NameToLayer("VFX");

        //try to control flash with multiple gunshots
        if (fireRate > 0f)
        {
            muzzleFlashDuration = Mathf.Min(muzzleFlashDuration, fireRate * 0.5f);
            hitFlashDuration = Mathf.Min(hitFlashDuration, fireRate * 0.5f);
        }
    }


    void Update() {
        //read if player is aiming
        HandleAimInput();
        //read if player trying to shoot
        HandleShootInput();
    }

    void HandleAimInput() {
        //do nothing if animator does not exist (it should exist)
        if (animator == null) return;

        //right click makes the character aim
        bool newIsAiming = Input.GetMouseButton(1);
        //if the aiming input differs from what is currently executing
        if (newIsAiming != isAiming)
        {
            //set to true or false based on if right click is being held
            isAiming = newIsAiming;
            //show the aiming animation
            animator.SetBool("IsAiming", isAiming);

            //aim layer controls upper body of character
            //differs from lower body so that we can show the aim animation and the walking animation
            //  at the same time but independent of eachother
            int aimLayerIndex = animator.GetLayerIndex("AimLayer");
            //if the layer exists then adjust it
            if (aimLayerIndex >= 0)
                animator.SetLayerWeight(aimLayerIndex, isAiming ? 1f : 0f);
        }
    }

    //shooting can only happen if you press left click WHILE holding right click to aim
    void HandleShootInput() {
        //if not holding right click then do nothing because you cant shoot
        if (!isAiming) return;
        //if camera does not exist you have bigger problems to figure out so do nothing
        if (playerCamera == null) return;

        //if you press left click and no cooldown then shoot
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            FireShot();
    }

    //actual shooting logic
    void FireShot() {
        //schedule cooldown
        nextFireTime = Time.time + fireRate;
        //statistics
        shotsFired++;

        //do shoot animation on upper animation mask
        if (animator != null)
            animator.SetTrigger("Shoot");

        //play shooting sound
        if (gunAudioSource != null && shootSfx != null)
            gunAudioSource.PlayOneShot(shootSfx, shootVolume);

        //show the gun flash
        PlayMuzzleFlash();

        //create the ray
        Vector3 origin = (muzzlePoint != null) ? muzzlePoint.position : playerCamera.transform.position;

        //make ray in direction of player
        Vector3 dir = playerCamera.transform.forward;

        //bullet spread. but we are not implementing this since we did not get to other firearms
        if (spreadDegrees > 0f)
            dir = ApplySpread(dir, spreadDegrees);

        //create the ray
        Ray ray = new Ray(origin, dir);

        //if something gets hit
        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            //increment statistics
            shotsHit++;
            //deal damage to zombie
            DealDamage(hit.collider, damage);

            //would display at the location if something got hit but we did not add anything
            if (hitImpactPrefab != null)
            {
                GameObject impact = Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (vfxLayer != -1) SetLayerRecursively(impact, vfxLayer);
            }

            //flash at place where bullet would have landed
            PlayHitFlash(hit.point);
        }
    }

    //deal damage to zombie objects
    void DealDamage(Collider col, float amount) {
        //only deals damage to zombies. does not try to deal damage to environment or anything
        ZombieAIBase ai = col.GetComponentInParent<ZombieAIBase>();
        if (ai != null)
        {
            ai.TakeDamage(amount);
            return;
        }

        //for old zombies
        ZombieOld oldA = col.GetComponentInParent<ZombieOld>();
        if (oldA != null)
        {
            oldA.TakeDamage(amount);
            return;
        }
    }

    //light particle system
    void PlayMuzzleFlash() {
        //if muzzle light exists
        if (muzzleLight != null)
        {
            //use effects layer if we have it
            if (vfxLayer != -1) muzzleLight.gameObject.layer = vfxLayer;

            //stop any previous gun flashes
            if (muzzleFlashRoutine != null)
                StopCoroutine(muzzleFlashRoutine);

            //start new gun flash
            muzzleFlashRoutine = StartCoroutine(MuzzleFlash());
        }

        //if flash already exists then stop and replay
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }
    }

    //enables and disables muzzle light very quickly
    IEnumerator MuzzleFlash() {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
    }

    //light at point of impact
    void PlayHitFlash(Vector3 position) {
        //if camera not here there is other issue
        if (playerCamera == null) return;

        //direction from camera to point of impact
        Vector3 fromCameraDir = (position - playerCamera.transform.position).normalized;
        //bring toward camera slightly or else the light will be inside the object and then you cant see it
        Vector3 spawnPos = position - fromCameraDir * 0.15f;

        //create flash object if it does not exist
        if (hitFlashObject == null)
        {
            //create
            hitFlashObject = new GameObject("HitFlash");
            //put on layer if layer exists
            if (vfxLayer != -1) hitFlashObject.layer = vfxLayer;

            //make light component
            hitFlashLight = hitFlashObject.AddComponent<Light>();
            //make point light
            hitFlashLight.type = LightType.Point;
            //no need for shadow
            hitFlashLight.shadows = LightShadows.None;
            //start disabled and function will quickly toggle on and off
            hitFlashLight.enabled = false;
        }

        //move hit flash to specified position
        hitFlashObject.transform.position = spawnPos;
        //rotate toward camera
        hitFlashObject.transform.rotation = Quaternion.LookRotation(fromCameraDir);

        //in unity inspector
        //configured light range
        hitFlashLight.range = hitFlashRange;
        //configured light intensity
        hitFlashLight.intensity = hitFlashIntensity;
        //configured light color
        hitFlashLight.color = hitFlashColor;

        //stop a previous hit flash
        if (hitFlashRoutine != null)
            StopCoroutine(hitFlashRoutine);

        //start new hit flash
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    //quickly toggle on and off the light where bullet lands
    IEnumerator HitFlashRoutine() {
        hitFlashLight.enabled = true;
        yield return new WaitForSeconds(hitFlashDuration);
        hitFlashLight.enabled = false;
    }

    //was supposed to be bullet spread randomization but not doing this
    Vector3 ApplySpread(Vector3 direction, float degrees) {
        //offsets
        float yaw = Random.Range(-degrees, degrees);
        float pitch = Random.Range(-degrees, degrees);
        //apply offsets for rotation
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        //apply to direction vector
        return rot * direction;
    }

    //set effects layer
    void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
