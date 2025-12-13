using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 500f;
    public float currentHealth;

    [Header("UI - Sprite Health Bar")]
    public Image healthBarImage;          // UI Image to swap sprites on
    public Sprite hp500Sprite;            // 500
    public Sprite hp400Sprite;            // 400
    public Sprite hp300Sprite;            // 300
    public Sprite hp200Sprite;            // 200
    public Sprite hp100Sprite;            // 100
    public Sprite hp0Sprite;              // 0

    [Header("Damage Animation")]
    public Animator playerAnimator;       // player animator (or child animator)
    public string hurtTriggerName = "Hurt";
    public float hurtTriggerCooldown = 0.1f;

    [Header("Death Animation")]
    public string dieTriggerName = "Die";
    public float deathDelay = 1.2f; // set to your clip length (or slightly less)
    private bool isDead;

    private float lastHurtTriggerTime = -999f;

    void Awake() {
        currentHealth = maxHealth;

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        UpdateHealthUI();
    }

    public void TakeDamage(float amount) {
        if (currentHealth <= 0f) return;

        float oldHealth = currentHealth;
        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        // Only play hurt if health actually went down
        if (currentHealth < oldHealth)
            PlayHurtAnimation();

        UpdateHealthUI();

        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            StartCoroutine(DeathRoutine());
        }
    }

    void UpdateHealthUI() {
        if (healthBarImage == null) return;

        // Snap to your 500/400/300/200/100/0 “buckets”
        Sprite target =
            (currentHealth >= 500f) ? hp500Sprite :
            (currentHealth >= 400f) ? hp400Sprite :
            (currentHealth >= 300f) ? hp300Sprite :
            (currentHealth >= 200f) ? hp200Sprite :
            (currentHealth >= 100f) ? hp100Sprite :
                                      hp0Sprite;

        if (target != null)
            healthBarImage.sprite = target;
    }

    void PlayHurtAnimation() {
        if (playerAnimator == null) return;
        if (Time.time < lastHurtTriggerTime + hurtTriggerCooldown) return;

        playerAnimator.ResetTrigger(hurtTriggerName);
        playerAnimator.SetTrigger(hurtTriggerName);
        lastHurtTriggerTime = Time.time;
    }

    // Optional helper if you ever want healing later
    public void Heal(float amount) {
        if (currentHealth <= 0f) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHealthUI();
    }
    IEnumerator DeathRoutine() {
        GameFlowManager.Instance.SetGameplayEnabledPublic(false);
        // Disable gameplay input immediately so the player can’t move/shoot while dying
        if (GameFlowManager.Instance != null)
        {
            // This calls your lose flow AFTER the delay
            // but first disable player control instantly:
            // (If you don’t have a method for this, see note below.)
        }

        // Trigger death animation
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(dieTriggerName);
            playerAnimator.SetTrigger(dieTriggerName);
        }

        // Optional: stop movement instantly (prevents sliding)
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        yield return new WaitForSeconds(deathDelay);

        // Now run your existing lose dialogue → main menu pipeline
        GameFlowManager.Instance.TriggerLose();
    }
}
