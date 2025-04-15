using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AcidRainDamage : MonoBehaviour
{
    [Tooltip("How much damage each droplet deals to the player on collision.")]
    public float dropletDamage = 1f;

    // We'll get the ParticleSystem automatically
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // OnParticleCollision is called when particles from this system collide with another object
    void OnParticleCollision(GameObject other)
    {
        // Check if the collided object is tagged as "Player"
        if (other.CompareTag("Player"))
        {
            // Attempt to get the PlayerController script
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                // Create a DamageInfo with dropletDamage
                // The "DamageSource" can be the droplet's origin (this Cloud's transform)
                DamageInfo info = new DamageInfo(dropletDamage, transform.position);

                // Call the Player's ApplyDamage method
                pc.ApplyDamage(info);
            }
        }
    }
}
