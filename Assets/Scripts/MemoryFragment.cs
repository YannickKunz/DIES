using UnityEngine;
using System.Collections;

public class MemoryFragment : MonoBehaviour
{
    [Header("Fragment Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private float collectionDelay = 0.1f;
    
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
        
        // Notify the fragment collector system
        if (FragmentCollector.Instance != null)
        {
            FragmentCollector.Instance.CollectFragment();
        }
        else
        {
            Debug.LogError("💀 FragmentCollector.Instance is null! Make sure player has FragmentCollector component.");
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