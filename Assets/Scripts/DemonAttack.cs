using UnityEngine;
using System.Collections;

public class DemonAttack : EnemyAttack
{
    [Header("Special Attack Settings")]
    [Tooltip("Radius for special demon attack (larger than normal attack)")]
    [SerializeField] private float specialAttackRadius = 1.5f;
    [Tooltip("Cooldown time between special attacks")]
    [SerializeField] private float specialAttackCooldown = 5f;
    [Tooltip("Visual effect spawned during special attacks")]
    [SerializeField] private GameObject specialAttackEffectPrefab;
    
    [Header("Attack Timing")]
    [Tooltip("Delay before damage is applied (allows animation to play)")]
    [SerializeField] private float attackDelay = 0.3f;
    
    // References
    private DemonData demonData;
    private float lastSpecialAttackTime = -Mathf.Infinity;
    
    // Add a flag to track if attack is in progress
    private bool isAttacking = false;
    public bool IsAttacking => isAttacking;
    
    // Override CanAttack to handle null data gracefully
    public new bool CanAttack 
    {
        get 
        {
            if (data != null)
                return Time.time >= lastAttackTime + data.attackCooldown;
            else
                return Time.time >= lastAttackTime + 1.5f; // Safe fallback cooldown
        }
    }
    
    // Override AttackDuration for demon-specific timing
    public new float AttackDuration 
    {
        get 
        {
            if (demonData != null)
                return demonData.attackDuration;
            else if (data != null)
                return 1.2f; // Demon attacks take a bit longer than basic enemy attacks
            else
                return 1.2f; // Safe fallback
        }
    }
    
    public new void Initialize(EnemyData enemyData)
    {
        base.Initialize(enemyData);
        
        // Cast to DemonData if possible, but don't require it
        if (enemyData is DemonData)
        {
            demonData = (DemonData)enemyData;
            Debug.Log($"{gameObject.name}: DemonAttack initialized with DemonData (special attacks available)");
        }
        else
        {
            Debug.Log($"{gameObject.name}: DemonAttack initialized with base EnemyData (using fallback values)");
        }
        
        // Ensure player layer is set if it wasn't set by base class
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
            Debug.Log($"{gameObject.name}: Auto-set player layer to 'Player' layer");
        }
        
        // Debug attack setup for demons
        Debug.Log($"{gameObject.name}: DemonAttack ready - CanAttack: {CanAttack}, AttackDuration: {AttackDuration}, PlayerLayer: {playerLayer.value}");
    }
    
    // Check if special attack is available
    public bool CanSpecialAttack => Time.time >= lastSpecialAttackTime + 
        (demonData?.specialAttackCooldown ?? specialAttackCooldown);
        
    // Perform special attack
    public void PerformSpecialAttack()
    {
        if (!CanSpecialAttack)
            return;
            
        lastSpecialAttackTime = Time.time;
        isAttacking = true;
        
        Debug.Log($"{gameObject.name}: Performing special attack!");
        
        // Trigger special attack animation via DemonAnimator
        DemonAnimator demonAnimator = GetComponent<DemonAnimator>();
        if (demonAnimator != null)
        {
            Debug.Log($"{gameObject.name}: Playing special attack animation");
            demonAnimator.PlaySpecialAttackAnimation();
        }
        else 
        {
            Debug.LogWarning($"{gameObject.name}: No DemonAnimator found for special attack!");
        }
        
        // Schedule the actual hit for special attack
        StartCoroutine(SpecialAttackSequence());
    }
    
    private IEnumerator SpecialAttackSequence()
    {
        // Wait for animation to reach damage frame
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"{gameObject.name}: Executing special attack hit");
        
        // Special attack uses a larger radius
        Collider2D hitPlayer = Physics2D.OverlapCircle(
            attackPoint.position, 
            specialAttackRadius, 
            playerLayer
        );
        
        if (hitPlayer != null)
        {
            // Apply enhanced damage
            float specialDamage = demonData?.specialAttackDamage ?? 4f;
            DamageInfo info = new DamageInfo(specialDamage, transform.position);
            
            hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
            
            Debug.Log($"{gameObject.name}: Special attack hit for {specialDamage} damage!");
            
            // Spawn special effect if available
            if (specialAttackEffectPrefab != null)
            {
                Instantiate(
                    specialAttackEffectPrefab, 
                    hitPlayer.transform.position, 
                    Quaternion.identity
                );
            }
        }
        else
        {
            Debug.Log($"{gameObject.name}: Special attack missed - no player in range");
        }
        
        // Wait for rest of animation
        yield return new WaitForSeconds(AttackDuration - 0.5f);
        
        // Reset attacking flag
        isAttacking = false;
        Debug.Log($"{gameObject.name}: Special attack sequence completed");
    }
    
    // Override the visualization to include special attack radius
    private new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // Also show special attack radius
        if (attackPoint != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.4f); // Orange
            Gizmos.DrawWireSphere(attackPoint.position, specialAttackRadius);
        }
    }

    // Override the PerformAttack to integrate better with Demon-specific systems
    public override void PerformAttack()
    {
        if (!CanAttack)
        {
            Debug.Log($"{gameObject.name}: Cannot attack - on cooldown");
            return;
        }
        
        lastAttackTime = Time.time;
        isAttacking = true;
        
        Debug.Log($"{gameObject.name}: Performing demon attack!");
        
        // Trigger attack animation via DemonAnimator if available
        DemonAnimator demonAnimator = GetComponent<DemonAnimator>();
        if (demonAnimator != null)
        {
            Debug.Log($"{gameObject.name}: Found DemonAnimator, attempting to play attack animation");
            demonAnimator.PlayAttackAnimation();
        }
        else 
        {
            Debug.LogWarning($"{gameObject.name}: No DemonAnimator found!");
            
            // Fallback to base animator
            if (animator != null)
            {
                Debug.Log($"{gameObject.name}: Trying fallback animator");
                animator.PlayAttackAnimation();
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: No animator components found at all!");
            }
        }
        
        // Schedule the actual hit
        StartCoroutine(HitAfterDelay(attackDelay));
    }

    private IEnumerator HitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"{gameObject.name}: Executing attack hit after {delay}s delay");
        
        try
        {
            // Check for player in attack range
            OnAttackHit();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{gameObject.name}: Error during attack hit: {e.Message}");
        }
        
        // Wait until attack animation would be done
        float remainingDuration = AttackDuration - attackDelay;
        if (remainingDuration > 0)
        {
            yield return new WaitForSeconds(remainingDuration);
        }
        
        // Always reset attacking flag, even if there was an error
        isAttacking = false;
        Debug.Log($"{gameObject.name}: Attack sequence completed, isAttacking = false");
    }

    // Override OnAttackHit to handle null data gracefully
    public new void OnAttackHit()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: No attack point found for demon attack!");
            return;
        }

        // Check for player in attack range
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, AttackRadius, playerLayer);
        if (hitPlayer != null)
        {
            // Get damage value safely
            float damageAmount = 2f; // Default demon damage
            if (data != null)
                damageAmount = data.damage;
            else if (demonData != null)
                damageAmount = demonData.damage;

            Debug.Log($"{gameObject.name}: Hit player with damage {damageAmount}!");
            
            // Apply damage
            DamageInfo info = new DamageInfo(damageAmount, transform.position);
            hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.Log($"{gameObject.name}: Attack missed - no player in range");
        }
    }
} 