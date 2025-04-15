// EnemyData.cs - Updated version
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Skeleton";
    
    [Header("Health Settings")]
    public float maxHealth = 20f;
    public GameObject hitEffectPrefab;
    
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float patrolSpeed = 1f;
    public float patrolStopDuration = 1f;
    public float groundCheckRadius = 0.2f;
    
    [Header("Detection Settings")]
    public float detectionRange = 7f;
    public float attackRange = 1.5f;
    public float chaseMemoryDuration = 3f;
    
    [Header("Attack Settings")]
    public float attackCooldown = 1.5f;
    public float damage = 2f;
    public float attackRadius = 0.8f;
}