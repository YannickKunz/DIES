// GhostAttack.cs
using UnityEngine;
using System.Collections;

public class GhostAttack : EnemyAttack
{
    [Header("Special Attack Settings")]
    [SerializeField] private float specialAttackRadius = 1.5f;
    [SerializeField] private float specialAttackCooldown = 5f;
    [SerializeField] private GameObject specialAttackEffectPrefab;
    
    // References
    private GhostData ghostData;
    private float lastSpecialAttackTime = -Mathf.Infinity;
    
    public new void Initialize(EnemyData enemyData)
    {
        base.Initialize(enemyData);
        
        // Cast to GhostData if possible
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
        }
    }
    
    // Check if special attack is available
    public bool CanSpecialAttack => Time.time >= lastSpecialAttackTime + 
        (ghostData?.specialAttackCooldown ?? specialAttackCooldown);
        
    // Perform special attack
    public void PerformSpecialAttack()
    {
        if (!CanSpecialAttack)
            return;
            
        lastSpecialAttackTime = Time.time;
        
        // Trigger special attack animation (controlled by GhostAI)
        StartCoroutine(DealSpecialDamage(0.5f));
    }
    
    private IEnumerator DealSpecialDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Special attack uses a larger radius
        Collider2D hitPlayer = Physics2D.OverlapCircle(
            attackPoint.position, 
            specialAttackRadius, 
            playerLayer
        );
        
        if (hitPlayer != null)
        {
            // Apply enhanced damage
            DamageInfo info = new DamageInfo(
                ghostData?.specialAttackDamage ?? 4f, 
                transform.position
            );
            
            hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
            
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
}