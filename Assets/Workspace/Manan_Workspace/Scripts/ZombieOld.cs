
using System.Collections;
using UnityEngine;

//original zombie script. not used anymore
//get zombie component
[RequireComponent(typeof(CharacterController))]
public class ZombieOld : MonoBehaviour


{
    //set attack parameters
    //need range it can reach
    //how much damage it does
    //how frequently it can attack
    //and a variable to track when it is time to attack again
    [Header("Attack")]
    public float attackRange = 1.4f;
    public float contactDamage = 10f;
    public float attackCooldown = 0.5f;
    private float nextAttackTime;

    //variables to set how fast zombie moves and how far from the player it will stop
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stoppingDistance = 1.2f;

    //gravity to keep zombie stuck to floor
    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundCheckOffset = 0.2f;

    //zombie health. can configure in unity inspector
    [Header("Health")]
    [Tooltip("Gun currently does 25 damage. 50 HP = 2 shots.")]
    public float maxHealth = 50f;

    //get player target
    [Header("Target (auto-filled if empty)")]
    public Transform target;

    //controller to move zombie
    private CharacterController controller;
    //track falling speed, health, and alive status
    private float verticalVelocity;
    private float currentHealth;
    private bool isDead;

    //activate when zombie is created
    void Start() {
        //get zombie controller
        controller = GetComponent<CharacterController>();
        //set default max health
        currentHealth = maxHealth;

        //error gaurd in case target was not manually set
        //finds game object with player tag
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    //runs every frame
    void Update() {
        //if zombie dead or player does not exist nothing to do
        if (isDead || target == null)
            return;

        //check if on ground to prevent floating
        bool isGrounded = CheckGrounded();

        //gravity
        if (isGrounded && verticalVelocity < 0f)
        {
            //reset gravity speed once on ground
            verticalVelocity = -2f;
        }
        //increase falling speed when in air
        verticalVelocity += gravity * Time.deltaTime;

        //points from zombie to player
        Vector3 toTarget = target.position - transform.position;
        //do not care about y axis position
        toTarget.y = 0f;
        //get distance between zombie and player
        float distance = toTarget.magnitude;

        //vector only cares about x and z axis
        Vector3 horizontalMove = Vector3.zero;

        //if zombie is not in radius of stopping distance then move toward player
        if (distance > stoppingDistance)
        {
            Vector3 dir = toTarget.normalized;
            horizontalMove = dir * moveSpeed;

            //rotate to face player
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                //apply rotation gradually
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        //apply updated position and make zombie move using character controller
        Vector3 velocity = horizontalMove + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        //only triggers if zombie is close enough and it is done with attack cooldown
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            //checks player health
            PlayerHealth hp = target.GetComponent<PlayerHealth>();
            //if player health isn't 0 then deal damage
            if (hp != null)
            {
                hp.TakeDamage(contactDamage);
                //restart cooldown
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    //makes sure not floating zombie
    bool CheckGrounded() {
        //uses raycast towards ground to see if it hits ground
        float rayLength = controller.height / 2f + groundCheckOffset;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, rayLength);
    }

    //handles how player deals damage to zombie
    public void TakeDamage(float amount) {
        //if zombie is dead don't do anything
        if (isDead) return;

        //decrement health
        currentHealth -= amount;
        //if zombie health is 0 then start dying process
        if (currentHealth <= 0f)
        {
            StartCoroutine(DieRoutine());
        }
    }

    //handles how zombie dies
    IEnumerator DieRoutine() {
        //set bool to indicate zombie is dead for checks
        isDead = true;

        //remove animation controller to do code driven death animation.
        //we use better animation assets in our final zombie prefabs
        controller.enabled = false;

        //disable hitbox
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        //make zombie fall
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.LookRotation(transform.forward, Vector3.down);
        
        //apply fall gradually so it looks like being pulled by gravity to fall over
        float t = 0f;
        float duration = 0.3f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        //wait a second before destroying zombie
        //want to cleanup so zombies aren't everywhere on the ground
        //would probably also drain computer resources for no reason if we left all the zombies there
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

   //handles what happens if collider trigger activates
    void OnTriggerStay(Collider other) {
        //do nothing if dead
        if (isDead) return;
        //do nothing if attack cooldown isn't done
        if (Time.time < nextAttackTime) return;

        //only triggers if collider was triggered by player
        if (other.CompareTag("Player"))
        {
            //get current player health
            PlayerHealth hp = other.GetComponent<PlayerHealth>();
            //if hp 0 do nothing
            if (hp != null)
            {
                //if hp not 0 then deal damage and start attack cooldown
                hp.TakeDamage(contactDamage);
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

}
