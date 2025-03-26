using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // Assign the player's Transform in the Inspector
    public float speed = 2f; // Speed at which the enemy moves toward the player

    void Update()
    {
        if (player != null)
        {
            // Calculate the direction to the player
            Vector3 direction = (player.position - transform.position).normalized;

            // Move the enemy toward the player
            transform.position += direction * speed * Time.deltaTime;

            // Optional: Rotate the enemy to face the player
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
        }
    }
}