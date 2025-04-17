// EnemyHealth.cs
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    private EnemyData data;
    private float currentHealth;
    
    public event Action OnDeath;
    public event Action<float> OnDamage;
    
    private EnemyAnimator animator;
    
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        currentHealth = data.maxHealth;
        animator = GetComponent<EnemyAnimator>();
    }
    
    public void TakeDamage(float amount, Vector3 source)
    {
        if (currentHealth <= 0) return;
        
        currentHealth -= amount;
        OnDamage?.Invoke(amount);
        
        // Show hit effect
        if (data.hitEffectPrefab)
        {
            Instantiate(data.hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Show hit animation
        if (animator)
        {
            animator.PlayHitAnimation();
        }
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Add this method to your EnemyHealth.cs
    public void ApplyDamage(DamageInfo damageInfo)
    {
        TakeDamage(damageInfo.DamageAmount, damageInfo.DamageSource);
    }

    // Also add this method for backwards compatibility
    public void ApplyDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }
    
    private void Die()
    {
        // Trigger death animation
        if (animator)
        {
            animator.PlayDeathAnimation();
        }
        
        // Disable collisions
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        
        // Disable rigidbody physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
        }
        
        // Notify subscribers
        OnDeath?.Invoke();
        
        // Destroy after delay
        Destroy(gameObject, 2f);
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / data.maxHealth;
    }
}