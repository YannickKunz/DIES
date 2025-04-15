// BaseEnemy.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BaseEnemy : MonoBehaviour
{
    [SerializeField] protected EnemyData enemyData;
    
    // References to other components
    [HideInInspector] public EnemyHealth health;
    [HideInInspector] public EnemyMovement movement;
    [HideInInspector] public EnemyAI ai;
    [HideInInspector] public EnemyAttack attack;
    [HideInInspector] public EnemyAnimator animator;
    
// In BaseEnemy.cs
    protected virtual void Awake()
    {
        // Get components
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        ai = GetComponent<EnemyAI>();
        attack = GetComponent<EnemyAttack>();
        animator = GetComponent<EnemyAnimator>();
        
        Debug.Log(gameObject.name + ": Components found - " +
                "Health: " + (health != null) +
                ", Movement: " + (movement != null) +
                ", AI: " + (ai != null) +
                ", Attack: " + (attack != null) +
                ", Animator: " + (animator != null));
        
        if (!enemyData)
        {
            Debug.LogError($"No EnemyData assigned to {gameObject.name}!");
            return;
        }
        
        // Initialize components with data
        if (health) health.Initialize(enemyData);
        if (movement) movement.Initialize(enemyData);
        if (ai) ai.Initialize(enemyData);
        if (attack) attack.Initialize(enemyData);
        if (animator) animator.Initialize(enemyData);
    }
    
    // Optional: Override for specific enemy types
    public virtual void SetupEnemy(EnemyData data)
    {
        enemyData = data;
        // Re-initialize components if needed
    }
}