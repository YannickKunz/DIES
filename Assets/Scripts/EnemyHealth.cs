// EnemyHealth.cs
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    private EnemyData data;
    private float currentHealth;
    private float maxHealth; // Store max health for percentage calculations
    
    // 👁️ EXPOSE HEALTH IN INSPECTOR FOR DEBUGGING
    [Header("Debug Info (Read Only)")]
    [SerializeField] private float debugCurrentHealth;
    [SerializeField] private float debugMaxHealth;
    [SerializeField] private bool debugIsInitialized;
    
    public event Action OnDeath;
    public event Action<float> OnDamage;
    
    private EnemyAnimator animator;
    
    // Update debug values every frame so they show in inspector
    private void Update()
    {
        debugCurrentHealth = currentHealth;
        debugMaxHealth = maxHealth;
        debugIsInitialized = (maxHealth > 0);
    }
    
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        
        // Handle null data gracefully with fallback values
        if (data != null)
        {
            maxHealth = data.maxHealth;
            currentHealth = data.maxHealth;
            Debug.Log($"🏥 {gameObject.name}: EnemyHealth initialized with {currentHealth} health from data");
        }
        else
        {
            // Fallback health for demons without data
            maxHealth = 10f; // Default max health
            currentHealth = 10f; // Default current health
            Debug.Log($"🏥 {gameObject.name}: EnemyHealth initialized with fallback health: {currentHealth}");
        }
        
        animator = GetComponent<EnemyAnimator>();
    }
    
    public void TakeDamage(float amount, Vector3 source)
    {
        Debug.Log($"🩸 {gameObject.name}: TakeDamage called - Amount: {amount}, Current Health: {currentHealth}");
        
        if (currentHealth <= 0) 
        {
            Debug.Log($"💀 {gameObject.name}: Already dead, ignoring damage");
            return;
        }
        
        // Subtract damage
        currentHealth -= amount;
        Debug.Log($"💔 {gameObject.name}: Health reduced to {currentHealth}");
        
        // Trigger damage event
        Debug.Log($"📢 {gameObject.name}: Invoking OnDamage event with {amount} damage");
        OnDamage?.Invoke(amount);
        
        // Show hit effect
        if (data != null && data.hitEffectPrefab)
        {
            Debug.Log($"💥 {gameObject.name}: Spawning hit effect");
            Instantiate(data.hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Show hit animation
        if (animator)
        {
            Debug.Log($"🎬 {gameObject.name}: Playing hit animation via animator");
            animator.PlayHitAnimation();
        }
        else
        {
            Debug.LogWarning($"❌ {gameObject.name}: No animator found for hit animation!");
        }
        
        // Check for death
        if (currentHealth <= 0)
        {
            Debug.Log($"☠️ {gameObject.name}: Health depleted, calling Die()");
            Die();
        }
    }

    // Add this method to your EnemyHealth.cs
    public void ApplyDamage(DamageInfo damageInfo)
    {
        Debug.Log($"🎯 {gameObject.name}: ApplyDamage called via DamageInfo - Amount: {damageInfo.DamageAmount}, Source: {damageInfo.DamageSource}");
        TakeDamage(damageInfo.DamageAmount, damageInfo.DamageSource);
    }

    // Also add this method for backwards compatibility
    public void ApplyDamage(float damage)
    {
        Debug.Log($"🎯 {gameObject.name}: ApplyDamage called with float - Amount: {damage}");
        TakeDamage(damage, transform.position);
    }
    
    private void Die()
    {
        Debug.Log($"💀 {gameObject.name}: Die() called - triggering death sequence");
        
        // Trigger death animation
        if (animator)
        {
            Debug.Log($"🎬 {gameObject.name}: Playing death animation");
            animator.PlayDeathAnimation();
        }
        
        // Disable collisions
        Collider2D col = GetComponent<Collider2D>();
        if (col) 
        {
            Debug.Log($"🚫 {gameObject.name}: Disabling collider");
            col.enabled = false;
        }
        
        // Disable rigidbody physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            Debug.Log($"⏸️ {gameObject.name}: Setting rigidbody to kinematic");
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
        }
        
        // Notify subscribers
        Debug.Log($"📢 {gameObject.name}: Invoking OnDeath event");
        OnDeath?.Invoke();
        
        // Destroy after delay
        Debug.Log($"🗑️ {gameObject.name}: Scheduling destruction in 2 seconds");
        Destroy(gameObject, 2f);
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    [ContextMenu("🔥 Test Damage (5 HP)")]
    public void TestDamage()
    {
        Debug.Log($"🧪 {gameObject.name}: MANUAL TEST DAMAGE");
        TakeDamage(5f, transform.position);
    }
    
    [ContextMenu("📊 Show Current Health")]
    public void ShowCurrentHealth()
    {
        Debug.Log($"💖 {gameObject.name}: Current Health = {currentHealth}/{maxHealth} (Initialized: {maxHealth > 0})");
    }
}