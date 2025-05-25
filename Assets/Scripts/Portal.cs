using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private string targetSceneName = "FinalScene";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireFragments = true;
    [SerializeField] private float transitionDelay = 1f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject portalActiveEffect;
    [SerializeField] private GameObject portalInactiveEffect;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private float pulsateSpeed = 2f;
    [SerializeField] private float pulsateAmplitude = 0.3f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private AudioClip blockedSound;
    
    [Header("UI Messages")]
    [SerializeField] private bool showUIMessages = true;
    [SerializeField] private string insufficientFragmentsMessage = "Need more memory fragments to use portal";
    [SerializeField] private string portalReadyMessage = "Press E to enter portal";
    
    // Internal state
    private bool isActive = false;
    private bool playerInRange = false;
    private bool isTransitioning = false;
    private Renderer portalRenderer;
    private Collider2D portalCollider;
    private Color originalColor;
    private Vector3 originalScale;
    
    private void Start()
    {
        // Get components
        portalRenderer = GetComponent<Renderer>();
        portalCollider = GetComponent<Collider2D>();
        
        if (portalRenderer != null)
        {
            originalColor = portalRenderer.material.color;
            originalScale = transform.localScale;
        }
        
        // Ensure we have a trigger collider
        if (portalCollider != null && !portalCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: Portal collider should be set to 'Is Trigger'!");
        }
        
        // Subscribe to fragment collection events
        if (requireFragments)
        {
            FragmentCollector.OnPortalUnlocked += ActivatePortal;
            CheckPortalStatus();
        }
        else
        {
            ActivatePortal();
        }
        
        Debug.Log($"🌀 {gameObject.name}: Portal initialized (Target: {targetSceneName})");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (requireFragments)
        {
            FragmentCollector.OnPortalUnlocked -= ActivatePortal;
        }
    }
    
    private void Update()
    {
        UpdateVisualEffects();
        HandleInput();
    }
    
    private void UpdateVisualEffects()
    {
        if (portalRenderer == null) return;
        
        if (isActive)
        {
            // Pulsating active effect
            float pulsate = 1f + Mathf.Sin(Time.time * pulsateSpeed) * pulsateAmplitude;
            transform.localScale = originalScale * pulsate;
            portalRenderer.material.color = Color.Lerp(originalColor, activeColor, 0.8f);
        }
        else
        {
            // Static inactive appearance
            transform.localScale = originalScale;
            portalRenderer.material.color = Color.Lerp(originalColor, inactiveColor, 0.5f);
        }
    }
    
    private void HandleInput()
    {
        if (playerInRange && !isTransitioning)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                AttemptPortalEntry();
            }
        }
    }
    
    private void CheckPortalStatus()
    {
        if (FragmentCollector.Instance != null && FragmentCollector.Instance.CanUsePortal)
        {
            ActivatePortal();
        }
        else
        {
            DeactivatePortal();
        }
    }
    
    private void ActivatePortal()
    {
        if (isActive) return;
        
        isActive = true;
        
        Debug.Log($"🌟 {gameObject.name}: Portal activated! Player can now travel to {targetSceneName}");
        
        // Play activation sound
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Show active effect
        if (portalActiveEffect != null)
        {
            portalActiveEffect.SetActive(true);
        }
        
        if (portalInactiveEffect != null)
        {
            portalInactiveEffect.SetActive(false);
        }
    }
    
    private void DeactivatePortal()
    {
        isActive = false;
        
        Debug.Log($"🔒 {gameObject.name}: Portal deactivated");
        
        // Show inactive effect
        if (portalActiveEffect != null)
        {
            portalActiveEffect.SetActive(false);
        }
        
        if (portalInactiveEffect != null)
        {
            portalInactiveEffect.SetActive(true);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            Debug.Log($"🚶 {gameObject.name}: Player entered portal range");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            Debug.Log($"🚶 {gameObject.name}: Player left portal range");
        }
    }
    
    private void AttemptPortalEntry()
    {
        if (isTransitioning) return;
        
        if (!isActive)
        {
            Debug.Log($"🚫 {gameObject.name}: Portal is not active - insufficient fragments");
            
            // Play blocked sound
            if (audioSource != null && blockedSound != null)
            {
                audioSource.PlayOneShot(blockedSound);
            }
            
            return;
        }
        
        Debug.Log($"🌀 {gameObject.name}: Player entering portal - transitioning to {targetSceneName}");
        StartCoroutine(TransitionToScene());
    }
    
    private IEnumerator TransitionToScene()
    {
        isTransitioning = true;
        
        // Play teleport sound
        if (audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Optional: Add screen fade or transition effect here
        Debug.Log($"⏳ {gameObject.name}: Transitioning in {transitionDelay} seconds...");
        
        yield return new WaitForSeconds(transitionDelay);
        
        // Load the target scene
        Debug.Log($"🎬 {gameObject.name}: Loading scene: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
    
    // Manual activation for testing
    [ContextMenu("🔓 Activate Portal")]
    public void ForceActivatePortal()
    {
        ActivatePortal();
    }
    
    [ContextMenu("🔒 Deactivate Portal")]
    public void ForceDeactivatePortal()
    {
        DeactivatePortal();
    }
    
    [ContextMenu("🌀 Test Transition")]
    public void TestTransition()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene());
        }
    }
    
    // UI for showing portal status
    private void OnGUI()
    {
        if (playerInRange && showUIMessages)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            if (isActive)
            {
                GUI.Label(new Rect(screenWidth/2 - 100, screenHeight - 100, 200, 30), portalReadyMessage);
            }
            else
            {
                int currentFragments = FragmentCollector.Instance != null ? FragmentCollector.Instance.TotalFragments : 0;
                int requiredFragments = FragmentCollector.Instance != null ? FragmentCollector.Instance.RequiredFragments : 5;
                
                GUI.Label(new Rect(screenWidth/2 - 150, screenHeight - 100, 300, 30), 
                    $"{insufficientFragmentsMessage} ({currentFragments}/{requiredFragments})");
            }
        }
    }
} 