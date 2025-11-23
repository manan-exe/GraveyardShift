using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ZombieTypeA : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stoppingDistance = 1.2f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundCheckOffset = 0.2f;

    [Header("Health")]
    [Tooltip("Gun currently does 25 damage. 50 HP = 2 shots.")]
    public float maxHealth = 50f;

    [Header("Target (auto-filled if empty)")]
    public Transform target;

    private CharacterController controller;
    private float verticalVelocity;
    private float currentHealth;
    private bool isDead;

    void Start() {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;

        // Auto-find player
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void Update() {
        if (isDead || target == null)
            return;

        bool isGrounded = CheckGrounded();

        // --- Gravity ---
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;  // keeps us glued to ground like your player script
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 horizontalMove = Vector3.zero;

        // --- Move toward player ---
        if (distance > stoppingDistance)
        {
            Vector3 dir = toTarget.normalized;
            horizontalMove = dir * moveSpeed;

            // Rotate toward player
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        // --- Final Movement ---
        Vector3 velocity = horizontalMove + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    bool CheckGrounded() {
        float rayLength = controller.height / 2f + groundCheckOffset;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        return Physics.Raycast(origin, Vector3.down, rayLength);
    }

    public void TakeDamage(float amount) {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            StartCoroutine(DieRoutine());
        }
    }

    IEnumerator DieRoutine() {
        isDead = true;

        // Disable controller so rotation animation works
        controller.enabled = false;

        // Disable colliders
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        // Fall forward animation
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.LookRotation(transform.forward, Vector3.down);

        float t = 0f;
        float duration = 0.3f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

}
