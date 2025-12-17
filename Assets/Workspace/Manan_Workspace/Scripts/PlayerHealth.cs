using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//script manages player health
//this is the lose condition
//also updates health bar
//i have no idea why i scaled everything up to the hundreds
//i could have just made it 0-5 but oh well
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    //basically you can take 5 hits
    public float maxHealth = 500f;
    //holds current health during gameplay
    public float currentHealth;

    [Header("UI - Sprite Health Bar")]
    //placeholder image that gets updated with sprites to show current health
    public Image healthBarImage;
    //special health sprites to show each level of health left
    public Sprite hp500Sprite;
    public Sprite hp400Sprite;
    public Sprite hp300Sprite;
    public Sprite hp200Sprite;
    public Sprite hp100Sprite;
    public Sprite hp0Sprite;

    [Header("Damage Animation")]
    //communicates with animator to play hurt animation when health decrements
    public Animator playerAnimator;
    //animation trigger
    public string hurtTriggerName = "Hurt";
    //to avoid spam and to avoid playing hurt animation when you are on damage cooldown
    public float hurtTriggerCooldown = 0.1f;

    [Header("Death Animation")]
    //animation trigger for when health reaches 0
    public string dieTriggerName = "Die";
    //slight delay before death because that is the lose condition that will trigger lose dialogue
    //but we need time to play the death animation first
    public float deathDelay = 1.2f;
    //flag that player is dead
    private bool isDead;

    //large number to ensure hurt logic plays for the first time
    //this is vital to keep track of taking damage cooldown
    private float lastHurtTriggerTime = -999f;

    [Header("Audio")]
    //entries to wire in death audio and hurt audio
    public AudioClip deathSfx;
    public AudioClip hurtSfx;
    //volume adjustments for unity inspector
    [Range(0f, 1f)] public float hurtVolume = 1f;
    [Range(0f, 1f)] public float deathSfxVolume = 1f;


    void Awake() {
        //initialize health to full health
        currentHealth = maxHealth;

        //find animator if not already assigned
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        //update ui. should show max health
        UpdateHealthUI();
    }

    public void TakeDamage(float amount) {
        //dont take damage if already dead
        //trying to prevent weird stun locks where zombie keeps damaging you and wont let the game end
        if (currentHealth <= 0f) return;

        //store previous health level to track changes
        float oldHealth = currentHealth;
        //decrement health
        currentHealth -= amount;
        //make sure health does not go below 0
        //i have no idea what will happen if it does but im too tired to find out
        if (currentHealth < 0f) currentHealth = 0f;

        //if losing health then play hurt animation
        //this is a gaurd and needs to be this way because every time the zombie attacks
        //  we might not take damage because we have a damage cooldown
        //  there are multiple probably redundant checks but it makes sure the zombie doesnt
        //  laser the player down in 1 second
        if (currentHealth < oldHealth){ 
            PlayHurtAnimation();

            //hurt sound effect
            if (hurtSfx != null)
                //play it at player position
           AudioSource.PlayClipAtPoint(hurtSfx, transform.position, hurtVolume);
            }

        //update health visual sprite to match taken damage
         UpdateHealthUI();
        
        //if for some reason death has not handle it but player health is 0 then handle it
        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            StartCoroutine(DeathRoutine());
        }
    }

    //helper function to help update the health ui
    void UpdateHealthUI() {
        //nothing to do if health bar placeholder does not exist. big problem if this executes
        if (healthBarImage == null) return;

        //update health based on current health
        //why did i not just make health 0-5
        //fun use of a ternary operator chain though
        Sprite target =
            (currentHealth >= 500f) ? hp500Sprite :
            (currentHealth >= 400f) ? hp400Sprite :
            (currentHealth >= 300f) ? hp300Sprite :
            (currentHealth >= 200f) ? hp200Sprite :
            (currentHealth >= 100f) ? hp100Sprite :
                                      hp0Sprite;

        //assign target image for sprite
        if (target != null)
            healthBarImage.sprite = target;
    }

    //linked to player animator to indicate losing health
    void PlayHurtAnimation() {
        //error gaurd if no animator exists. we have a problem if this triggers
        //might prevent any weird logic during the dying process in case we remove the animator first
        //but i guess you shouldn't be taking damage during the dying process because we made a 
        //  gaurd for that issue since zombie damage could stun-lock you and not let you die
        //anyways i am just going to leave it there because it works and i am tired
        if (playerAnimator == null) return;
        //cooldown time for taking damage
        if (Time.time < lastHurtTriggerTime + hurtTriggerCooldown) return;

        //reset animation trigger to prevent weird logic issues
        playerAnimator.ResetTrigger(hurtTriggerName);
        //trigger hurt animation
        playerAnimator.SetTrigger(hurtTriggerName);
        //record time for cooldown
        lastHurtTriggerTime = Time.time;
    }

    //function to help introduce a heal system
    //didnt end up implementing it but ill just leave it here
    public void Heal(float amount) {
        //do nothing if dead
        if (currentHealth <= 0f) return;

        //increment health
        currentHealth += amount;
        //cant heal past max health
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        //update ui to show health after healing
        UpdateHealthUI();
    }

    //handles dying process
    IEnumerator DeathRoutine() {
        //disable gameplay so you cant do something weird like shoot while dying
        GameFlowManager.Instance.SetGameplayEnabledPublic(false);

        //play death sound effect
        if (deathSfx != null)
            AudioSource.PlayClipAtPoint(deathSfx, transform.position, deathSfxVolume);

        //i have no idea why this is here
        //i think i was going to disable gameplay here if it wasnt already disabled?
        //i have no idea
        //i am just going to leave it here
        if (GameFlowManager.Instance != null)
        {

        }


        //communicate with animator to trigger dying animation
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(dieTriggerName);
            playerAnimator.SetTrigger(dieTriggerName);
        }

        //no sliding while dying
        var cc = GetComponent<CharacterController>();
        //disable character controller so no new inputs
        if (cc != null) cc.enabled = false;

        //wait for death animation to play
        yield return new WaitForSeconds(deathDelay);

        //run lose process. play lose dialogue and go to main menu
        GameFlowManager.Instance.TriggerLose();
    }
}
