using UnityEngine;
using UnityEngine.SceneManagement; // Needed for loading scenes

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Name of the scene to load when the player dies.")]
    public string deathSceneName = "DeathScene"; // Ensure this matches your death scene's filename

    [Header("Debug")]
    [SerializeField] // Show in inspector but not editable directly by other scripts easily
    private bool isDead = false;

    // References (Optional, but good practice)
    private PlayerController playerController; // To potentially disable controls on death
    private SpriteRenderer spriteRenderer; // To potentially flash or change color on death

    void Awake()
    {
        // Get references to other components on the same GameObject
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // --- Collision Detection for Hazards ---

    // This method is called by Unity when another Collider2D enters this object's trigger collider.
    // For this to work:
    // 1. Your Player GameObject MUST have a Collider2D component (e.g., BoxCollider2D).
    // 2. EITHER the Player's Collider2D OR the Ghost's Collider2D (or both) MUST have "Is Trigger" checked.
    // 3. Both Player and Ghost MUST have a Rigidbody2D component.
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we're already dead to prevent multiple triggers
        if (isDead) return;

        // Check if the object we collided with is tagged as "Ghost"
        if (other.gameObject.CompareTag("Ghost"))
        {
            Debug.Log("Player Health: Detected collision with Ghost!");
            Die();
        }

        // You could add checks for other hazards here:
        // else if (other.gameObject.CompareTag("Spikes")) { Die(); }
        // else if (other.gameObject.CompareTag("Lava")) { Die(); }
    }

    // Alternative: Use OnCollisionEnter2D if NEITHER your player nor the ghost collider has "Is Trigger" checked.
    /*
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Ghost"))
        {
            Debug.Log("Player Health: Detected collision with Ghost!");
            Die();
        }
    }
    */

    // --- Death Logic ---

    private void Die()
    {
        // Mark as dead to prevent this function running multiple times
        isDead = true;

        Debug.Log("Player Died! Initiating death sequence.");

        // Optional: Disable player controls immediately
        if (playerController != null)
        {
            playerController.enabled = false; // Disables the PlayerController script
            // You might also want to stop any existing movement:
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero; // Stop movement
            }
        }

        // Optional: Visual/Audio Feedback for death
        if (spriteRenderer != null)
        {
            // Example: Make the player red
            // spriteRenderer.color = Color.red;
        }
        // Play death sound effect here if you have one
        // AudioSource.PlayClipAtPoint(deathSoundClip, transform.position);

        // Load the Death Scene
        // Consider a small delay if you want death animations/sounds to play first
        // Invoke("LoadDeathScene", 1.0f); // Example: Load after 1 second
        LoadDeathScene(); // Load immediately for now
    }

    private void LoadDeathScene()
    {
        Debug.Log("Loading Scene: " + deathSceneName);
        SceneManager.LoadScene(deathSceneName);
    }

    // --- Optional: Health Points (If you want damage instead of instant death later) ---
    /*
    public int maxHealth = 1;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Optional: Damage feedback (flash red, etc.)

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    */
}