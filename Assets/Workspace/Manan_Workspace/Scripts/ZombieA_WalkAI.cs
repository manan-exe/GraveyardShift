using UnityEngine;

//zombieA script inherits from zombieAI base
//zombieA is pretty basic so script is pretty short
public class ZombieA_WalkAI : ZombieAIBase
{
    //speed of zombieA
    [Header("Walk Settings")]
    public float walkSpeed = 2.0f;

    //call zombieAI base function
    //nothing unique besides speed
    //overrides nav mesh agent so you have to set speed in the field for the script
    //  and not the nav mesh agent
    protected override void Awake() {
        base.Awake();
        agent.speed = walkSpeed;
    }

}
