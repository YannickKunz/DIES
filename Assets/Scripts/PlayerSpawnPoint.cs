using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(-29.5f, -11.3f, 0f);
    [SerializeField] private bool useThisObjectPosition = false;
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool showSpawnGizmo = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Setup Helper")]
    [SerializeField] private bool showSetupButtons = true;
    
    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnPlayerAtPosition();
        }
    }
    
    public void SpawnPlayerAtPosition()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("❌ PlayerSpawnPoint: No player found with 'Player' tag!");
            return;
        }
        
        // Determine spawn position
        Vector3 targetPosition = useThisObjectPosition ? transform.position : spawnPosition;
        
        // Move the player
        player.transform.position = targetPosition;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 PlayerSpawnPoint: Moved player to {targetPosition}");
        }
        
        // Reset player physics if they have a Rigidbody2D
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
        
        // Reset any movement states
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Reset any movement flags if needed
            if (enableDebugLogs)
            {
                Debug.Log("🔄 PlayerSpawnPoint: Reset player movement states");
            }
        }
    }
    
    // Visualize spawn point in Scene view
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmo) return;
        
        Vector3 gizmoPosition = useThisObjectPosition ? transform.position : spawnPosition;
        
        // Draw spawn point indicator
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gizmoPosition, 1f);
        Gizmos.DrawWireCube(gizmoPosition, Vector3.one * 0.5f);
        
        // Draw up arrow
        Gizmos.DrawLine(gizmoPosition, gizmoPosition + Vector3.up * 2f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(gizmoPosition + Vector3.up * 2.5f, "Player Spawn");
        #endif
    }
    
    // 🆕 HELPER: Copy current player position as spawn position
    [ContextMenu("📍 Set Spawn to Current Player Position")]
    public void SetSpawnToCurrentPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            spawnPosition = player.transform.position;
            Debug.Log($"📍 Spawn position set to current player position: {spawnPosition}");
        }
        else
        {
            Debug.LogError("❌ No player found to copy position from!");
        }
    }
    
    // 🆕 HELPER: Move this object to current player position  
    [ContextMenu("📦 Move This Object to Player")]
    public void MoveThisObjectToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            transform.position = player.transform.position;
            Debug.Log($"📦 Moved spawn point object to player position: {transform.position}");
        }
        else
        {
            Debug.LogError("❌ No player found to move to!");
        }
    }
} 