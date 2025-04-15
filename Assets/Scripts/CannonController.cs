using UnityEngine;

public class CannonController : MonoBehaviour 
{
    [Header("Firing Settings")]
    [SerializeField] private GameObject cannonballPrefab;
    [SerializeField] private Transform firingPoint;
    [SerializeField] private float firingInterval = 3f;
    [SerializeField] private float projectileForce = 10f;
    [SerializeField] private float firingAngle = 45f; // Angle in degrees
    
    [Header("Visual Settings")]
    [SerializeField] private bool showTrajectoryPreview = true;
    [SerializeField] private int trajectorySteps = 10;
    [SerializeField] private float trajectoryTimeStep = 0.1f;
    [SerializeField] private Color trajectoryColor = new Color(1f, 0.5f, 0f, 0.5f);
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem firingEffect;
    [SerializeField] private AudioClip firingSound;
    [SerializeField] private float volume = 1f;
    
    private float nextFireTime;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Add AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && firingSound != null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = volume;
        }
        
        // Create firing point if not assigned
        if (firingPoint == null)
        {
            GameObject newFiringPoint = new GameObject("FiringPoint");
            newFiringPoint.transform.parent = transform;
            newFiringPoint.transform.localPosition = new Vector3(0.5f, 0, 0); // Adjust based on your sprite
            firingPoint = newFiringPoint.transform;
            
            Debug.Log("Firing point automatically created. Consider assigning it manually in the Inspector.");
        }
        
        // Initialize next fire time
        nextFireTime = Time.time + firingInterval;
    }
    
    private void Update() 
    {
        // Check if it's time to fire
        if (Time.time >= nextFireTime) 
        {
            FireCannonball();
            nextFireTime = Time.time + firingInterval;
        }
    }
    
    private void FireCannonball() 
    {
        if (cannonballPrefab == null) 
        {
            Debug.LogError("Cannonball prefab not assigned to " + gameObject.name);
            return;
        }
        
        // Create the cannonball at the firing point
        GameObject cannonball = Instantiate(cannonballPrefab, firingPoint.position, Quaternion.identity);
        
        // Get or add the Rigidbody2D component
        Rigidbody2D rb = cannonball.GetComponent<Rigidbody2D>();
        if (rb == null) 
        {
            rb = cannonball.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        // Calculate direction based on angle
        Vector2 direction = Quaternion.Euler(0, 0, firingAngle) * transform.right;
        
        // Apply force to launch the cannonball
        rb.AddForce(direction * projectileForce, ForceMode2D.Impulse);
        
        // Play effects
        if (firingEffect != null) firingEffect.Play();
        if (audioSource != null && firingSound != null) audioSource.PlayOneShot(firingSound);
        
        // If there's a Cannonball component, initialize it
        Cannonball cannonballComponent = cannonball.GetComponent<Cannonball>();
        if (cannonballComponent != null)
        {
            cannonballComponent.Initialize(gameObject);
        }
        
        Debug.Log($"Cannon fired at angle {firingAngle} with force {projectileForce}");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showTrajectoryPreview || firingPoint == null) return;
        
        // Draw the firing direction
        Gizmos.color = trajectoryColor;
        Vector2 direction = Quaternion.Euler(0, 0, firingAngle) * transform.right;
        Gizmos.DrawLine(firingPoint.position, firingPoint.position + (Vector3)(direction * 1.5f));
        
        // Draw predicted trajectory (simplified, doesn't account for collisions)
        Vector2 velocity = direction * projectileForce;
        Vector2 position = firingPoint.position;
        Vector2 acceleration = Physics2D.gravity;
        
        Gizmos.color = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.3f);
        Vector2 previousPos = position;
        
        for (int i = 0; i < trajectorySteps; i++)
        {
            float time = i * trajectoryTimeStep;
            Vector2 pos = position + velocity * time + 0.5f * acceleration * time * time;
            Gizmos.DrawLine(previousPos, pos);
            previousPos = pos;
        }
    }
}