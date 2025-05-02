using UnityEngine;

public class GhostEnemy : BaseEnemy
{
    [Header("Instance Override Settings")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Ghost-Specific Settings")]
    [SerializeField] private float hoverHeight = 1.2f;
    [SerializeField] private float hoverVariation = 0.2f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private GameObject ghostEffectPrefab;

    // Access properties that check for instance overrides
    public float HoverHeight => useInstanceValues ? hoverHeight : 
        (ghostData != null ? ghostData.hoverHeight : 1.2f);
    public float HoverVariation => useInstanceValues ? hoverVariation : 
        (ghostData != null ? ghostData.hoverVariation : 0.2f);
    public float HoverSpeed => useInstanceValues ? hoverSpeed : 
        (ghostData != null ? ghostData.hoverSpeed : 2f);

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