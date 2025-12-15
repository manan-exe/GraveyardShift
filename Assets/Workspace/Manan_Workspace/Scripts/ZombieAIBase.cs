using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAIBase : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Combat")]
    public float maxHealth = 50f;
    public float contactDamage = 10f;
    public float attackRange = 1.4f;
    public float attackCooldown = 0.5f;

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";

    [Header("Animation Triggers")]
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";

    [Header("Death Cleanup")]
    public float corpseLifetime = 4f;
    public bool disableCollidersOnDeath = true;

    [Header("Hurt Stun")]
    public float hurtStunTime = 0.35f;
    protected float stunnedUntil;

    [Header("Stopping / Facing")]
    public float stopBuffer = 0.15f;          // extra space so they don't bump
    public float faceTurnSpeed = 12f;         // rotation smoothness

    [Header("Attack Animation")]
    public string attackTrigger = "Attack";   // trigger on UpperBody layer
    public bool stopToAttack = true;          // freeze agent while attacking

    [Header("Attack Timing")]
    public float attackHitDelay = 0.25f;     // when damage is applied after trigger
    protected bool isAttacking;

    private PlayerHealth targetHealth;


    protected NavMeshAgent agent;
    protected float currentHealth;
    protected bool isDead;
    protected float nextAttackTime;

    [Header("Audio")]
    public AudioClip deathSfx;
    [Range(0f, 1f)] public float deathSfxVolume = 1f;
    public AudioSource sfxSource;


    protected virtual void Awake() {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (target != null)
        {
            targetHealth = target.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null) targetHealth = target.GetComponentInChildren<PlayerHealth>();
            if (targetHealth == null) targetHealth = target.GetComponent<PlayerHealth>();
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }


    protected virtual void Update() {
        if (isDead || target == null) return;

        // If agent isn't valid (e.g., during death cleanup), do nothing
        if (!AgentReady())
        {
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }

        // If gameplay not active (your intro/pause gating), stop safely
        if (!GameFlowManager.GameplayActive)
        {
            agent.isStopped = true;
            agent.ResetPath();
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }

        // Hurt stun
        if (Time.time < stunnedUntil)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }
        if (isAttacking)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }


        agent.isStopped = false;

        UpdateMovement();
        TryDamagePlayer();
        UpdateAnimator();
    }

    // Implemented differently by A vs B
    protected virtual void UpdateMovement() {
        if (!AgentReady()) return;

        // XZ distance so slopes/height don't mess it up
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        agent.stoppingDistance = attackRange + stopBuffer;

        bool inRange = dist <= agent.stoppingDistance;

        if (inRange)
        {
            agent.isStopped = true;
            agent.ResetPath();
            FaceTarget();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }


    protected void TryDamagePlayer() {
        if (isAttacking) return;                 // prevents spam
        if (Time.time < nextAttackTime) return;  // cooldown gate

        // XZ distance check
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        if (dist > attackRange) return;

        // Commit to attack NOW (cooldown starts even if damage later fails)
        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }



    protected void UpdateAnimator() {
        if (animator == null) return;

        // agent.velocity magnitude is good for blend trees
        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParam, speed);
    }

    public void TakeDamage(float amount) {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth > 0f)
        {
            if (animator != null) animator.SetTrigger(hurtTrigger);

            // stun: stop moving for a moment so hurt animation looks grounded
            stunnedUntil = Time.time + hurtStunTime;
            if (agent != null && agent.enabled) agent.isStopped = true;

            return;
        }

        // Death
        currentHealth = 0f;
        isDead = true;
        StartCoroutine(DieRoutine());
    }
    IEnumerator DieRoutine() {
        isDead = true;

        if (sfxSource != null && deathSfx != null)
            sfxSource.PlayOneShot(deathSfx, deathSfxVolume);

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
            animator.SetTrigger(dieTrigger);

        if (disableCollidersOnDeath)
        {
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;
        }

        // Disable agent AFTER stopping/resetting path
        if (agent != null) agent.enabled = false;

        yield return new WaitForSeconds(corpseLifetime);
        Destroy(gameObject);
    }
    protected bool AgentReady() {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }
    protected void FaceTarget() {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, faceTurnSpeed * Time.deltaTime);
    }
    IEnumerator AttackRoutine() {
        isAttacking = true;

        // Stop + face while attacking
        if (stopToAttack && AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FaceTarget();

        // Trigger attack animation once
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }

        // Wait until the "hit moment"
        yield return new WaitForSeconds(attackHitDelay);

        // Re-check range at hit moment (so stepping away avoids damage)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        if (dist <= attackRange && targetHealth != null)
        {
            targetHealth.TakeDamage(contactDamage);
        }

        // Small safety delay so we don't immediately re-trigger in the same frame
        yield return null;

        isAttacking = false;
    }


}
