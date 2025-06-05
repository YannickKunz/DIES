using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // Assign the player's Transform in the Inspector
    public float speed = 2f; // Speed at which the enemy moves toward the player
    private bool isPlayerInRange = false; // Tracks if the player is within the trigger
    public bool alwaysActive = false; // Set this to true for enemies that should always move


    // --- Private Variables ---
    private SpriteRenderer spriteRenderer; // **NEW**: Reference to the sprite renderer

    void Awake()
    {
        // **NEW**: Get the SpriteRenderer component attached to this GameObject.
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("EnemyController requires a SpriteRenderer component.", this);
        }

        // It's good practice to find the player by tag if it's not assigned,
        // but assigning in the Inspector is fine too.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }
    void Update()
    {

        // Gradually increase speed over time
        speed += 0.1f * Time.deltaTime;

        // Enemy moves if alwaysActive is true or if the player has entered the trigger
        if ((alwaysActive || isPlayerInRange) && player != null)
        {
            // Calculate the direction to the player
            Vector3 direction = (player.position - transform.position).normalized;
            // Prevent vertical movement
            direction.y = 0f;
            // Move the enemy toward the player
            transform.position += direction * speed * Time.deltaTime;


            // --- **NEW: SPRITE FLIPPING LOGIC** ---
            // Check the horizontal direction
            if (direction.x > 0)
            {
                // Moving Right - ensure sprite is not flipped
                spriteRenderer.flipX = false;
            }
            else if (direction.x < 0)
            {
                // Moving Left - flip the sprite
                spriteRenderer.flipX = true;
            }

            // Optional: Rotate the enemy to face the player
            // Quaternion lookRotation = Quaternion.LookRotation(direction);
            // transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger!"); // Debug log
            isPlayerInRange = true; // Enable movement when the player is in range
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Do nothing here to ensure the enemy keeps moving even after the player leaves the trigger
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited the trigger!"); // Debug log
        }
    }
}