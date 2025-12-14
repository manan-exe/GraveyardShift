using System.Collections;
using UnityEngine;

public class ZombieB_ErraticAI : ZombieAIBase
{
    [Header("Run Settings")]
    public float runSpeed = 4.2f;

    [Header("Erratic Pathing (far away)")]
    public float retargetInterval = 0.5f;
    public float offsetRadius = 2.0f;

    [Header("Lock-On Zone")]
    [Tooltip("Within this distance (meters), stop strafing and use base chase/stop behavior.")]
    public float lockOnRange = 2.0f;

    [Header("Strafe Jump (burst)")]
    [Tooltip("Average seconds between strafe-jumps (randomized).")]
    public float jumpFrequency = 2.0f;

    [Tooltip("Randomness on jump frequency (+/- seconds).")]
    public float jumpFrequencyJitter = 0.6f;

    [Tooltip("How far the strafe-jump moves sideways (meters).")]
    public float jumpDistance = 2.0f;

    [Tooltip("Temporary speed during the strafe-jump burst.")]
    public float jumpSpeed = 8.0f;

    [Tooltip("How long to keep jumpSpeed active (seconds).")]
    public float jumpBurstDuration = 0.35f;

    [Tooltip("Freeze movement briefly after landing so it doesn't glide.")]
    public float landingFreezeTime = 0.20f;

    [Header("Jump Animation")]
    public string jumpTrigger = "Jump";

    private float nextRetargetTime;
    private Vector3 currentOffset;

    private float nextJumpTime;
    private bool isStrafeJumping;

    // Store original agent settings so we can restore them
    private float baseSpeed;
    private float baseAccel;
    private float baseAngular;

    protected override void Awake() {
        base.Awake();

        agent.speed = runSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, 18f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);

        baseSpeed = agent.speed;
        baseAccel = agent.acceleration;
        baseAngular = agent.angularSpeed;

        PickNewOffset();
        nextRetargetTime = Time.time + retargetInterval;

        ScheduleNextJump();
    }

    protected override void Update() {
        base.Update();

        // Only consider jump when gameplay is active and we're not busy
        if (!GameFlowManager.GameplayActive) return;
        if (isDead || target == null) return;
        if (!AgentReady()) return;
        if (isStrafeJumping) return;
        if (Time.time < stunnedUntil) return;
        if (isAttacking) return;

        // Don't strafe-jump when close (lock-on + attack zone)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist <= lockOnRange) return;

        if (Time.time >= nextJumpTime)
        {
            StartCoroutine(StrafeJumpRoutine());
            ScheduleNextJump();
        }
    }

    protected override void UpdateMovement() {
        if (!AgentReady() || target == null) return;
        if (isStrafeJumping) return;

        // XZ distance (match base)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        // Close: behave exactly like ZombieA
        if (dist <= lockOnRange)
        {
            base.UpdateMovement();
            nextRetargetTime = Time.time + retargetInterval;
            return;
        }

        // Far: offset chasing
        if (Time.time >= nextRetargetTime)
        {
            PickNewOffset();
            nextRetargetTime = Time.time + retargetInterval;
        }

        agent.isStopped = false;
        agent.SetDestination(target.position + currentOffset);
    }

    IEnumerator StrafeJumpRoutine() {
        isStrafeJumping = true;

        // Decide strafe direction relative to zombie->player
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f)
        {
            isStrafeJumping = false;
            yield break;
        }

        Vector3 dir = toPlayer.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        float side = (Random.value < 0.5f) ? -1f : 1f;

        // Slight diagonal forward so it's not a perfect sideways slide
        Vector3 diagonal = (right * side + dir * 0.35f).normalized;

        Vector3 jumpTarget = transform.position + diagonal * jumpDistance;

        // Kick off animation (visual only)
        if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
            animator.SetTrigger(jumpTrigger);

        // Temporarily boost agent movement
        baseSpeed = agent.speed;
        baseAccel = agent.acceleration;
        baseAngular = agent.angularSpeed;

        agent.speed = jumpSpeed;
        agent.acceleration = Mathf.Max(baseAccel, 30f);
        agent.angularSpeed = Mathf.Max(baseAngular, 720f);

        agent.isStopped = false;
        agent.SetDestination(jumpTarget);

        // Wait for burst duration (or early finish if it reaches the target)
        float tEnd = Time.time + jumpBurstDuration;
        while (Time.time < tEnd)
        {
            if (!AgentReady()) break;

            // If the agent got close enough, consider it "landed"
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                break;

            yield return null;
        }

        // Restore normal run settings
        if (AgentReady())
        {
            agent.speed = runSpeed;
            agent.acceleration = Mathf.Max(agent.acceleration, 18f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);

            // "Landing kill horizontal" = brief stun
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + landingFreezeTime);
            agent.isStopped = true;
            agent.ResetPath();
        }

        isStrafeJumping = false;
    }

    void PickNewOffset() {
        Vector2 circle = Random.insideUnitCircle.normalized * offsetRadius;
        currentOffset = new Vector3(circle.x, 0f, circle.y);
    }

    void ScheduleNextJump() {
        float jitter = Random.Range(-jumpFrequencyJitter, jumpFrequencyJitter);
        nextJumpTime = Time.time + Mathf.Max(0.2f, jumpFrequency + jitter);
    }
}
