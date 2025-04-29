using UnityEngine;

public class WanderMovement : MonoBehaviour
{
    public float speed = 1.5f;
    public float wanderRadius = 5f; // How far it might wander from its start point
    public float timeToChangeDirection = 3f; // How often it picks a new destination

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;
    private Rigidbody2D rb; // Use Rigidbody for movement

    void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
         if (rb == null) {
            Debug.LogError("WanderMovement requires a Rigidbody2D component!", this);
            enabled = false; // Disable script if no Rigidbody2D
            return;
        }
        rb.isKinematic = false; // We'll control via velocity
        rb.gravityScale = 0; // No gravity for ghosts usually
        PickNewWanderTarget();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeToChangeDirection)
        {
            PickNewWanderTarget();
        }
    }

    void FixedUpdate() // Apply physics movement in FixedUpdate
    {
        if(rb != null) {
            Vector2 direction = (targetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;

            // Optional: Stop if very close to target to prevent jittering
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                rb.linearVelocity = Vector2.zero;
                PickNewWanderTarget(); // Pick a new one immediately
            }
        }
    }

    void PickNewWanderTarget()
    {
        // Pick a random point within the wander radius around the start position
        Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
        targetPosition = startPosition + new Vector3(randomDirection.x, randomDirection.y, 0);
        timer = 0f; // Reset timer
         // Optional: Clamp targetPosition to stay within level bounds if needed
    }
}