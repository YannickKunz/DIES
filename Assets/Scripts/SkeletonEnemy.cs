// SkeletonEnemy.cs
using UnityEngine;

public class SkeletonEnemy : BaseEnemy
{
    [Header("Skeleton-Specific Settings")]
    [SerializeField] private bool useBonePile = false; // Leave bone pile when dying
    [SerializeField] private GameObject bonePilePrefab;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to death event for skeleton-specific behavior
        if (health != null)
        {
            health.OnDeath += HandleSkeletonDeath;
        }
    }
    
    private void HandleSkeletonDeath()
    {
        if (useBonePile && bonePilePrefab != null)
        {
            Instantiate(bonePilePrefab, transform.position, Quaternion.identity);
        }
    }
    
    // Override for any skeleton-specific behaviors
    public override void SetupEnemy(EnemyData data)
    {
        base.SetupEnemy(data);
        
        // Any additional skeleton-specific setup
        Debug.Log($"Skeleton {gameObject.name} initialized with {data.enemyName} data");
    }
}