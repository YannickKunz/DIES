using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyGroupDeathHandler : MonoBehaviour
{
    [Header("Enemy Group Settings")]
    [Tooltip("Tag of enemies to track (e.g., 'Ghost', 'Enemy', 'Skeleton')")]
    [SerializeField] private string enemyTag = "Ghost";
    [Tooltip("How many enemies must be defeated (0 = auto-detect all in scene)")]
    [SerializeField] private int requiredEnemyCount = 0;
    [Tooltip("Friendly name for this enemy group (for debug messages)")]
    [SerializeField] private string groupName = "Ghost Pack";
    
    [Header("Spawn Locations")]
    [SerializeField] private Vector3 fragmentSpawnPosition;
    [SerializeField] private Vector3 portalSpawnPosition;
    
    [Header("Prefabs to Spawn")]
    [SerializeField] private GameObject memoryFragmentPrefab;
    [SerializeField] private GameObject portalPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private bool showSpawnEffects = true;
    [SerializeField] private GameObject spawnEffect;
    
    [Header("Portal Behavior")]
    [Tooltip("Portal only works after collecting the memory fragment")]
    [SerializeField] private bool requireFragmentCollection = true;
    [Tooltip("Scene name to load when portal is used")]
    [SerializeField] private string nextSceneName = "NextLevel";
    [Tooltip("Should this fragment count toward the global fragment collection?")]
    [SerializeField] private bool fragmentCountsGlobally = true;
    
    [Header("UI Feedback")]
    [SerializeField] private bool showProgressMessages = true;
    [SerializeField] private string progressMessagePrefix = "Enemies defeated:";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showSpawnPositions = true;
    [SerializeField] private bool autoFindEnemiesOnStart = true;
    
    // Internal state
    private List<EnemyHealth> trackedEnemies = new List<EnemyHealth>();
    private int defeatedCount = 0;
    private bool hasSpawnedRewards = false;
    private bool memoryFragmentCollected = false;
    private GameObject spawnedFragment;
    private GameObject spawnedPortal;
    private ConditionalPortal conditionalPortal;
    
    // Events for other scripts to listen to
    public System.Action<int, int> OnEnemyDefeated; // (defeated, total)
    public System.Action OnAllEnemiesDefeated;
    public System.Action OnMemoryFragmentCollected;
    public System.Action OnPortalReady;
    
    private void Start()
    {
        if (autoFindEnemiesOnStart)
        {
            FindAndTrackEnemies();
        }
        
        // Set spawn positions to current position if not set
        if (fragmentSpawnPosition == Vector3.zero)
        {
            fragmentSpawnPosition = transform.position + Vector3.right * 2f;
        }
        if (portalSpawnPosition == Vector3.zero)
        {
            portalSpawnPosition = transform.position + Vector3.right * 5f;
        }
        
        ValidateSetup();
    }
    
    [ContextMenu("🔍 Find and Track Enemies")]
    public void FindAndTrackEnemies()
    {
        // Clear existing tracking
        ClearTracking();
        
        // Find all GameObjects with the specified tag
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag(enemyTag);
        DebugLog($"Found {enemyObjects.Length} objects with tag '{enemyTag}'");
        
        foreach (GameObject enemyObj in enemyObjects)
        {
            EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
            if (health != null)
            {
                trackedEnemies.Add(health);
                health.OnDeath += HandleEnemyDeath;
                DebugLog($"Now tracking: {enemyObj.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ {enemyObj.name} has tag '{enemyTag}' but no EnemyHealth component!");
            }
        }
        
        // Set required count if auto-detect is enabled
        if (requiredEnemyCount <= 0)
        {
            requiredEnemyCount = trackedEnemies.Count;
        }
        
        DebugLog($"Tracking {trackedEnemies.Count} enemies. Need to defeat {requiredEnemyCount} to spawn rewards.");
        
        if (trackedEnemies.Count == 0)
        {
            Debug.LogWarning($"⚠️ No enemies found with tag '{enemyTag}' that have EnemyHealth components!");
        }
    }
    
    private void ClearTracking()
    {
        // Unsubscribe from existing events
        foreach (EnemyHealth health in trackedEnemies)
        {
            if (health != null)
            {
                health.OnDeath -= HandleEnemyDeath;
            }
        }
        
        trackedEnemies.Clear();
        defeatedCount = 0;
        hasSpawnedRewards = false;
        memoryFragmentCollected = false;
    }
    
    private void HandleEnemyDeath()
    {
        defeatedCount++;
        DebugLog($"💀 Enemy defeated! Progress: {defeatedCount}/{requiredEnemyCount}");
        
        // Notify other scripts
        OnEnemyDefeated?.Invoke(defeatedCount, requiredEnemyCount);
        
        // Show progress message
        if (showProgressMessages)
        {
            Debug.Log($"📊 {progressMessagePrefix} {defeatedCount}/{requiredEnemyCount}");
        }
        
        // Check if all enemies are defeated
        if (defeatedCount >= requiredEnemyCount && !hasSpawnedRewards)
        {
            DebugLog($"🎉 All {groupName} defeated! Spawning rewards...");
            OnAllEnemiesDefeated?.Invoke();
            StartCoroutine(SpawnRewardsWithDelay());
        }
    }
    
    private System.Collections.IEnumerator SpawnRewardsWithDelay()
    {
        hasSpawnedRewards = true;
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
        spawnedFragment = Instantiate(memoryFragmentPrefab, fragmentSpawnPosition, Quaternion.identity);
        spawnedFragment.name = $"{groupName} Memory Fragment";
        
        // Ensure it has the MemoryFragment component and configure it
        MemoryFragment fragmentComp = spawnedFragment.GetComponent<MemoryFragment>();
        if (fragmentComp == null)
        {
            fragmentComp = spawnedFragment.AddComponent<MemoryFragment>();
            DebugLog("Added MemoryFragment component to spawned object");
        }
        
        // 🔗 IMPORTANT: Link this fragment to this group handler
        fragmentComp.SetGroupHandler(this);
        
        // 🔧 CONFIGURE: Don't count toward global fragments (only for this group)
        fragmentComp.ConfigureAsGroupFragment(countsGlobally: fragmentCountsGlobally);
        
        // Set up collection detection
        StartCoroutine(MonitorFragmentCollection());
        
        DebugLog($"✅ Memory fragment spawned and configured successfully");
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
        spawnedPortal = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
        spawnedPortal.name = $"{groupName} Victory Portal";
        
        // Set up conditional portal behavior
        conditionalPortal = spawnedPortal.GetComponent<ConditionalPortal>();
        if (conditionalPortal == null)
        {
            conditionalPortal = spawnedPortal.AddComponent<ConditionalPortal>();
        }
        
        // Configure the portal
        conditionalPortal.requireMemoryFragment = requireFragmentCollection;
        conditionalPortal.targetSceneName = nextSceneName;
        conditionalPortal.groupHandler = this;
        
        DebugLog($"✅ Portal spawned successfully");
    }
    
    private System.Collections.IEnumerator MonitorFragmentCollection()
    {
        while (spawnedFragment != null && !memoryFragmentCollected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (memoryFragmentCollected)
        {
            DebugLog($"💎 Memory fragment collected! Portal is now ready.");
            OnMemoryFragmentCollected?.Invoke();
            
            if (conditionalPortal != null)
            {
                conditionalPortal.OnMemoryFragmentCollected();
                OnPortalReady?.Invoke();
            }
        }
    }
    
    // Called by MemoryFragment when collected
    public void NotifyFragmentCollected()
    {
        memoryFragmentCollected = true;
        DebugLog($"📢 Fragment collection notified to {groupName} handler");
    }
    
    // Getter for other scripts
    public bool IsMemoryFragmentCollected => memoryFragmentCollected;
    public bool AreAllEnemiesDefeated => defeatedCount >= requiredEnemyCount;
    public int DefeatedCount => defeatedCount;
    public int RequiredCount => requiredEnemyCount;
    public float CompletionPercentage => requiredEnemyCount > 0 ? (float)defeatedCount / requiredEnemyCount : 0f;
    
    private void ValidateSetup()
    {
        if (string.IsNullOrEmpty(enemyTag))
        {
            Debug.LogError($"❌ {gameObject.name}: Enemy tag not set!");
        }
        
        if (memoryFragmentPrefab == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Memory Fragment Prefab not assigned!");
        }
        
        if (portalPrefab == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Portal Prefab not assigned!");
        }
        
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Next scene name not set!");
        }
    }
    
    // Manual testing methods
    [ContextMenu("🧪 Test All Enemies Defeated")]
    public void TestAllEnemiesDefeated()
    {
        defeatedCount = requiredEnemyCount;
        HandleEnemyDeath();
    }
    
    [ContextMenu("💎 Test Fragment Collection")]
    public void TestFragmentCollection()
    {
        NotifyFragmentCollected();
    }
    
    [ContextMenu("📊 Show Current Progress")]
    public void ShowProgress()
    {
        DebugLog($"Progress: {defeatedCount}/{requiredEnemyCount} ({CompletionPercentage:P0})");
        DebugLog($"Rewards spawned: {hasSpawnedRewards}");
        DebugLog($"Fragment collected: {memoryFragmentCollected}");
    }
    
    [ContextMenu("🔄 Reset Handler")]
    public void ResetHandler()
    {
        ClearTracking();
        if (spawnedFragment != null) DestroyImmediate(spawnedFragment);
        if (spawnedPortal != null) DestroyImmediate(spawnedPortal);
        FindAndTrackEnemies();
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[EnemyGroupHandler - {groupName}] {message}");
        }
    }
    
    private void OnDestroy()
    {
        ClearTracking();
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
        UnityEditor.Handles.Label(fragmentSpawnPosition + Vector3.up * 1f, $"{groupName}\nFragment Spawn");
        UnityEditor.Handles.Label(portalSpawnPosition + Vector3.up * 1.5f, $"{groupName}\nPortal Spawn");
        #endif
    }
} 