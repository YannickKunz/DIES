using UnityEngine;

public class GhostEnemy : BaseEnemy
{
    [Header("Instance Override Settings")]
    [Tooltip("When enabled, uses the values set in this component rather than from the GhostData asset")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Ghost-Specific Settings")]
    [Tooltip("How high above the ground the ghost hovers. The ghost is considered 'grounded' when it's within 0.05 units of this height.")]
    [SerializeField] private float hoverHeight = 1.2f;
    [Tooltip("Random variation added to hover height to make movement look more natural")]
    [SerializeField] private float hoverVariation = 0.2f;
    [Tooltip("How quickly the ghost adjusts to new hover heights when moving over different terrain")]
    [SerializeField] private float hoverSpeed = 2f;
    [Tooltip("Special effect prefab spawned when the ghost vanishes/dies")]
    [SerializeField] private GameObject ghostEffectPrefab;

    // Access properties that check for instance overrides
    public float HoverHeight => useInstanceValues ? hoverHeight : 1.2f;
    public float HoverVariation => useInstanceValues ? hoverVariation : 0.2f;
    public float HoverSpeed => useInstanceValues ? hoverSpeed : 2f;

    // Reference to specialized components
    [HideInInspector] public GhostAI ghostAI;
    [HideInInspector] public GhostMovement ghostMovement;
    
    // Reference to Ghost data
    private GhostData ghostData;

    protected override void Awake()
    {
        base.Awake();
        
        // Get Ghost-specific components
        ghostAI = GetComponent<GhostAI>();
        ghostMovement = GetComponent<GhostMovement>();
        
        // Cast enemy data to ghost data if possible
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
        }
        
        Debug.Log(gameObject.name + ": Ghost components found - " +
                "GhostAI: " + (ghostAI != null) +
                ", GhostMovement: " + (ghostMovement != null));
    }

    // Special death effect for ghost
    public void PlayVanishEffect()
    {
        if (ghostEffectPrefab != null)
        {
            Instantiate(ghostEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}