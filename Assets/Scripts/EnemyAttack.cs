// EnemyAttack.cs
using System.Collections;
using UnityEngine;


public class EnemyAttack : MonoBehaviour
{
[Header("Instance Override Settings")]
[SerializeField] private bool useInstanceValues = false;
[Tooltip("Override the attack radius from EnemyData")]
[SerializeField] private float instanceAttackRadius = 0.8f;

public float AttackRadius => useInstanceValues ? instanceAttackRadius : (data != null ? data.attackRadius : 0.8f);

    private EnemyData data;
    private Transform attackPoint;
    private EnemyAnimator animator;
    private float lastAttackTime = -Mathf.Infinity;
    
    [SerializeField] private LayerMask playerLayer;
    
    public void DebugAttackSetup()
    {
        Transform attackCheck = transform.Find("AttackCheck");
        Transform attackPoint = transform.Find("AttackPoint");
        
        Debug.Log($"Enemy {gameObject.name} attack setup:");
        Debug.Log($"- Has AttackCheck: {attackCheck != null}");
        Debug.Log($"- Has AttackPoint: {attackPoint != null}");
        Debug.Log($"- Using point: {(this.attackPoint ? this.attackPoint.name : "none")}");
        Debug.Log($"- Player Layer set: {playerLayer.value != 0}");
    }

    // Add this call to your Initialize method
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        animator = GetComponent<EnemyAnimator>();
        
        // Create attack point if needed
        SetupAttackPoint();
        
        // Debug attack setup
        DebugAttackSetup();
    }

    
    
    private void SetupAttackPoint()
    {
        if (attackPoint == null)
        {
            // First check if there's an existing child with either name
            Transform existingPoint = transform.Find("AttackCheck");
            
            // Fall back to AttackPoint if AttackCheck isn't found
            if (existingPoint == null)
                existingPoint = transform.Find("AttackPoint");
                
            if (existingPoint != null)
            {
                attackPoint = existingPoint;
                Debug.Log(gameObject.name + ": Found existing attack point: " + existingPoint.name);
            }
            else
            {
                // Create new point with standard name if neither exists
                GameObject attackPointObj = new GameObject("AttackCheck");
                attackPointObj.transform.parent = transform;
                attackPointObj.transform.localPosition = new Vector3(0.8f, 0, 0);
                attackPoint = attackPointObj.transform;
                Debug.Log(gameObject.name + ": Created new AttackCheck at " + attackPointObj.transform.localPosition);
            }
        }
        
        // Check player layer
        if (playerLayer.value == 0)
        {
            Debug.LogError(gameObject.name + ": Player Layer not set in EnemyAttack component!");
        }
    }
    
    public void PerformAttack()
    {
        if (!CanAttack)
            return;
            
        lastAttackTime = Time.time;
        
        // Play attack animation
        if (animator != null)
        {
            animator.PlayAttackAnimation();
        }
        
        // Actual damage is dealt via animation event or directly
        StartCoroutine(DealDamageAfterDelay(0.3f));
    }
    
    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check for player in attack range
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, AttackRadius, playerLayer);
        if (hitPlayer != null)
        {
            // Apply damage
            DamageInfo info = new DamageInfo(data.damage, transform.position);
            info.DamageAmount = data.damage;
            info.DamageSource = transform.position;
            hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    // Called by animation event
    public void OnAttackHit()
    {
        // Check for player in attack range
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, AttackRadius, playerLayer);
        if (hitPlayer != null)
        {
            Debug.Log($"Hit player with radius: {AttackRadius}");
            // Apply damage
            DamageInfo info = new DamageInfo(data.damage, transform.position);
            hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    public bool CanAttack => Time.time >= lastAttackTime + data.attackCooldown;
    
    public float AttackDuration => 1.1f; // Total attack animation duration
    
private void OnDrawGizmosSelected()
{
    // Always draw this even when the game is not running
    Transform gizmoAttackPoint = attackPoint;
    
    // If attackPoint is null (likely in edit mode), try to find it
    if (gizmoAttackPoint == null)
    {
        // Try finding the attack point with a recursive search
        gizmoAttackPoint = FindAttackPointRecursively(transform);
        
        // If still not found, show where it would be created
        if (gizmoAttackPoint == null)
        {
            // Draw where the attack point would be if created
            Vector3 projectedPosition = transform.position + transform.right * 0.8f;
            
            // Draw a clear indicator where the attack point would be
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectedPosition, 0.1f);
            Gizmos.DrawLine(transform.position, projectedPosition);
            
            // Draw the attack radius at this projected position
            Gizmos.color = new Color(1, 0, 0, 0.5f);  // More visible red
            float radius = useInstanceValues ? instanceAttackRadius : 
                (data != null ? data.attackRadius : 0.8f);
            Gizmos.DrawWireSphere(projectedPosition, radius);
            
            // Add a label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(projectedPosition, "AttackPoint (projected)");
            #endif
            
            return;
        }
    }
    
    // Draw the attack point clearly
    Gizmos.color = Color.yellow;
    Gizmos.DrawSphere(gizmoAttackPoint.position, 0.1f);
    
    // Draw the attack radius with high visibility
    Gizmos.color = new Color(1, 0, 0, 0.5f);  // More visible red
    float attackRadius = useInstanceValues ? instanceAttackRadius : 
        (data != null ? data.attackRadius : 0.8f);
    Gizmos.DrawWireSphere(gizmoAttackPoint.position, attackRadius);
    
    // Draw a solid sphere at a smaller size for better visibility
    Gizmos.color = new Color(1, 0, 0, 0.2f);  // Semi-transparent red
    Gizmos.DrawSphere(gizmoAttackPoint.position, attackRadius * 0.5f);
    
    // Add a label
    #if UNITY_EDITOR
    UnityEditor.Handles.Label(gizmoAttackPoint.position + Vector3.up * 0.2f, 
        $"Attack Radius: {attackRadius}");
    #endif
}

private Transform FindAttackPointRecursively(Transform parent)
{
    // Check for either name
    if (parent.name == "AttackCheck" || parent.name == "AttackPoint")
        return parent;
        
    foreach (Transform child in parent)
    {
        Transform found = FindAttackPointRecursively(child);
        if (found != null)
            return found;
    }
    
    return null;
}

}