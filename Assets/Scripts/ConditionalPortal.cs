using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ConditionalPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("Scene name to load when portal is activated")]
    public string targetSceneName = "NextLevel";
    [Tooltip("If true, portal only works after collecting memory fragment")]
    public bool requireMemoryFragment = true;
    [Tooltip("Can the portal be used right now?")]
    [SerializeField] private bool isPortalReady = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool changeVisualWhenReady = true;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private GameObject portalEffect;
    [SerializeField] private Animator portalAnimator;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip transportSound;
    
    [Header("Player Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private float interactionRange = 2f;
    
    [Header("UI Messages")]
    [SerializeField] private bool showUIMessages = true;
    [SerializeField] private string messageWhenLocked = "Collect the memory fragment first!";
    [SerializeField] private string messageWhenReady = "Press E to enter portal";
    [SerializeField] private float messageDuration = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Internal references
    [HideInInspector] public EnemyGroupDeathHandler groupHandler;
    private Transform player;
    private bool playerNearby = false;
    private Renderer portalRenderer;
    private Light portalLight;
    private ParticleSystem portalParticles;
    
    // Events
    public System.Action OnPortalActivated;
    public System.Action OnPortalUsed;
    public System.Action OnPortalDenied;
    
    private void Start()
    {
        // Get components for visual feedback
        portalRenderer = GetComponent<Renderer>();
        portalLight = GetComponent<Light>();
        portalParticles = GetComponent<ParticleSystem>();
        
        // If we don't require memory fragment, portal is ready immediately
        if (!requireMemoryFragment)
        {
            SetPortalReady(true);
        }
        else
        {
            SetPortalReady(false);
        }
        
        // Find player
        FindPlayer();
        
        DebugLog($"Conditional portal initialized. Target: {targetSceneName}, Requires fragment: {requireMemoryFragment}");
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        // Check if player is nearby
        float distance = Vector3.Distance(transform.position, player.position);
        bool wasNearby = playerNearby;
        playerNearby = distance <= interactionRange;
        
        // Handle UI messages when player approaches/leaves
        if (playerNearby && !wasNearby)
        {
            OnPlayerEnterRange();
        }
        else if (!playerNearby && wasNearby)
        {
            OnPlayerExitRange();
        }
        
        // Handle interaction input
        if (playerNearby && Input.GetKeyDown(interactionKey))
        {
            AttemptPortalUse();
        }
    }
    
    private void OnPlayerEnterRange()
    {
        if (showUIMessages)
        {
            string message = isPortalReady ? messageWhenReady : messageWhenLocked;
            ShowMessage(message);
        }
        
        DebugLog($"Player entered portal range. Portal ready: {isPortalReady}");
    }
    
    private void OnPlayerExitRange()
    {
        // Could hide UI messages here if needed
        DebugLog("Player left portal range");
    }
    
    private void AttemptPortalUse()
    {
        if (isPortalReady)
        {
            DebugLog($"✅ Portal activated! Loading scene: {targetSceneName}");
            UsePortal();
        }
        else
        {
            DebugLog($"❌ Portal denied - memory fragment not collected yet");
            DenyPortalUse();
        }
    }
    
    private void UsePortal()
    {
        // Play transport sound
        PlaySound(transportSound);
        
        // Trigger event
        OnPortalUsed?.Invoke();
        
        // Show effect if available
        if (portalEffect != null)
        {
            Instantiate(portalEffect, transform.position, Quaternion.identity);
        }
        
        // Animate portal if animator available
        if (portalAnimator != null)
        {
            portalAnimator.SetTrigger("Transport");
        }
        
        // Load the target scene
        StartCoroutine(LoadSceneWithDelay());
    }
    
    private void DenyPortalUse()
    {
        // Play denied sound
        PlaySound(deniedSound);
        
        // Trigger event
        OnPortalDenied?.Invoke();
        
        // Show message
        if (showUIMessages)
        {
            ShowMessage(messageWhenLocked);
        }
        
        // Could add visual feedback here (shake, red flash, etc.)
        if (portalAnimator != null)
        {
            portalAnimator.SetTrigger("Denied");
        }
    }
    
    private System.Collections.IEnumerator LoadSceneWithDelay()
    {
        // Give time for effects to play
        yield return new WaitForSeconds(0.5f);
        
        // Validate scene name
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"❌ Target scene name is empty! Cannot load scene.");
            yield break;
        }
        
        // Load the scene
        try
        {
            SceneManager.LoadScene(targetSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to load scene '{targetSceneName}': {e.Message}");
        }
    }
    
    public void OnMemoryFragmentCollected()
    {
        if (requireMemoryFragment && !isPortalReady)
        {
            DebugLog("💎 Memory fragment collected! Portal is now ready.");
            SetPortalReady(true);
        }
    }
    
    private void SetPortalReady(bool ready)
    {
        isPortalReady = ready;
        
        // Visual feedback
        if (changeVisualWhenReady)
        {
            UpdatePortalVisuals();
        }
        
        // Play activation sound
        if (ready && activationSound != null)
        {
            PlaySound(activationSound);
        }
        
        // Trigger event
        if (ready)
        {
            OnPortalActivated?.Invoke();
        }
        
        DebugLog($"Portal ready state changed to: {ready}");
    }
    
    private void UpdatePortalVisuals()
    {
        Color targetColor = isPortalReady ? activeColor : inactiveColor;
        
        // Update renderer color
        if (portalRenderer != null)
        {
            if (portalRenderer.material != null)
            {
                portalRenderer.material.color = targetColor;
            }
        }
        
        // Update light
        if (portalLight != null)
        {
            portalLight.color = targetColor;
            portalLight.enabled = isPortalReady;
        }
        
        // Update particle system
        if (portalParticles != null)
        {
            var main = portalParticles.main;
            main.startColor = targetColor;
            
            if (isPortalReady && !portalParticles.isPlaying)
            {
                portalParticles.Play();
            }
            else if (!isPortalReady && portalParticles.isPlaying)
            {
                portalParticles.Stop();
            }
        }
        
        // Update animator
        if (portalAnimator != null)
        {
            portalAnimator.SetBool("IsReady", isPortalReady);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void ShowMessage(string message)
    {
        // This is a simple debug message - you might want to integrate with your UI system
        Debug.Log($"🗨️ Portal Message: {message}");
        
        // TODO: Integrate with your UI system to show messages to player
        // For example: UIManager.Instance.ShowMessage(message, messageDuration);
    }
    
    // Manual testing methods
    [ContextMenu("🧪 Test Portal Activation")]
    public void TestPortalActivation()
    {
        SetPortalReady(true);
    }
    
    [ContextMenu("🔒 Test Portal Lock")]
    public void TestPortalLock()
    {
        SetPortalReady(false);
    }
    
    [ContextMenu("🌀 Test Portal Use")]
    public void TestPortalUse()
    {
        UsePortal();
    }
    
    [ContextMenu("❌ Test Portal Denial")]
    public void TestPortalDenial()
    {
        DenyPortalUse();
    }
    
    // Getters for other scripts
    public bool IsPortalReady => isPortalReady;
    public string TargetScene => targetSceneName;
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ConditionalPortal] {message}");
        }
    }
    
    // Visualize interaction range in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isPortalReady ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Portal: {(isPortalReady ? "READY" : "LOCKED")}\nTarget: {targetSceneName}");
        #endif
    }
} 