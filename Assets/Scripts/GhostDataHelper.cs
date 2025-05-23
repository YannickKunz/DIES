using UnityEngine;

[System.Serializable]
public class GhostDataHelper : MonoBehaviour
{
    [Header("📖 GhostData Setup Guide")]
    [TextArea(3, 5)]
    [SerializeField] private string instructions = 
        "Right-click in Project → Create → Game → Ghost Data\n" +
        "Name it 'BasicGhostData'\n" +
        "Set the values shown below:";
    
    [Header("🏃 Basic Enemy Settings (inherited from EnemyData)")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float chaseMemoryDuration = 3f;
    [SerializeField] private float patrolStopDuration = 2f;
    
    [Header("👻 Ghost Movement Settings")]
    [SerializeField] private float hoverSmoothing = 0.2f;
    [SerializeField] private float hoverBobAmplitude = 0.1f;
    [SerializeField] private float hoverBobSpeed = 1.5f;
    [SerializeField] private float jumpForce = 10f;
    
    [Header("⚔️ Ghost Attack Settings")]
    [SerializeField] private float specialAttackCooldown = 5f;
    [SerializeField] private float specialAttackDamage = 4f;
    [SerializeField] private float attackDuration = 1.1f;
    
    [Space(10)]
    [Header("🎮 Quick Actions")]
    [SerializeField] private GhostData targetGhostData;
    
    [ContextMenu("Apply These Values to GhostData")]
    public void ApplyValuesToGhostData()
    {
        if (targetGhostData == null)
        {
            Debug.LogError("Please assign a GhostData asset to 'Target Ghost Data' first!");
            return;
        }
        
        // Apply basic enemy settings
        targetGhostData.moveSpeed = moveSpeed;
        targetGhostData.patrolSpeed = patrolSpeed;
        targetGhostData.detectionRange = detectionRange;
        targetGhostData.attackRange = attackRange;
        targetGhostData.attackCooldown = attackCooldown;
        targetGhostData.damage = damage;
        targetGhostData.chaseMemoryDuration = chaseMemoryDuration;
        targetGhostData.patrolStopDuration = patrolStopDuration;
        
        // Apply ghost-specific settings
        targetGhostData.hoverSmoothing = hoverSmoothing;
        targetGhostData.hoverBobAmplitude = hoverBobAmplitude;
        targetGhostData.hoverBobSpeed = hoverBobSpeed;
        targetGhostData.jumpForce = jumpForce;
        targetGhostData.specialAttackCooldown = specialAttackCooldown;
        targetGhostData.specialAttackDamage = specialAttackDamage;
        targetGhostData.attackDuration = attackDuration;
        
        // Mark as dirty so Unity saves the changes
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetGhostData);
        #endif
        
        Debug.Log($"✅ Applied all values to {targetGhostData.name}!");
    }
    
    [ContextMenu("Create New GhostData Asset")]
    public void CreateNewGhostDataAsset()
    {
        #if UNITY_EDITOR
        // Create a new GhostData asset
        GhostData newGhostData = ScriptableObject.CreateInstance<GhostData>();
        
        // Apply the values
        newGhostData.moveSpeed = moveSpeed;
        newGhostData.patrolSpeed = patrolSpeed;
        newGhostData.detectionRange = detectionRange;
        newGhostData.attackRange = attackRange;
        newGhostData.attackCooldown = attackCooldown;
        newGhostData.damage = damage;
        newGhostData.chaseMemoryDuration = chaseMemoryDuration;
        newGhostData.patrolStopDuration = patrolStopDuration;
        newGhostData.hoverSmoothing = hoverSmoothing;
        newGhostData.hoverBobAmplitude = hoverBobAmplitude;
        newGhostData.hoverBobSpeed = hoverBobSpeed;
        newGhostData.jumpForce = jumpForce;
        newGhostData.specialAttackCooldown = specialAttackCooldown;
        newGhostData.specialAttackDamage = specialAttackDamage;
        newGhostData.attackDuration = attackDuration;
        
        // Save it as an asset
        string path = "Assets/BasicGhostData.asset";
        UnityEditor.AssetDatabase.CreateAsset(newGhostData, path);
        UnityEditor.AssetDatabase.SaveAssets();
        
        // Select it in the project
        UnityEditor.Selection.activeObject = newGhostData;
        
        Debug.Log($"✅ Created new GhostData asset at {path}!");
        #endif
    }
} 