using System.Collections; // For IEnumerator
using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("Health & Damage")]
    public int maxHpMin = 3;
    public int maxHpMax = 5;
    public int currentHp;

    [Header("Visual Feedback")]
    public float damageFlashDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer;

    [Header("Retreat Mechanic")]
    public float retreatDuration = 5f;      // How long the ghost retreats
    public float retreatSpeedMultiplier = 1.5f; // How much faster it moves when retreating
    private bool isRetreating = false;      // Flag to indicate if currently retreating

    // --- References to Movement Scripts (Important!) ---
    private EnemyController enemyController;   // For Chaser type movement
    private WanderMovement wanderMovement; // For Wanderer type movement
    private Rigidbody2D rb; // For direct velocity control if needed

    // --- State for Movement Scripts ---
    private float originalSpeedForMovementScript;
    private bool wasEnemyControllerActive;
    private bool wasWanderMovementActive;


    // The isDying flag is for permanent death, not temporary retreat
    public bool isDying = false;

    void Awake()
    {
        isDying = false;
        isRetreating = false;
        currentHp = Random.Range(maxHpMin, maxHpMax + 1);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Get references to potential movement scripts and Rigidbody2D
        enemyController = GetComponent<EnemyController>();
        wanderMovement = GetComponent<WanderMovement>();
        rb = GetComponent<Rigidbody2D>();
        Debug.Log(gameObject.name + " Rigidbody2D: " + (rb == null ? "NULL" : "Assigned, BodyType: " + rb.bodyType)); // Modified log
    }

    void Update()
    {
        // Handle damage flash
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0 && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        // If retreating and using Rigidbody2D for retreat, apply retreat movement here
        // This part is optional if your movement scripts handle the retreat direction well enough
        // when their speed is simply reversed/increased.
    }

    public void TakeDamage(int damageAmount)
    {
        // Don't take damage if already permanently dying OR already retreating
        if (isDying || isRetreating) return;

        currentHp -= damageAmount;
        Debug.Log(gameObject.name + " took " + damageAmount + " damage. HP: " + currentHp);
        FlashDamageFeedback(); // Renamed from FlashDamage

        if (currentHp <= 0)
        {
            // Instead of Die(), start the retreat
            StartCoroutine(RetreatRoutine());
        }
    }

    private IEnumerator RetreatRoutine()
    {
        isRetreating = true;
        Debug.Log(gameObject.name + " is starting retreat!");

        // --- 1. Store original state and modify movement for retreat ---
        Transform playerTransform = null;
        if (enemyController != null && enemyController.player != null) // Assuming EnemyController has a 'player' field
        {
            playerTransform = enemyController.player;
        }
        else
        {
            // Fallback: Try to find player if EnemyController or its player ref is missing
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }


        // --- Modify attached movement scripts for retreat ---
        originalSpeedForMovementScript = 2f;
        wasEnemyControllerActive = false;
        wasWanderMovementActive = false;

        Vector3 retreatDirection = (transform.position - (playerTransform != null ? playerTransform.position : transform.position - transform.forward)).normalized;
        if (playerTransform == null) {
            Debug.LogWarning(gameObject.name + " could not find player for retreat direction, retreating from self.forward.");
        }


        if (enemyController != null && enemyController.enabled)
        {
            wasEnemyControllerActive = true;
            originalSpeedForMovementScript = enemyController.speed;
            enemyController.speed = originalSpeedForMovementScript * retreatSpeedMultiplier; // Increase speed
            // EnemyController needs to be aware it should move AWAY from player or in 'retreatDirection'
            // This might require a temporary mode switch in EnemyController or direct velocity override here.
            // For simplicity, let's assume we directly control Rigidbody2D for retreat if available.
            if (rb != null)
            {
                enemyController.enabled = false; // Temporarily disable its own logic
                rb.linearVelocity = retreatDirection * (originalSpeedForMovementScript * retreatSpeedMultiplier);
            }
            else
            {
                // If no RB, EnemyController needs a way to move in 'retreatDirection'
                // This is more complex and better handled by modifying EnemyController.
                Debug.LogWarning(gameObject.name + " is retreating via EnemyController without Rigidbody2D. Retreat direction might not be accurate.");
            }
        }
        else if (wanderMovement != null && wanderMovement.enabled)
        {
            wasWanderMovementActive = true;
            originalSpeedForMovementScript = wanderMovement.speed;
            wanderMovement.speed = originalSpeedForMovementScript * retreatSpeedMultiplier;
            // WanderMovement needs to be made to move in 'retreatDirection'
            if (rb != null)
            {
                wanderMovement.enabled = false; // Temporarily disable its own logic
                rb.linearVelocity = retreatDirection * (originalSpeedForMovementScript * retreatSpeedMultiplier);
            }
             else
            {
                Debug.LogWarning(gameObject.name + " is retreating via WanderMovement without Rigidbody2D. Retreat direction might not be accurate.");
            }
        }
        else if (rb != null) // No specific movement script, but has Rigidbody2D
        {
             // Assume a default speed if no movement script was active to get originalSpeed
            float baseSpeed = 2f; // Fallback base speed
            rb.linearVelocity = retreatDirection * (baseSpeed * retreatSpeedMultiplier);
        }


        // --- 2. Wait for retreat duration ---
        yield return new WaitForSeconds(retreatDuration);

        // --- 3. Restore original movement ---
        Debug.Log(gameObject.name + " is ending retreat.");
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Stop retreat movement
        }

        if (wasEnemyControllerActive && enemyController != null)
        {
            enemyController.speed = originalSpeedForMovementScript;
            enemyController.enabled = true; // Re-enable its logic
        }
        else if (wasWanderMovementActive && wanderMovement != null)
        {
            wanderMovement.speed = originalSpeedForMovementScript;
            wanderMovement.enabled = true; // Re-enable its logic
        }
        // If only Rigidbody2D was used, it's now stopped. If it needs to resume some default, handle here.

        // --- 4. Reset HP (e.g., to full or a fraction) ---
        currentHp = Random.Range(maxHpMin, maxHpMax + 1); // Or set to a specific value like maxHpMax
        Debug.Log(gameObject.name + " HP reset to " + currentHp);

        isRetreating = false;
    }

    void FlashDamageFeedback() // Renamed
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            flashTimer = damageFlashDuration;
        }
    }

    // Die() is for PERMANENT death, called if something else kills it (e.g., an instant kill trap)
    public void Die()
    {
        if (isDying) return;
        isDying = true;

        // If it was retreating, stop that
        if (isRetreating)
        {
            StopCoroutine(RetreatRoutine());
            isRetreating = false;
            // Restore any modified movement script states if necessary before destroying
        }

        Debug.Log(gameObject.name + " permanently DIED!");
        Destroy(gameObject);
    }
}