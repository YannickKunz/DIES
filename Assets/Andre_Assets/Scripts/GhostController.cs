using UnityEngine;

public class GhostController : MonoBehaviour
{
    public int maxHpMin = 3;
    public int maxHpMax = 5;
    public int currentHp;

    public float damageFlashDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer;

    // --- NEW ---
    public bool isDying = false; // Flag to prevent double cleanup

    void Awake()
    {
        isDying = false; // Ensure flag is false on awake
        currentHp = Random.Range(maxHpMin, maxHpMax + 1);
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0 && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        // Don't take damage if already dying
        if (isDying) return;

        currentHp -= damageAmount;
        Debug.Log(gameObject.name + " took " + damageAmount + " damage. HP: " + currentHp);
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
            spriteRenderer.color = Color.red;
            flashTimer = damageFlashDuration;
        }
    }

    void Die()
    {
        // Prevent multiple Die calls or processing if already dying
        if (isDying) return;

        // --- SET FLAG FIRST ---
        isDying = true;
        // --------------------

        Debug.Log(gameObject.name + " died!");
        // Optional: Play death animation/effect

        // Destroy the GameObject. OnTriggerExit will be called IMMEDIATELY.
        Destroy(gameObject);
    }
}