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