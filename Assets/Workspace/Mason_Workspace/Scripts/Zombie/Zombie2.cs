//needed this tool for coroutines
using System.Collections;
using UnityEngine;

//old zombie movement script
//this was scrapped because it uses simple targeting and movement via character controller
//it wasn't good because this sort of AI targeting was not smart enough to navigate around obstacles and stuff
//we moved on to using nav mesh agents
//im just keeping this here as a sort of record of how we developed our game and what we tried.

//grabs zombie character controller component
[RequireComponent(typeof(CharacterController))]
public class ZombieTypeB : MonoBehaviour
{
    //controls zombie movment
    //simple fields like speed
    //other field controls how close a zombie gets to a player before stopping
    //stopping distance is needed or else the zombie will keep walking into the player and it looks weird
    //  while also messing with its attack system
    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float stoppingDistance = 1.2f;

    //keeps zombie stuck to the floor
    //this is here in case the zombie is going down a slope or something
    //we want its y position to move downwards or else it will just float
    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundCheckOffset = 0.2f;

    //prototype for zombie variant
    //this would have been a tank zombie that requires more hits
    //given the limited time to present we wanted to focus on a feature that was more visual
    [Header("Health")]
    [Tooltip("Heavy zombie. 25 damage × 5 shots = 125 HP.")]
    public float maxHealth = 125f;   // 5-shot zombie

    //tells script to target player
    [Header("Target (auto-filled if empty)")]
    public Transform target;

    //references zombie char controller
    private CharacterController controller;
    //for gravity
    private float verticalVelocity;
    //tracks zombie health
    private float currentHealth;
    //tracks zombie state. needed trigger for death animation and object destruction
    private bool isDead;

    //called when object exists
    void Start() {
        //grab zombie's character controller
        controller = GetComponent<CharacterController>();
        //sets zombie health
        currentHealth = maxHealth;

        //get player as the target as a fallback in case we forgot to assign
        if (target == null)
        {
            //checks game objects for the tag player.
            //i don't even know why i am commenting this the function is self explanatory but oh well.
            var p = GameObject.FindGameObjectWithTag("Player");
            //if it finds a object tagged player it assigns it as target
            if (p) target = p.transform;
        }
    }

    //runs every frame
    void Update() {
        //if zombie is dead or there is no target, then stop doing stuff
        if (isDead || target == null)
            return;

        //makes sure zombie is on ground
        bool isGrounded = CheckGrounded();

        //gravity logic
        if (isGrounded && verticalVelocity < 0f)
        {
            //if on ground reset velocity since we have it increase over time while falling
            verticalVelocity = -2f;
        }
        //zombie falls faster the longer it is in the air
        //honestly i don't think we needed this
        verticalVelocity += gravity * Time.deltaTime;

        //tracks a straight distance from the zombie to the player
        Vector3 toTarget = target.position - transform.position;
        //ignore height difference since there are slopes. don't want it to start walking on air
        toTarget.y = 0f;
        //calculate distance between player and zombie
        float distance = toTarget.magnitude;

        //update movment on x and z axis
        Vector3 horizontalMove = Vector3.zero;

        //only want to move if zombie is outside of the stopping distance radius
        if (distance > stoppingDistance)
        {
            //calculate horizontal movement with vectors
            Vector3 dir = toTarget.normalized;
            horizontalMove = dir * moveSpeed;

            //make the zombie face the way it is walking
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                //makes sure rotation is smooth and doesn't just snap into place
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        //apply x and z axis movement with natural gravity to make zombie stick to ground
        Vector3 velocity = horizontalMove + Vector3.up * verticalVelocity;
        //use character controller to move
        controller.Move(velocity * Time.deltaTime);
    }

    //helper function to make sure zombie is on the ground
    bool CheckGrounded() {
        //uses a small raycast that starts a little above zombies feet and ends a little below zombie feet
        //if the raycast hits something then zombie is on the ground
        float rayLength = controller.height / 2f + groundCheckOffset;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        return Physics.Raycast(origin, Vector3.down, rayLength);
    }

    //damage function that updates health of zombie
    public void TakeDamage(float amount) {
        //damage does not matter if zombie dead
        if (isDead) return;

        //subtract damage from health
        currentHealth -= amount;

        //if health hits 0 then start the dying function
        if (currentHealth <= 0f)
        {
            StartCoroutine(DieRoutine());
        }
    }

    //called when zombie health is 0
    IEnumerator DieRoutine() {
        //update bool for other function checks
        isDead = true;
        //do not need character controller to update zombie position anymore
        controller.enabled = false;

        //remove the collider so it doesn't tank hits for zombies behind it.
        //this is more of a failsafe where it is doing a falling animation and the zombie behind it
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        //dying animation
        //rotates the zombie to fall and die
        //we swap this out for an actual animation in our final draft zombie scripts
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.LookRotation(transform.forward, Vector3.down);
        float t = 0f;
        float duration = 0.3f;

        //slowly rotate zombie downwards instead of instant so it looks like it is falling
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        //pause to show zombie on the floor and dead before destroying
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

}
