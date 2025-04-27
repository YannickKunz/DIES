using UnityEngine;

public class GhostController : MonoBehaviour
{
    public int maxHpMin = 3; // Minimum possible starting HP
    public int maxHpMax = 5; // Maximum possible starting HP
    public int currentHp;

    // Optional: Add feedback for taking damage
    public float damageFlashDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer;

    void Awake()
    {
        // Initialize HP randomly within the specified range
        currentHp = Random.Range(maxHpMin, maxHpMax + 1); // +1 because Random.Range for ints is exclusive max

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        // Handle damage flash feedback
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor; // Reset color
                }
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHp -= damageAmount;
        Debug.Log(gameObject.name + " took " + damageAmount + " damage. HP: " + currentHp);

        // Trigger damage feedback
        FlashDamage();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void FlashDamage()
    {
         if (spriteRenderer != null)
         {
             spriteRenderer.color = Color.red; // Or white, or any flash color
             flashTimer = damageFlashDuration;
         }
    }


    void Die()
    {
        Debug.Log(gameObject.name + " died!");
        // Optional: Play death animation or particle effect here
        Destroy(gameObject); // Remove the ghost from the scene
    }

    // Note: Collision with the player is handled by the PlayerController script
    // using OnTriggerEnter2D, checking for the "Ghost" tag.
}