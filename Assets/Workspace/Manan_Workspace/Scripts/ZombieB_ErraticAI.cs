using System.Collections;
using UnityEngine;

//final version of zombie B variant
//what makes it different from zombie A is its unique movement
//inherits from zombieAi base so we are not repeating code
public class ZombieB_ErraticAI : ZombieAIBase
{
    //field for run speed. have to modify this and not the run speed in the nav mesh
    [Header("Run Settings")]
    public float runSpeed = 4.2f;

    //when far from the player it will also strafe and hop to a random position to evade gunfire
    [Header("Erratic Pathing (far away)")]
    //how often it will find a strafe target
    public float retargetInterval = 0.5f;
    //how far it will jump when strafing
    public float offsetRadius = 2.0f;

    //stop strafing when close to player
    //we need this or else the zombie keeps trying to strafe instead of attacking the player
    //when it gets close enough to the player it just locks on and walks straight to the player
    //but if the player gets far enough away it will resume strafe behavior
    [Header("Lock-On Zone")]
    [Tooltip("Within this distance (meters), stop strafing and use base chase/stop behavior.")]
    public float lockOnRange = 2.0f;

    //cooldown between actually executing jump
    [Header("Strafe Jump (burst)")]
    [Tooltip("Average seconds between strafe-jumps (randomized).")]
    public float jumpFrequency = 2.0f;

    //adds a small random timing to the frequency it jumps for a high level of randomness
    //honestly this is probably not noticeable
    [Tooltip("Randomness on jump frequency (+/- seconds).")]
    public float jumpFrequencyJitter = 0.6f;

    //how far zombie will jump to a side
    [Tooltip("How far the strafe-jump moves sideways (meters).")]
    public float jumpDistance = 2.0f;

    //how fast the zombie will be while jumping
    [Tooltip("Temporary speed during the strafe-jump burst.")]
    public float jumpSpeed = 8.0f;

    //how long jump speed will exist
    [Tooltip("How long to keep jumpSpeed active (seconds).")]
    public float jumpBurstDuration = 0.35f;

    //freeze zombie while landing so zombie does not glide while finishing landing animation
    [Tooltip("Freeze movement briefly after landing so it doesn't glide.")]
    public float landingFreezeTime = 0.20f;

    //trigger for jump animation
    //the jump animation plays but the zombie is not actually lifting off the ground
    //if we lifted off the ground it would mess with the nav mesh agent because it is not on the nav mesh
    [Header("Jump Animation")]
    public string jumpTrigger = "Jump";

    //time until picking new target
    private float nextRetargetTime;
    //offset that will be added to player position
    private Vector3 currentOffset;

    //track time until next jump
    private float nextJumpTime;
    //tracks state of if zombie is in the middle of strafe jump
    private bool isStrafeJumping;

    //used to reset zombie speed when strafe jump action is over
    private float baseSpeed;
    private float baseAccel;
    private float baseAngular;

    //method overides the zombieAI base class that this class is inheriting from
    protected override void Awake() {
        //call awake() from zombieAI base for zombie setup
        base.Awake();

        //set speed to specified running speed
        agent.speed = runSpeed;
        //makes sure zombie does not accelerate slowly when turning
        agent.acceleration = Mathf.Max(agent.acceleration, 18f);
        //makes sure turning is quick and not slow and gradual
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);

        //save current values so they can be reset to these values after the strafe action is done
        baseSpeed = agent.speed;
        baseAccel = agent.acceleration;
        baseAngular = agent.angularSpeed;

        //pick offset on path towards player to move towards during strafe action
        //zombieB does not make a straight advance to the player
        PickNewOffset();
        //cooldown before choosing  offset again
        nextRetargetTime = Time.time + retargetInterval;

        //cooldown for actually doing a strafe jump action
        ScheduleNextJump();
    }

    //override zombieAIbase update so that we can add code specifically for zombieB
    protected override void Update() {
        //run base update from ZombieAI base first
        base.Update();

        //if one of the following is true then zombieB will not update
        //makes sure gameplay is active
        //  game might be paused or intro dialogue is still playing
        //makes sure zombie is not dead
        //makes sure it has a target
        //makes sure zombie is not strafe jumping. it has to finish that action before it does anything else
        //makes sure it is not stunned from getting hit. when it gets hit it plays the hurt animation
        //  and gets stunned for a second so that it does not glide while the hit animation plays
        //if zombie is in the middle of attacking do not make it move or else that might mess up logic
        //  just have it do one thing at a time
        if (!GameFlowManager.GameplayActive) return;
        if (isDead || target == null) return;
        if (!AgentReady()) return;
        if (isStrafeJumping) return;
        if (Time.time < stunnedUntil) return;
        if (isAttacking) return;

        //logic to make sure zombie does not strafe jump when close to player
        //get zombie position but ignore y because map has slopes
        Vector3 a = transform.position; a.y = 0f;
        //get player position but ignore y
        Vector3 b = target.position; b.y = 0f;
        //get x and z axis distance from zombie and player
        float dist = Vector3.Distance(a, b);
        //if zombie is within lock on range then return and do not attempt to execute strafe jump code below
        if (dist <= lockOnRange) return;

        //if the return above did not trigger that means zombie is not close enough and has the chance to
        //  strafe jump
        //checks if cooldown is over
        if (Time.time >= nextJumpTime)
        {
            //runs jump helper function and starts cooldown
            StartCoroutine(StrafeJumpRoutine());
            ScheduleNextJump();
        }
    }

    //overrides generic lock on movement from zombieAIBase
    protected override void UpdateMovement() {
        //don't do anything if for some reason player does not exist or zombie is not in ready state
        if (!AgentReady() || target == null) return;
        //dont do movement if zombie is in the middle of strafe jump
        if (isStrafeJumping) return;

        //get zombie position. ignore y value
        Vector3 a = transform.position; a.y = 0f;
        //get player position. ignore y value
        Vector3 b = target.position; b.y = 0f;
        //compute distance on x and z axis
        float dist = Vector3.Distance(a, b);

        //executes if zombie is close enoguh to player
        if (dist <= lockOnRange)
        {
            //revert back to generic chase logic and just walk straight to player
            //  no strafe jump
            base.UpdateMovement();
            //add to retarget cooldown so that it does not trigger accidentally
            nextRetargetTime = Time.time + retargetInterval;
            return;
        }

        //executes if not close enough to player for simple targetting
        //checks if cooldown is over
        if (Time.time >= nextRetargetTime)
        {
            //pick new jump target and reset cooldown
            PickNewOffset();
            nextRetargetTime = Time.time + retargetInterval;
        }

        //allows zombie to move again after stun or strafe jump
        agent.isStopped = false;
        //set target destination
        agent.SetDestination(target.position + currentOffset);
    }

    //strafe jump helper function
    IEnumerator StrafeJumpRoutine() {
        //sets flag that we are jumping so that other movement logic does not conflict our jumping execution
        isStrafeJumping = true;

        //calculate where to strafe jump to
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;

        //if jump position is too close then does not execute strafe jump to prevent weird in place jumps
        if (toPlayer.sqrMagnitude < 0.01f)
        {
            isStrafeJumping = false;
            yield break;
        }

        //set direction to face player
        Vector3 dir = toPlayer.normalized;

        //choose and set strafe direction
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        float side = (Random.value < 0.5f) ? -1f : 1f;

        //make jump sort of diagonal facing so that it does not look like a weird side step
        Vector3 diagonal = (right * side + dir * 0.35f).normalized;

        //calculate target position for strafe jump
        Vector3 jumpTarget = transform.position + diagonal * jumpDistance;

        //this is a fun workaround
        //i can't make the agent lift off the nav mesh because that messes stuff up
        //so during the "jump" execution i play the jump animation while increasing movement speed
        //it looks like the zombie is off of the ground and is jumping
        //however the collider is still planted on the ground. its just a visual effect and a movement speed boost
        if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
            //trigger for animator to execute jump animation
            animator.SetTrigger(jumpTrigger);

        //save regular movement speed values
        baseSpeed = agent.speed;
        baseAccel = agent.acceleration;
        baseAngular = agent.angularSpeed;

        //set jump movement speed values
        agent.speed = jumpSpeed;
        agent.acceleration = Mathf.Max(baseAccel, 30f);
        agent.angularSpeed = Mathf.Max(baseAngular, 720f);

        //make sure zombie can move
        agent.isStopped = false;
        //set jump target
        agent.SetDestination(jumpTarget);

        //wait for jump to end before doing other movement. this is fine tuned between the animation timing and stuff
        //this was pretty annoying
        float tEnd = Time.time + jumpBurstDuration;
        //only executes if jump timer is done
        while (Time.time < tEnd)
        {
            //error handling in case something breaks during jump
            if (!AgentReady()) break;

            //stop jump operation if we are close to player
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                break;

            yield return null;
        }

        //revert back to normal movement values
        if (AgentReady())
        {
            //set values back to default
            agent.speed = runSpeed;
            agent.acceleration = Mathf.Max(agent.acceleration, 18f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);

            //kill horizontal movement while jump animation executes.
            //need to do this because while jump animation is ending and the model is landing
            //  its legs are not moving. so if it starts horizontal movement while
            //  the landing animation is playing it just looks like it is gliding
            //  that is why we need this
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + landingFreezeTime);
            agent.isStopped = true;
            agent.ResetPath();
        }

        //mark that jump operation is done so that zombie is not locked anymore
        isStrafeJumping = false;
    }

    //helper function to get new jump location target
    void PickNewOffset() {
        Vector2 circle = Random.insideUnitCircle.normalized * offsetRadius;
        currentOffset = new Vector3(circle.x, 0f, circle.y);
    }

    //schedule cooldown until next jump with some randomness
    void ScheduleNextJump() {
        float jitter = Random.Range(-jumpFrequencyJitter, jumpFrequencyJitter);
        nextJumpTime = Time.time + Mathf.Max(0.2f, jumpFrequency + jitter);
    }
}
