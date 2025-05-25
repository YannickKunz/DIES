using UnityEngine;

public class GhostInitializer : MonoBehaviour
{
    [Header("Ghost Setup")]
    [Tooltip("GhostData asset to use. OPTIONAL - ghost will work without this using inspector values.")]
    [SerializeField] private GhostData ghostData; // Assign your BasicGhostData asset here
    
    [Header("Initialization Options")]
    [Tooltip("If enabled, initializes ghost automatically when the game starts")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [Tooltip("If enabled, ghost will work even without GhostData (uses inspector values)")]
    [SerializeField] private bool allowFallbackMode = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeGhost();
        }
    }
    
    [ContextMenu("Initialize Ghost")]
    public void InitializeGhost()
    {
        // Get the GhostAI component
        GhostAI ghostAI = GetComponent<GhostAI>();
        if (ghostAI == null)
        {
            Debug.LogError($"{gameObject.name}: No GhostAI component found!");
            return;
        }
        
        // Also get GhostAnimator and EnemyHealth to initialize them directly
        GhostAnimator ghostAnimator = GetComponent<GhostAnimator>();
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        
        if (ghostData != null)
        {
            // Initialize with GhostData asset
            if (showDebugLogs)
            {
                Debug.Log($"{gameObject.name}: Initializing Ghost with GhostData asset '{ghostData.name}'");
            }
            ghostAI.Initialize(ghostData);
            
            // Initialize animator separately to ensure it gets set up
            if (ghostAnimator != null)
            {
                ghostAnimator.Initialize(ghostData);
            }
            
            // 🏥 IMPORTANT: Initialize EnemyHealth with data!
            if (enemyHealth != null)
            {
                Debug.Log($"🏥 {gameObject.name}: Initializing EnemyHealth with GhostData");
                enemyHealth.Initialize(ghostData);
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name}: No EnemyHealth component found!");
            }
        }
        else if (allowFallbackMode)
        {
            // Initialize with null data - ghost will use inspector values
            if (showDebugLogs)
            {
                Debug.Log($"{gameObject.name}: Initializing Ghost in fallback mode (using inspector values)");
            }
            ghostAI.Initialize(null);
            
            // Initialize animator separately to ensure it gets set up
            if (ghostAnimator != null)
            {
                ghostAnimator.Initialize(null);
            }
            
            // 🏥 IMPORTANT: Initialize EnemyHealth even with null data (uses fallback)
            if (enemyHealth != null)
            {
                Debug.Log($"🏥 {gameObject.name}: Initializing EnemyHealth with fallback values");
                enemyHealth.Initialize(null);
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name}: No EnemyHealth component found!");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: No GhostData assigned and fallback mode disabled!");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name}: Ghost initialization completed!");
        }
    }
    
    private void OnValidate()
    {
        // This runs in the editor when values change
        if (ghostData == null && !allowFallbackMode)
        {
            Debug.LogWarning($"{gameObject.name}: GhostData not assigned and fallback mode disabled!");
        }
    }
    
    // Helpful info displayed in inspector
    [Space(10)]
    [Header("📖 How This Works")]
    [TextArea(4, 8)]
    [SerializeField] private string helpText = 
        "• WITH GhostData: Ghost uses values from ScriptableObject asset\n" +
        "• WITHOUT GhostData: Ghost uses values from inspector (Per-Ghost Configuration)\n" +
        "• You can change inspector values anytime while testing!\n" +
        "• Multiple ghosts can share one GhostData asset or have individual settings";
} 