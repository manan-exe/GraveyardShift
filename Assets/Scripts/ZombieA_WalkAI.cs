using UnityEngine;

public class ZombieA_WalkAI : ZombieAIBase
{
    [Header("Walk Settings")]
    public float walkSpeed = 2.0f;

    protected override void Awake() {
        base.Awake();
        agent.speed = walkSpeed;
    }

}
