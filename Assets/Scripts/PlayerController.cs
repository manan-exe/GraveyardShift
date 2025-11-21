using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Animation")]
    public Animator animator;   // drag in inspector (or we auto-find)

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    void Awake() {
        controller = GetComponent<CharacterController>();

        // Try to auto-find animator if not set
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

        if (hasInput)
        {
            horizontalVelocity = moveDir * moveSpeed;

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

        // -------- Gravity / Grounding --------
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            // small negative keeps us "glued" to ground
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // -------- Animation --------
        if (animator != null)
        {
            // value between 0 and 1, used for blend/transition
            float speedPercent = horizontalVelocity.magnitude / moveSpeed;

            // Damped for smoother transitions
            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }
}
