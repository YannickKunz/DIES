using UnityEngine;
using System.Collections;

public class MemoryFragment : MonoBehaviour
{
    [Header("Fragment Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private float collectionDelay = 0.1f;
    
    [Header("Fragment Type")]
    [Tooltip("If assigned, this fragment will notify the specific group handler when collected")]
    [SerializeField] private EnemyGroupDeathHandler specificGroupHandler;
    [Tooltip("If true, this fragment counts toward the global fragment count")]
    [SerializeField] private bool countsTowardGlobalFragments = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float rotationSpeed = 50f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectSound;
    
    private Vector3 startPosition;
    private bool isCollected = false;
    private Renderer fragmentRenderer;
    private Collider2D fragmentCollider;
    
    private void Start()
    {
        startPosition = transform.position;
        fragmentRenderer = GetComponent<Renderer>();
        fragmentCollider = GetComponent<Collider2D>();
        
        // Ensure we have a trigger collider
        if (fragmentCollider != null && !fragmentCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: MemoryFragment collider should be set to 'Is Trigger'!");
        }
        
        Debug.Log($"💎 {gameObject.name}: Memory Fragment ready for collection");
    }
    
    private void Update()
    {
        if (!isCollected)
        {
            // Floating bob animation
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPosition + Vector3.up * bobOffset;
            
            // Rotation animation
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"💎 {gameObject.name}: Player touched memory fragment - collecting!");
            StartCoroutine(CollectFragment());
        }
    }
    
    private IEnumerator CollectFragment()
    {
        if (isCollected) yield break;
        
        isCollected = true;
        
        Debug.Log($"🌟 {gameObject.name}: Memory Fragment collected!");
        
        // Disable collider to prevent multiple collections
        if (fragmentCollider != null)
            fragmentCollider.enabled = false;
        
        // Play collect sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Spawn collect effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Notify the global fragment collector system (for overall progress)
        if (countsTowardGlobalFragments && FragmentCollector.Instance != null)
        {
            FragmentCollector.Instance.CollectFragment();
            Debug.Log($"🌍 Global fragment collected! Total: {FragmentCollector.Instance.TotalFragments}");
        }
        else if (specificGroupHandler != null)
        {
            Debug.Log($"📍 Group-specific fragment - not counting toward global total");
        }
        
        // Notify specific group handler (for conditional portals)
        if (specificGroupHandler != null)
        {
            specificGroupHandler.NotifyFragmentCollected();
            Debug.Log($"🎯 Notified specific group handler: {specificGroupHandler.name}");
        }
        else
        {
            Debug.Log($"ℹ️ No specific group handler assigned - this is a regular memory fragment");
        }
        
        // Wait a bit for effects/sound
        yield return new WaitForSeconds(collectionDelay);
        
        // Hide or destroy the fragment
        if (destroyOnCollect)
        {
            Debug.Log($"🗑️ {gameObject.name}: Destroying collected fragment");
            Destroy(gameObject);
        }
        else
        {
            // Just hide it
            if (fragmentRenderer != null)
                fragmentRenderer.enabled = false;
            if (fragmentCollider != null)
                fragmentCollider.enabled = false;
        }
    }
    
    // Method to set the specific group handler (called by EnemyGroupDeathHandler when spawning)
    public void SetGroupHandler(EnemyGroupDeathHandler handler)
    {
        specificGroupHandler = handler;
        Debug.Log($"💎 {gameObject.name}: Assigned to group handler {handler.name}");
    }
    
    // Method to configure fragment behavior (called by EnemyGroupDeathHandler)
    public void ConfigureAsGroupFragment(bool countsGlobally = false)
    {
        countsTowardGlobalFragments = countsGlobally;
        Debug.Log($"⚙️ {gameObject.name}: Configured as group fragment (Global: {countsGlobally})");
    }
    
    // Manual collection method for testing
    [ContextMenu("🧪 Test Collection")]
    public void TestCollection()
    {
        if (!isCollected)
        {
            StartCoroutine(CollectFragment());
        }
    }
} 