using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ZombieTypeB : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.2f;   // slower but still scary
    public float stoppingDistance = 1.2f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundCheckOffset = 0.2f;

    [Header("Health")]
    [Tooltip("Heavy zombie. 25 damage × 5 shots = 125 HP.")]
    public float maxHealth = 125f;   // 5-shot zombie

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
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 horizontalMove = Vector3.zero;

        // --- Movement ---
        if (distance > stoppingDistance)
        {
            Vector3 dir = toTarget.normalized;
            horizontalMove = dir * moveSpeed;

            // Rotate
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        // --- Apply movement ---
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

        controller.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

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
