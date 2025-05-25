using UnityEngine;

public class BossDeathHandler : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private string bossName = "Demon Boss";
    
    [Header("Spawn Locations")]
    [SerializeField] private Vector3 fragmentSpawnPosition = new Vector3(343.48999f, 1.90999997f, -0.980000019f);
    [SerializeField] private Vector3 portalSpawnPosition = new Vector3(353.179993f, 3.1099999f, -0.319999993f);
    
    [Header("Prefabs to Spawn")]
    [SerializeField] private GameObject memoryFragmentPrefab;
    [SerializeField] private GameObject portalPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private bool showSpawnEffects = true;
    [SerializeField] private GameObject spawnEffect;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showSpawnPositions = true;
    
    // Internal state
    private bool hasSpawnedRewards = false;
    
    private void Start()
    {
        // Try to find boss health if not assigned
        if (bossHealth == null)
        {
            bossHealth = GetComponent<EnemyHealth>();
            if (bossHealth == null)
            {
                // Look for demon in the scene
                GameObject demonObject = GameObject.FindGameObjectWithTag("Demon");
                if (demonObject != null)
                {
                    bossHealth = demonObject.GetComponent<EnemyHealth>();
                }
            }
        }
        
        if (bossHealth != null)
        {
            bossHealth.OnDeath += HandleBossDeath;
            DebugLog($"✅ Boss Death Handler connected to {bossHealth.gameObject.name}");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name}: No EnemyHealth found for boss! Please assign it in the inspector.");
        }
        
        ValidatePrefabs();
        DebugLog($"🎯 Boss Death Handler initialized for {bossName}");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (bossHealth != null)
        {
            bossHealth.OnDeath -= HandleBossDeath;
        }
    }
    
    private void ValidatePrefabs()
    {
        if (memoryFragmentPrefab == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Memory Fragment Prefab not assigned!");
        }
        
        if (portalPrefab == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Portal Prefab not assigned!");
        }
    }
    
    private void HandleBossDeath()
    {
        if (hasSpawnedRewards)
        {
            DebugLog("Boss rewards already spawned, ignoring duplicate death event");
            return;
        }
        
        hasSpawnedRewards = true;
        
        DebugLog($"💀 {bossName} has been defeated! Spawning rewards...");
        
        // Use coroutine for delayed spawning
        StartCoroutine(SpawnRewardsWithDelay());
    }
    
    private System.Collections.IEnumerator SpawnRewardsWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        
        SpawnMemoryFragment();
        SpawnPortal();
    }
    
    private void SpawnMemoryFragment()
    {
        if (memoryFragmentPrefab == null)
        {
            Debug.LogError($"❌ Cannot spawn memory fragment - prefab not assigned!");
            return;
        }
        
        DebugLog($"💎 Spawning memory fragment at {fragmentSpawnPosition}");
        
        // Spawn effect first
        if (showSpawnEffects && spawnEffect != null)
        {
            Instantiate(spawnEffect, fragmentSpawnPosition, Quaternion.identity);
        }
        
        // Spawn the fragment
        GameObject fragment = Instantiate(memoryFragmentPrefab, fragmentSpawnPosition, Quaternion.identity);
        fragment.name = "Boss Memory Fragment";
        
        // Ensure it has the MemoryFragment component
        if (fragment.GetComponent<MemoryFragment>() == null)
        {
            fragment.AddComponent<MemoryFragment>();
            DebugLog("Added MemoryFragment component to spawned object");
        }
        
        DebugLog($"✅ Memory fragment spawned successfully at {fragmentSpawnPosition}");
    }
    
    private void SpawnPortal()
    {
        if (portalPrefab == null)
        {
            Debug.LogError($"❌ Cannot spawn portal - prefab not assigned!");
            return;
        }
        
        DebugLog($"🌀 Spawning portal at {portalSpawnPosition}");
        
        // Spawn effect first
        if (showSpawnEffects && spawnEffect != null)
        {
            Instantiate(spawnEffect, portalSpawnPosition, Quaternion.identity);
        }
        
        // Spawn the portal
        GameObject portal = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
        portal.name = "Boss Victory Portal";
        
        // Ensure it has the Portal component
        if (portal.GetComponent<Portal>() == null)
        {
            portal.AddComponent<Portal>();
            DebugLog("Added Portal component to spawned object");
        }
        
        DebugLog($"✅ Portal spawned successfully at {portalSpawnPosition}");
    }
    
    // Manual testing methods
    [ContextMenu("🧪 Test Boss Death")]
    public void TestBossDeath()
    {
        DebugLog("Testing boss death manually...");
        HandleBossDeath();
    }
    
    [ContextMenu("💎 Spawn Fragment Only")]
    public void TestSpawnFragment()
    {
        SpawnMemoryFragment();
    }
    
    [ContextMenu("🌀 Spawn Portal Only")]
    public void TestSpawnPortal()
    {
        SpawnPortal();
    }
    
    [ContextMenu("📍 Show Spawn Positions")]
    public void ShowSpawnPositions()
    {
        DebugLog($"Fragment spawn position: {fragmentSpawnPosition}");
        DebugLog($"Portal spawn position: {portalSpawnPosition}");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BossDeathHandler] {message}");
        }
    }
    
    // Visualize spawn positions in Scene view
    private void OnDrawGizmos()
    {
        if (!showSpawnPositions) return;
        
        // Draw fragment spawn position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(fragmentSpawnPosition, 0.5f);
        Gizmos.DrawCube(fragmentSpawnPosition, Vector3.one * 0.2f);
        
        // Draw portal spawn position
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(portalSpawnPosition, 1f);
        Gizmos.DrawCube(portalSpawnPosition, Vector3.one * 0.3f);
        
        #if UNITY_EDITOR
        // Labels in Scene view
        UnityEditor.Handles.Label(fragmentSpawnPosition + Vector3.up * 1f, "Memory Fragment Spawn");
        UnityEditor.Handles.Label(portalSpawnPosition + Vector3.up * 1.5f, "Portal Spawn");
        #endif
    }
} 