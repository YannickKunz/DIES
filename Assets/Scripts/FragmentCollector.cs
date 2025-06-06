using UnityEngine;
using System;

public class FragmentCollector : MonoBehaviour
{
    [Header("Fragment Collection")]
    [SerializeField] private int totalFragmentsCollected = 0;
    [SerializeField] private int fragmentsRequiredForPortal = 3;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Events for other systems to listen to
    public static event Action<int> OnFragmentCollected; // Passes current total count
    public static event Action OnPortalUnlocked; // When player reaches required fragment count
    
    // Static instance for easy access from anywhere
    public static FragmentCollector Instance { get; private set; }
    
    // Properties
    public int TotalFragments => totalFragmentsCollected;
    public int RequiredFragments => fragmentsRequiredForPortal;
    public bool CanUsePortal => totalFragmentsCollected >= fragmentsRequiredForPortal;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            DebugLog("FragmentCollector initialized as singleton");
        }
        else
        {
            Debug.LogWarning("Multiple FragmentCollectors found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    public void CollectFragment()
    {
        totalFragmentsCollected++;
        DebugLog($"Fragment collected! Total: {totalFragmentsCollected}/{fragmentsRequiredForPortal}");
        
        // Notify other systems
        OnFragmentCollected?.Invoke(totalFragmentsCollected);
        
        // Check if portal should be unlocked
        if (totalFragmentsCollected >= fragmentsRequiredForPortal && totalFragmentsCollected - 1 < fragmentsRequiredForPortal)
        {
            DebugLog("🌟 Portal unlocked! Player can now use portal to final scene!");
            OnPortalUnlocked?.Invoke();
        }
    }
    
    public void AddFragments(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            CollectFragment();
        }
    }
    
    // Reset fragments (useful for testing)
    [ContextMenu("🔄 Reset Fragments")]
    public void ResetFragments()
    {
        totalFragmentsCollected = 0;
        DebugLog("Fragments reset to 0");
    }
    
    // Add fragments for testing
    [ContextMenu("🧩 Add Test Fragment")]
    public void AddTestFragment()
    {
        CollectFragment();
    }
    
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FragmentCollector] {message}");
        }
    }
    
    // Display fragment count in inspector
    private void OnGUI()
    {
        if (showDebugLogs)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Memory Fragments: {totalFragmentsCollected}/{fragmentsRequiredForPortal}");
            
            if (CanUsePortal)
            {
                GUI.Label(new Rect(10, 30, 300, 20), "✅ Portal Available!");
            }
        }
    }
} 