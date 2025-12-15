using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Tooltip("How quickly horizontal speed decays when grounded and no input.")]
    public float groundFriction = 20f;

    [Header("Landing Horizontal Kill")]
    [Tooltip("Seconds after pressing jump before horizontal movement is killed.")]
    public float killHorizontalDelay = 0.0f;

    [Tooltip("How long horizontal movement stays killed.")]
    public float killHorizontalDuration = 0.18f;

    private float killHorizontalStartTime = -999f;
    private float killHorizontalEndTime = -999f;

    private bool wasGrounded;

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    [Header("Audio - Movement")]
    public AudioSource movementAudioSource;
    public AudioClip walkLoop;
    public AudioClip runLoop;
    [Range(0f, 1f)] public float movementVolume = 1f;

    void Awake() {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update() {
        // --- INPUT ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);
        bool hasInput = input.sqrMagnitude > 0.001f;

        // --- CAMERA RELATIVE DIRS ---
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // Camera forward on XZ for aiming rotation
        Vector3 camForwardXZ = camForward;

        // --- GROUND CHECK ---
        bool isGrounded = CheckGrounded();

        // --- ANIMATOR STATE ---
        bool isAiming = animator != null && animator.GetBool("IsAiming");

        // --- SPRINTING ---
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasInput;
        if (isAiming) isSprinting = false;

        // --- MOVEMENT AUDIO ---
        if (movementAudioSource != null)
        {
            // stop sound if not moving or gameplay inactive
            if (!hasInput || !GameFlowManager.GameplayActive || GameFlowManager.IsPaused)
            {
                if (movementAudioSource.isPlaying)
                    movementAudioSource.Stop();
            }
            else
            {
                AudioClip desiredClip = isSprinting ? runLoop : walkLoop;

                if (desiredClip != null)
                {
                    movementAudioSource.loop = true;
                    movementAudioSource.volume = movementVolume;

                    if (movementAudioSource.clip != desiredClip)
                    {
                        movementAudioSource.clip = desiredClip;
                        movementAudioSource.Play();
                    }
                    else if (!movementAudioSource.isPlaying)
                    {
                        movementAudioSource.Play();
                    }
                }
            }
        }

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // --- KILL-HORIZONTAL WINDOW ---
        bool inKillWindow = Time.time >= killHorizontalStartTime &&
                            Time.time <= killHorizontalEndTime;

        // --- HORIZONTAL MOVEMENT ---
        if (!inKillWindow)
        {
            if (hasInput)
            {
                horizontalVelocity = moveDir * currentSpeed;
            }
            else
            {
                // Apply ground friction ONLY when grounded
                if (isGrounded)
                {
                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        Vector3.zero,
                        groundFriction * Time.deltaTime
                    );
                }
            }
        }
        else
        {
            // Movement ignored during kill window
            horizontalVelocity = Vector3.zero;
        }

        // --- ROTATION ---
        if (isAiming)
        {
            // Face camera while aiming
            if (camForwardXZ.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camForwardXZ);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else if (!inKillWindow)
        {
            // Movement-based facing only outside kill window
            if (hasInput && moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // --- JUMP ---
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
                verticalVelocity = jumpSpeed;

                if (animator != null)
                    animator.SetTrigger("Jump");

                // Start new kill window
                killHorizontalStartTime = Time.time + killHorizontalDelay;
                killHorizontalEndTime = killHorizontalStartTime + killHorizontalDuration;
            }
        }

        // --- GRAVITY ---
        verticalVelocity += gravity * Time.deltaTime;

        // --- APPLY MOVEMENT ---
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // --- ANIMATION ---
        if (animator != null)
        {
            float maxSpeed = moveSpeed * sprintMultiplier;
            float speedPercent = horizontalVelocity.magnitude / maxSpeed;

            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsSprinting", isSprinting);
        }

        wasGrounded = isGrounded;
    }

    bool CheckGrounded() {
        float rayLength = controller.height / 2f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }
}




//version 3 ---------------------------------------------------------------------------------------
/*
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Tooltip("How quickly horizontal speed decays when grounded and no input.")]
    public float groundFriction = 20f;

    [Header("Animation")]
    public Animator animator;   // drag in inspector (or auto-found)

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool wasGrounded;

    void Awake() {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update() {
        // -------- Input --------
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);
        bool hasInput = input.sqrMagnitude > 0.001f;

        // -------- Camera-relative dirs --------
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // Camera forward on XZ plane (for aiming rotation)
        Vector3 cameraForwardOnPlane = camForward; // already flattened and normalized

        // -------- Grounding --------
        bool isGrounded = CheckGrounded();

        // -------- Animator aiming state --------
        bool isAiming = animator != null && animator.GetBool("IsAiming");

        // -------- Sprinting --------
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasInput;

        // Don't allow sprinting while aiming
        if (isAiming)
        {
            isSprinting = false;
        }

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // -------- Horizontal movement --------
        if (hasInput)
        {
            // Move in camera-relative direction (works for both normal and aim)
            horizontalVelocity = moveDir * currentSpeed;
        }
        else
        {
            if (isGrounded)
            {
                // Apply ground friction when no input
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    groundFriction * Time.deltaTime
                );
            }
            // In air with no input: keep whatever horizontalVelocity we had
        }

        // -------- Rotation --------
        if (isAiming)
        {
            // While aiming, always face camera direction on XZ
            if (cameraForwardOnPlane.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(cameraForwardOnPlane);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Normal movement-based facing
            if (hasInput && moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // -------- Jump / Vertical movement --------
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // keep us "stuck" to the ground when grounded
                verticalVelocity = -2f;
            }

            if (Input.GetButtonDown("Jump"))
            {
                float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
                verticalVelocity = jumpSpeed;

                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }
            }
        }

        // Gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Final movement
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // -------- Animation params --------
        if (animator != null)
        {
            float maxSpeed = moveSpeed * sprintMultiplier;
            float speedPercent = horizontalVelocity.magnitude / maxSpeed;

            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsSprinting", isSprinting);
        }

        wasGrounded = isGrounded;
    }

    // --- Ground check helper ---
    bool CheckGrounded() {
        float rayLength = controller.height / 2f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }
}
*/



//version 2 -------------------------------------------------------------------------
/*
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Landing Horizontal Kill")]
    [Tooltip("Seconds after pressing jump before horizontal movement is killed.")]
    public float killHorizontalDelay = 0.0f;

    [Tooltip("How long horizontal movement stays killed.")]
    public float killHorizontalDuration = 0.18f;

    private float killHorizontalStartTime = -999f;
    private float killHorizontalEndTime = -999f;

    private bool wasGrounded;

    [Header("Animation")]
    public Animator animator;   // drag in inspector (or we auto-find)

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    void Awake() {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update() {
        // -------- Input --------
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);
        bool hasInput = input.sqrMagnitude > 0.001f;

        // -------- Camera-relative movement --------
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // -------- Grounding --------
        bool isGrounded = CheckGrounded();

        // -------- Horizontal kill window (relative to jump press time) --------
        bool inKillWindow = Time.time >= killHorizontalStartTime &&
                            Time.time <= killHorizontalEndTime;

        // -------- Sprinting --------
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasInput;

        if (animator != null && animator.GetBool("IsAiming"))
        {
            // Optional: don't allow sprinting while aiming
            isSprinting = false;
        }

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // -------- Horizontal movement --------
        if (!inKillWindow)
        {
            if (hasInput)
            {
                horizontalVelocity = moveDir * currentSpeed;

                // Rotate player towards movement direction
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
            else
            {
                horizontalVelocity = Vector3.zero;
            }
        }
        else
        {
            // During kill window: no movement, ignore input completely
            horizontalVelocity = Vector3.zero;
        }

        // -------- Vertical movement / Jump --------
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // keep us "stuck" to the ground when grounded
                verticalVelocity = -2f;
            }

            if (Input.GetButtonDown("Jump"))
            {
                float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
                verticalVelocity = jumpSpeed;

                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }

                // Schedule horizontal kill window based on this jump press
                killHorizontalStartTime = Time.time + killHorizontalDelay;
                killHorizontalEndTime = killHorizontalStartTime + killHorizontalDuration;
            }
        }

        // Gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Final movement
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // -------- Animation --------
        if (animator != null)
        {
            float maxSpeed = moveSpeed * sprintMultiplier;
            float speedPercent = horizontalVelocity.magnitude / maxSpeed;

            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsSprinting", isSprinting);
        }

        wasGrounded = isGrounded;
    }

    // --- Ground check helper ---
    bool CheckGrounded() {
        float rayLength = controller.height / 2f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }
}
*/







//version 1 ------------------------------------------------------------------------------
/*
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Landing Control")]
    [Tooltip("How long after pressing jump until horizontal velocity kill begins.")]
    public float landingDelay = 0.15f;

    [Tooltip("How long horizontal velocity stays zero once kill begins.")]
    public float landingKillDuration = 0.15f;

    private float landingTimer = -1f;
    private float landingKillTimer = -1f;

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool wasGrounded;

    void Awake() {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update() {
        // -------- Input --------
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;
        bool hasInput = input.sqrMagnitude > 0.001f;

        // -------- Camera-relative movement --------
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // -------- Ground detection --------
        bool isGrounded = CheckGrounded();
        bool justLanded = !wasGrounded && isGrounded && verticalVelocity <= 0f;

        if (justLanded)
        {
            // When we actually hit the ground, begin the kill timer *if*
            // the pre-delay window completed.
            if (landingTimer > 0f)
                landingKillTimer = landingKillDuration;

            landingTimer = -1f; // landing phase over
        }

        // -------- Sprinting --------
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasInput;
        if (animator != null && animator.GetBool("IsAiming"))
            isSprinting = false;

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // -------- Handle movement --------
        if (landingKillTimer > 0f)
        {
            // Kill horizontal motion completely
            horizontalVelocity = Vector3.zero;
            landingKillTimer -= Time.deltaTime;
        }
        else
        {
            if (hasInput)
            {
                horizontalVelocity = moveDir * currentSpeed;

                // Rotate player towards movement direction
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
            else if (isGrounded)
            {
                // Normal ground friction
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    20f * Time.deltaTime
                );
            }
        }

        // -------- Jumping --------
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                // Begin "landing delay" timer as soon as we jump
                landingTimer = landingDelay;

                float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
                verticalVelocity = jumpSpeed;

                if (animator != null)
                    animator.SetTrigger("Jump");
            }
        }

        // -------- Timers --------
        if (landingTimer > 0f)
            landingTimer -= Time.deltaTime;

        // -------- Gravity --------
        verticalVelocity += gravity * Time.deltaTime;

        // -------- Apply movement --------
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // -------- Animation --------
        if (animator != null)
        {
            float maxSpeed = moveSpeed * sprintMultiplier;
            float speedPercent = horizontalVelocity.magnitude / maxSpeed;

            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsSprinting", isSprinting);
        }

        wasGrounded = isGrounded;
    }

    // --- Ground check ---
    bool CheckGrounded() {
        float rayLength = controller.height / 2f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }
}
*/
