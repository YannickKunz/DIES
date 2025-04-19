using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // Assign the player's Transform in the Inspector
    public float speed = 2f; // Speed at which the enemy moves toward the player
    private bool isPlayerInRange = false; // Tracks if the player is within the trigger
    public bool alwaysActive = false; // Set this to true for enemies that should always move

    void Update()
    {
        // Enemy moves if alwaysActive is true or if the player has entered the trigger
        if ((alwaysActive || isPlayerInRange) && player != null)
        {
            // Calculate the direction to the player
            Vector3 direction = (player.position - transform.position).normalized;

            // Move the enemy toward the player
            transform.position += direction * speed * Time.deltaTime;

            // Optional: Rotate the enemy to face the player
            //Quaternion lookRotation = Quaternion.LookRotation(direction);
            //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
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