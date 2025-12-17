using System.Collections;
using UnityEngine;
using UnityEngine.AI;

//base class that the zombieA and zombieB scripts inherit from
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAIBase : MonoBehaviour
{
    //field for target which is the player
    [Header("Target")]
    public Transform target;


    [Header("Combat")]
    //field for health
    public float maxHealth = 50f;
    //for damage zombie deals
    public float contactDamage = 10f;
    //zombies reach
    public float attackRange = 1.4f;
    //how often zombie can attack
    public float attackCooldown = 0.5f;

    [Header("Animation")]
    //field for animator
    public Animator animator;
    //movement speed
    public string speedParam = "Speed";

    [Header("Animation Triggers")]
    //triggers for animation like taking damage or dying
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die";

    [Header("Death Cleanup")]
    //delete game object after some time 
    public float corpseLifetime = 4f;
    public bool disableCollidersOnDeath = true;

    [Header("Hurt Stun")]
    //how long zombie is stunned after getting hit
    public float hurtStunTime = 0.35f;
    protected float stunnedUntil;

    [Header("Stopping / Facing")]
    //stop distance so they dont get super close to player and mess with colliders
    public float stopBuffer = 0.15f;
    //zombie rotation speed. should not be instant because that looks not natural
    public float faceTurnSpeed = 12f;

    [Header("Attack Animation")]
    //trigger for animator
    public string attackTrigger = "Attack";
    //locks zombie movement so it attacks and does not fight the animator
    public bool stopToAttack = true;


    [Header("Attack Timing")]
    //delay from performing hit and when player health updates
    public float attackHitDelay = 0.25f;
    //flag for when attack function is running
    protected bool isAttacking;

    //tracks players health
    private PlayerHealth targetHealth;

    //field for the actual movement ai of the zombie
    protected NavMeshAgent agent;
    //zombie health
    protected float currentHealth;
    //flag if dead
    protected bool isDead;
    //cooldown for attack
    protected float nextAttackTime;

    [Header("Audio")]
    //specific clip that plays when zombie dies
    public AudioClip deathSfx;
    //volume
    [Range(0f, 1f)] public float deathSfxVolume = 1f;
    //audio source
    public AudioSource sfxSource;



    protected virtual void Awake() {
        //get navmesh to communicate when it has to stop and stuff
        agent = GetComponent<NavMeshAgent>();
        //set health
        currentHealth = maxHealth;

        //if target is not manually wired in then try to find object with player tag
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        //if player exists then we can execute what is below
        if (target != null)
        {
            //get player health. check parent and child objects to thoroughly search for player health
            //dont need all of this but i dont want to break anything at this point
            targetHealth = target.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null) targetHealth = target.GetComponentInChildren<PlayerHealth>();
            if (targetHealth == null) targetHealth = target.GetComponent<PlayerHealth>();
        }

        //search for animator if it was not manually assigned.
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    //base update method. each specific zombie script can override
    protected virtual void Update() {
        //if zombie is dead or player does not exist dont do anything
        if (isDead || target == null) return;

        //agent will not be ready during death cleanup
        if (!AgentReady())
        {
            //tell animator speed is 0 to stop movement animation
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }

        //checks if gameplay is running so that zombie isn't moving if game is paused or during intro dialogue
        if (!GameFlowManager.GameplayActive)
        {
            //if gameplay is not running then stop zombie
            agent.isStopped = true;
            //clear set path to prevent any weird logic after game resumes
            agent.ResetPath();
            //tell animator to stop playing movement animation so they are not moving in place while game is paused
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }

        //logic to stun zombie in place while getting hit or attacking

        //sees if stun duration is over
        if (Time.time < stunnedUntil)
        {
            //make zombie stop during stun
            agent.isStopped = true;
            //stop run animation
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }
        //stop zombie while attacking
        if (isAttacking)
        {
            agent.isStopped = true;
            //make animator play idle
            if (animator != null) animator.SetFloat(speedParam, 0f);
            return;
        }

        //let zombie move again
        agent.isStopped = false;

        //run movement update
        UpdateMovement();
        //see if in range to damage player
        TryDamagePlayer();
        //makes sure animator is accurately showing what zombie is doing
        UpdateAnimator();
    }

    //movement is different between zombieA and zombieB
    protected virtual void UpdateMovement() {
        //if agent is not available do not do anything
        if (!AgentReady()) return;

        //get zombie position
        //get player position
        //ignore y axis for both
        //calculate distance
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        //stopping distance to avoid bumping into player
        agent.stoppingDistance = attackRange + stopBuffer;

        //flag to see if zombie in range of player or not
        bool inRange = dist <= agent.stoppingDistance;

        //executes if zombie is close enough
        if (inRange)
        {
            //stop zombie
            agent.isStopped = true;
            //delete path target
            agent.ResetPath();
            //face the player for attacking
            FaceTarget();
        }
        //if zombie is not close enough
        else
        {
            //let zombie move
            agent.isStopped = false;
            //make a new target destination
            agent.SetDestination(target.position);
        }
    }

    //function for attacking
    protected void TryDamagePlayer() {
        //dont try to attack if you are already attacking
        if (isAttacking) return;
        //cooldown for attack
        if (Time.time < nextAttackTime) return;

        //calculate distance between zombie and player
        //ignore y axis
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        //if too far then return because you cannot attack
        if (dist > attackRange) return;

        //try to attack. and activate cooldown
        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }


    //update animator to mirror what zombie logic is doing
    protected void UpdateAnimator() {
        //error handling
        //do nothing if animator does not exist
        if (animator == null) return;

        //calculate speed
        float speed = agent.velocity.magnitude;
        //transition between animatons
        animator.SetFloat(speedParam, speed);
    }

    //function to deal damage to zombie
    public void TakeDamage(float amount) {
        //if zombie is dead nothing needs to be done
        if (isDead) return;

        //decrement zombie health
        currentHealth -= amount;

        //if zombie health is not 0, meaning zombie is not dead
        if (currentHealth > 0f)
        {
            //checks that animator exists
            //if it does then set the hurt trigger for the animator
            if (animator != null) animator.SetTrigger(hurtTrigger);

            //zombie gets stunned by the hit
            stunnedUntil = Time.time + hurtStunTime;
            //tell nav mesh agent to stop zombie
            if (agent != null && agent.enabled) agent.isStopped = true;

            return;
        }

        //code only gets here if zombie has 0 or less health
        currentHealth = 0f;
        isDead = true;
        StartCoroutine(DieRoutine());
    }

    //zombie dying process
    IEnumerator DieRoutine() {
        //set flag that zombie is dead for any update checks
        isDead = true;

        //play death sound if it exists
        if (sfxSource != null && deathSfx != null)
            sfxSource.PlayOneShot(deathSfx, deathSfxVolume);

        //if nav mesh agent is active stop it
        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        //set animator to death animation
        if (animator != null)
            animator.SetTrigger(dieTrigger);

        //delete colliders since zombie dead
        if (disableCollidersOnDeath)
        {
            //error handling to make sure there are not any extra random colliders
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;
        }

        //turn off nav mesh agent
        if (agent != null) agent.enabled = false;

        //zombie body lies on the floor for a second before being deleted
        yield return new WaitForSeconds(corpseLifetime);
        Destroy(gameObject);
    }
    
    //helper function to check that zombie nav mesh agent is useable
    protected bool AgentReady() {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    //handles making zombie face the player
    protected void FaceTarget() {
        //direction player is from zombie
        Vector3 dir = target.position - transform.position;
        //ignore y axis
        dir.y = 0f;
        //if zombie is pretty much facing player dont do unnecessary corrections
        if (dir.sqrMagnitude < 0.0001f) return;

        //rotate zombie towards player gradually
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, faceTurnSpeed * Time.deltaTime);
    }

    //attack helper function
    IEnumerator AttackRoutine() {
        //set flag so that movement stops
        isAttacking = true;

        //if zombie is stopped and nav mesh agent is useable then this executes
        if (stopToAttack && AgentReady())
        {
            //stop the agent
            agent.isStopped = true;
            //reset target that zombie moving towards
            agent.ResetPath();
        }

        //make zombie face player
        FaceTarget();

        ////run attack animation
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            //makes sure attack trigger wasnt somehow still triggered
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }

        //delay between attack/animation and player health update
        yield return new WaitForSeconds(attackHitDelay);

        //checks one last time that player is close enough to get hit
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        if (dist <= attackRange && targetHealth != null)
        {
            //deal damage
            targetHealth.TakeDamage(contactDamage);
        }

        yield return null;

        isAttacking = false;
    }


}
