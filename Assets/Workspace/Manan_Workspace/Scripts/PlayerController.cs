using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    //walking speed
    public float moveSpeed = 4f;
    //affects sprinting speed
    public float sprintMultiplier = 1.8f;
    //rotation speed to face camera
    public float rotationSpeed = 10f;
    //gravity
    public float gravity = -9.81f;
    //jump height
    public float jumpHeight = 1.5f;

    [Tooltip("How quickly horizontal speed decays when grounded and no input.")]
    public float groundFriction = 20f;

    //need to kill horizontal movement while jump animation finishes so that the player
    //  does not look like it is gliding since the land animaiton is playing so it cannot
    //  play the walk/run animation yet
    //this field specifically is the delay from the jump until when we should kill horizontal movement
    //this was fine tuned to match when the player gets back on the ground but the jump animation has not
    // finished yet
    [Header("Landing Horizontal Kill")]
    [Tooltip("Seconds after pressing jump before horizontal movement is killed.")]
    public float killHorizontalDelay = 0.0f;

    //how long kill horizontal movement lasts
    //very brief just to let land animation finish
    [Tooltip("How long horizontal movement stays killed.")]
    public float killHorizontalDuration = 0.18f;

    //when kill horizontal window starts and ends
    private float killHorizontalStartTime = -999f;
    private float killHorizontalEndTime = -999f;

    //not used anywhere but i do not want to break anything
    private bool wasGrounded;

    [Header("Animation")]
    //reference player animator for movement animations
    public Animator animator;

    //reference character controller for movement
    private CharacterController controller;
    //control velocity on x and z axis
    private Vector3 horizontalVelocity;
    //vertical velocity. on y axis. controlled by jump and gravity
    private float verticalVelocity;

    [Header("Audio - Movement")]
    //sound effects for walk and run
    //also volume control
    public AudioSource movementAudioSource;
    public AudioClip walkLoop;
    public AudioClip runLoop;
    [Range(0f, 1f)] public float movementVolume = 1f;

    void Awake() {
        //character controller of player object
        controller = GetComponent<CharacterController>();

        //if not assigned then get it
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update() {
        //horizontal input from A and D keys
        float h = Input.GetAxisRaw("Horizontal");
        //vertical input from W and S
        float v = Input.GetAxisRaw("Vertical");

        //x and z axis vector
        Vector3 input = new Vector3(h, 0f, v);
        //try to avoid weird diagonal movement
        input = Vector3.ClampMagnitude(input, 1f);
        //determine if player trying to move
        bool hasInput = input.sqrMagnitude > 0.001f;

        //see direction where camera facing
        Vector3 camForward = Camera.main.transform.forward;
        //see location to the right of camera
        Vector3 camRight = Camera.main.transform.right;
        //flatten vectors and ignore y axis
        camForward.y = 0;
        camRight.y = 0;
        //use unit length
        camForward.Normalize();
        camRight.Normalize();

        //convert to world space movement in relation to the camera
        Vector3 moveDir = camForward * input.z + camRight * input.x;

        //store camera forward direction
        //need this to make player turn to face where camera is facing when you start aiming
        Vector3 camForwardXZ = camForward;

        //use raycast to check player is on ground
        bool isGrounded = CheckGrounded();

        //check if character is aiming
        bool isAiming = animator != null && animator.GetBool("IsAiming");

        //sprint if left shift and a movement key is held
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasInput;
        //if aiming then you are not allowed to spring
        //you can still walk though while aiming
        if (isAiming) isSprinting = false;

        //movement audio
        //if audio source then manage movement audio
        if (movementAudioSource != null)
        {
            //no sound if not moving or gameplay not active
            if (!hasInput || !GameFlowManager.GameplayActive || GameFlowManager.IsPaused)
            {
                //stop audio if it is playing
                if (movementAudioSource.isPlaying)
                    movementAudioSource.Stop();
            }
            else
            {
                //run sound effect if sprinting or walk sound effect if walking
                AudioClip desiredClip = isSprinting ? runLoop : walkLoop;

                //checks if we have a valid clip
                if (desiredClip != null)
                {
                    //loop clip
                    movementAudioSource.loop = true;
                    //aply volume
                    movementAudioSource.volume = movementVolume;

                    //if we arent currently having the clip that we need to play assigned
                    if (movementAudioSource.clip != desiredClip)
                    {
                        //swap to clip we need to play and play it
                        movementAudioSource.clip = desiredClip;
                        movementAudioSource.Play();
                    }
                    //if we already have the clip we need then just play it
                    else if (!movementAudioSource.isPlaying)
                    {
                        movementAudioSource.Play();
                    }
                }
            }
        }

        //calculate movement speed and apply the sprint multiplier if we are sprinting
        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // determine if we are in kill horizontal window (needed for jumping)
        bool inKillWindow = Time.time >= killHorizontalStartTime &&
                            Time.time <= killHorizontalEndTime;

        //if not in window
        if (!inKillWindow)
        {
            //and want to move
            if (hasInput)
            {
                //then apply velocity for movement
                horizontalVelocity = moveDir * currentSpeed;
            }
            //if not wanting to move
            else
            {
                //if on ground
                if (isGrounded)
                {
                    //slowly kill horizontal movement so not abrupt stop
                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        Vector3.zero,
                        groundFriction * Time.deltaTime
                    );
                }
            }
        }
        //if in kill horizontal window
        else
        {
            //no horizontal movement allowed
            horizontalVelocity = Vector3.zero;
        }

        //handles rotation
        //if right click is held it means we are aiming
        if (isAiming)
        {
            //if player not pretty much facing the camera
            if (camForwardXZ.sqrMagnitude > 0.001f)
            {
                //create rotation destination
                Quaternion targetRot = Quaternion.LookRotation(camForwardXZ);
                //gradually rotate player to camera so that it is not instant because that would
                //  look not natural
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        //if not aiming and not in kill horizontal window
        else if (!inKillWindow)
        {
            //if player is moving and not pretty much facing direction of movement
            if (hasInput && moveDir.sqrMagnitude > 0.001f)
            {
                //create rotation destination
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                //gradually rotate player
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        //jump logic
        if (isGrounded)
        {
            //keep vertical velocity at negative so that we stay stuck on the ground even along slopes
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            //if space bar pressed
            if (Input.GetButtonDown("Jump"))
            {
                //calculate jump velocity
                float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
                //apply vertical velocity to make player jump
                verticalVelocity = jumpSpeed;

                //set trigger for jump animtion to play
                if (animator != null)
                    animator.SetTrigger("Jump");

                //start the timers for the kill horizontal window to make animation look natural
                killHorizontalStartTime = Time.time + killHorizontalDelay;
                killHorizontalEndTime = killHorizontalStartTime + killHorizontalDuration;
            }
        }

        //gravity increases the longer you are falling
        verticalVelocity += gravity * Time.deltaTime;

        //horizontal and vertical velocity combined into one vector and applied
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        //animation logic
        if (animator != null)
        {
            //speed computations
            float maxSpeed = moveSpeed * sprintMultiplier;
            float speedPercent = horizontalVelocity.magnitude / maxSpeed;

            //set speed value for animator
            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            //set grounded trigger for animator
            animator.SetBool("IsGrounded", isGrounded);
            //set sprinting flag for animator
            animator.SetBool("IsSprinting", isSprinting);
        }

        //set grounded status
        wasGrounded = isGrounded;
    }

    //check if player is on the ground
    bool CheckGrounded() {
        //use raycast slightly offset from player to see if we are on the ground
        //using offset so that the raycast does not just hit the player
        float rayLength = controller.height / 2f + 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);
        //returns if something was hit
        return Physics.Raycast(rayOrigin, Vector3.down, rayLength);
    }
}