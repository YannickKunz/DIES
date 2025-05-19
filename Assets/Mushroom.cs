using UnityEngine;

public class Mushroom : MonoBehaviour
{
    // Optional: Sound effect to play on collection
    public AudioClip collectionSound;
    // Optional: Particle effect to spawn on collection
    public GameObject collectionEffectPrefab;

    // This variable will be used to ensure the mushroom is collected only once.
    private bool isCollected = false;

    // This method is called by Unity when another Collider2D enters this object's trigger.
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if already collected to prevent multiple collections (e.g., if player quickly re-enters trigger)
        if (isCollected)
        {
            return;
        }

        // Check if the object that entered the trigger is the Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Mushroom collected by: " + other.name);
            isCollected = true; // Mark as collected

            // --- Notify the GameManager (or Player) that a mushroom was collected ---
            // We'll create the GameManager script next.
            // This is a common way to find a singleton-style GameManager.
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.MushroomCollected();
            }
            else
            {
                Debug.LogError("GameManager not found in the scene!");
            }

            // Optional: Play sound effect
            if (collectionSound != null)
            {
                AudioSource.PlayClipAtPoint(collectionSound, transform.position);
            }

            // Optional: Spawn particle effect
            if (collectionEffectPrefab != null)
            {
                Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy the mushroom GameObject
            Destroy(gameObject);
        }
    }
}