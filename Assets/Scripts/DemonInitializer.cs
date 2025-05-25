using UnityEngine;

public class DemonInitializer : MonoBehaviour
{
    [Header("Demon Setup")]
    [Tooltip("DemonData asset to use. OPTIONAL - demon will work without this using inspector values.")]
    [SerializeField] private DemonData demonData; // Assign your BasicDemonData asset here
    
    [Header("Initialization Options")]
    [Tooltip("If enabled, initializes demon automatically when the game starts")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [Tooltip("If enabled, demon will work even without DemonData (uses inspector values)")]
    [SerializeField] private bool allowFallbackMode = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeDemon();
        }
    }
    
    [ContextMenu("Initialize Demon")]
    public void InitializeDemon()
    {
        // Get the DemonAI component
        DemonAI demonAI = GetComponent<DemonAI>();
        if (demonAI == null)
        {
            Debug.LogError($"{gameObject.name}: No DemonAI component found!");
            return;
        }
        
        // Also get DemonAnimator and EnemyHealth to initialize them directly
        DemonAnimator demonAnimator = GetComponent<DemonAnimator>();
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        
        if (demonData != null)
        {
            // Initialize with DemonData asset
            if (showDebugLogs)
            {
                Debug.Log($"{gameObject.name}: Initializing Demon with DemonData asset '{demonData.name}'");
            }
            demonAI.Initialize(demonData);
            
            // Initialize animator separately to ensure it gets set up
            if (demonAnimator != null)
            {
                demonAnimator.Initialize(demonData);
            }
            
            // 🏥 IMPORTANT: Initialize EnemyHealth with data!
            if (enemyHealth != null)
            {
                Debug.Log($"🏥 {gameObject.name}: Initializing EnemyHealth with DemonData");
                enemyHealth.Initialize(demonData);
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name}: No EnemyHealth component found!");
            }
        }
        else if (allowFallbackMode)
        {
            // Initialize with null data - demon will use inspector values
            if (showDebugLogs)
            {
                Debug.Log($"{gameObject.name}: Initializing Demon in fallback mode (using inspector values)");
            }
            demonAI.Initialize(null);
            
            // Initialize animator separately to ensure it gets set up
            if (demonAnimator != null)
            {
                demonAnimator.Initialize(null);
            }
            
            // 🏥 Initialize EnemyHealth even with null data (it will use fallback values)
            if (enemyHealth != null)
            {
                Debug.Log($"🏥 {gameObject.name}: Initializing EnemyHealth with fallback values (null data)");
                // In fallback mode, we pass null to EnemyHealth and it will use default health values
                enemyHealth.Initialize(null);
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name}: No EnemyHealth component found!");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: No DemonData assigned and fallback mode disabled!");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name}: Demon initialization completed!");
        }
    }
    
    private void OnValidate()
    {
        // This runs in the editor when values change
        if (demonData == null && !allowFallbackMode)
        {
            Debug.LogWarning($"{gameObject.name}: DemonData not assigned and fallback mode disabled!");
        }
    }
    
    // Helpful info displayed in inspector
    [Space(10)]
    [Header("📖 How This Works")]
    [TextArea(4, 8)]
    [SerializeField] private string helpText = 
        "• WITH DemonData: Demon uses values from ScriptableObject asset\n" +
        "• WITHOUT DemonData: Demon uses values from inspector (Per-Demon Configuration)\n" +
        "• You can change inspector values anytime while testing!\n" +
        "• Multiple demons can share one DemonData asset or have individual settings\n" +
        "• Demons have special attacks that trigger randomly when in range";
} 