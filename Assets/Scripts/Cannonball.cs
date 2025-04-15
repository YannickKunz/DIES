using UnityEngine;

public class Cannonball : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 2f;
    [SerializeField] private float damageRadius = 0.5f; // For area damage (optional)
    
    [Header("Physics Settings")]
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float bounceForceReduction = 0.6f; // How much force is lost on bounce
    [SerializeField] private int maxBounces = 3;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem impactEffect;
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private LayerMask collisionLayers;
    
    private GameObject owner; // Who fired this cannonball
    private int bounceCount = 0;
    private bool hasHitPlayer = false;
    private Rigidbody2D rb;
    private float spawnTime;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        // Apply physics material if needed
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f; // Adjust based on sprite size
        }
        
        // Track spawn time for lifetime management
        spawnTime = Time.time;
    }
    
    private void Update()
    {
        // Check max lifetime
        if (Time.time - spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // Optional: Rotate sprite based on velocity direction
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    public void Initialize(GameObject shooter)
    {
        owner = shooter;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we've hit the player
        if (collision.gameObject.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                // Apply damage to player using their existing damage system
                Vector3 damageSource = transform.position;
                DamageInfo damageInfo = new DamageInfo(damage, damageSource);
                player.ApplyDamage(damageInfo);
                Debug.Log($"Player hit by cannonball for {damage} damage");
            }
            
            // Destroy after hitting player
            DestroyCannonball();
            return;
        }
        
        // Handle bounces
        bounceCount++;
        if (bounceCount >= maxBounces)
        {
            DestroyCannonball();
            return;
        }
        
        // Reduce velocity on bounce
        if (rb != null)
        {
            rb.linearVelocity *= bounceForceReduction;
        }
        
        // Play impact effect
        if (impactEffect != null)
        {
            ParticleSystem effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
        
        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position, 0.6f);
        }
    }
    
    private void DestroyCannonball()
    {
        // Create final impact effect
        if (impactEffect != null)
        {
            ParticleSystem effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
        
        // Play sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position, 0.6f);
        }
        
        Destroy(gameObject);
    }
    
    // Draw the damage radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}