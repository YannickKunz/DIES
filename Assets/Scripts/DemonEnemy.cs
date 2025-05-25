using UnityEngine;

public class DemonEnemy : BaseEnemy
{
    [Header("Instance Override Settings")]
    [Tooltip("When enabled, uses the values set in this component rather than from the DemonData asset")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Demon-Specific Settings")]
    [Tooltip("How high above the ground the demon hovers. The demon is considered 'grounded' when it's within 0.05 units of this height.")]
    [SerializeField] private float hoverHeight = 1.2f;
    [Tooltip("Random variation added to hover height to make movement look more natural")]
    [SerializeField] private float hoverVariation = 0.2f;
    [Tooltip("How quickly the demon adjusts to new hover heights when moving over different terrain")]
    [SerializeField] private float hoverSpeed = 2f;
    [Tooltip("Special effect prefab spawned when the demon vanishes/dies")]
    [SerializeField] private GameObject demonEffectPrefab;

    // Access properties that check for instance overrides
    public float HoverHeight => useInstanceValues ? hoverHeight : 1.2f;
    public float HoverVariation => useInstanceValues ? hoverVariation : 0.2f;
    public float HoverSpeed => useInstanceValues ? hoverSpeed : 2f;

    // Reference to specialized components
    [HideInInspector] public DemonAI demonAI;
    [HideInInspector] public DemonMovement demonMovement;
    
    // Reference to Demon data
    private DemonData demonData;

    protected override void Awake()
    {
        base.Awake();
        
        // Get Demon-specific components
        demonAI = GetComponent<DemonAI>();
        demonMovement = GetComponent<DemonMovement>();
        
        // Cast enemy data to demon data if possible
        if (enemyData is DemonData)
        {
            demonData = (DemonData)enemyData;
        }
        
        Debug.Log(gameObject.name + ": Demon components found - " +
                "DemonAI: " + (demonAI != null) +
                ", DemonMovement: " + (demonMovement != null));
    }

    // Special death effect for demon
    public void PlayVanishEffect()
    {
        if (demonEffectPrefab != null)
        {
            Instantiate(demonEffectPrefab, transform.position, Quaternion.identity);
        }
    }
} 