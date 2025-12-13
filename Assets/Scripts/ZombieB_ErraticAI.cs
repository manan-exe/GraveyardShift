using UnityEngine;

public class ZombieB_ErraticAI : ZombieAIBase
{
    [Header("Run Settings")]
    public float runSpeed = 4.2f;

    [Header("Erratic Movement")]
    [Tooltip("How often (seconds) we pick a new offset around the player.")]
    public float retargetInterval = 0.35f;

    [Tooltip("How far sideways/around the player we aim (meters).")]
    public float offsetRadius = 2.0f;

    [Tooltip("Chance per retarget to strafe harder left/right.")]
    [Range(0f, 1f)] public float strafeChance = 0.35f;

    [Tooltip("Chance per retarget to trigger a hop animation.")]
    [Range(0f, 1f)] public float hopChance = 0.12f;

    [Header("Animation")]
    public string hopTrigger = "Hop";

    float nextRetargetTime;
    Vector3 currentOffset;

    protected override void Awake() {
        base.Awake();
        agent.speed = runSpeed;
        PickNewOffset();
    }

    protected override void UpdateMovement() {
        if (Time.time >= nextRetargetTime)
        {
            PickNewOffset();
            nextRetargetTime = Time.time + retargetInterval;
        }

        Vector3 desired = target.position + currentOffset;
        agent.SetDestination(desired);
    }

    void PickNewOffset() {
        // Base offset around player on XZ
        Vector2 circle = Random.insideUnitCircle.normalized * offsetRadius;
        currentOffset = new Vector3(circle.x, 0f, circle.y);

        // Occasionally bias offset sideways relative to player (strafey feel)
        if (Random.value < strafeChance)
        {
            Vector3 toPlayer = (target.position - transform.position);
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, toPlayer.normalized);
                float dir = Random.value < 0.5f ? -1f : 1f;
                currentOffset += right * dir * (offsetRadius * 0.75f);
            }
        }

        // Occasionally trigger a hop animation (visual only)
        if (animator != null && Random.value < hopChance)
        {
            animator.SetTrigger(hopTrigger);
        }
    }
}
