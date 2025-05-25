using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public float dmgValue = 4;
    public GameObject throwableObject;
    public Transform attackCheck;
    private Rigidbody2D m_Rigidbody2D;
    public Animator animator;
    public bool canAttack = true;
    public bool isTimeToCheck = false;

    [Tooltip("Assign the Main Camera or camera controller here")]
    public GameObject cam; // This needs to be assigned in Inspector

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        
        // Auto-find camera if not assigned
        if (cam == null) 
        {
            cam = Camera.main?.gameObject;
            Debug.Log("Attack script: Auto-assigned main camera");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            animator.SetBool("IsAttacking", true);
            StartCoroutine(AttackCooldown());
        }

        if (Input.GetKeyDown(KeyCode.E) && throwableObject != null)
        {
            GameObject throwableWeapon = Instantiate(throwableObject, transform.position + new Vector3(transform.localScale.x * 0.5f,-0.2f), Quaternion.identity);
            Vector2 direction = new Vector2(transform.localScale.x, 0);
            throwableWeapon.GetComponent<ThrowableWeapon>().direction = direction;
            throwableWeapon.name = "ThrowableWeapon";
        }
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(0.25f);
        canAttack = true;
    }

    // This is called via animation events
    public void DoDashDamage()
    {
        dmgValue = Mathf.Abs(dmgValue);
        // 🔧 TEMPORARILY INCREASED ATTACK RANGE FOR TESTING
        float testAttackRadius = 1.5f; // Increased from 0.9f
        Collider2D[] collidersEnemies = Physics2D.OverlapCircleAll(attackCheck.position, testAttackRadius);
        
        // 🔍 DEBUG: Log attack attempt
        Debug.Log($"=== ATTACK DEBUG START ===");
        Debug.Log($"Attack position: {attackCheck.position}");
        Debug.Log($"Attack radius: {testAttackRadius}f (INCREASED FOR TESTING)");
        Debug.Log($"Total colliders detected: {collidersEnemies.Length}");
        
        for (int i = 0; i < collidersEnemies.Length; i++)
        {
            // 🔍 DEBUG: Log each detected collider
            Debug.Log($"Collider {i}: {collidersEnemies[i].name} - Tag: '{collidersEnemies[i].tag}' - Layer: {LayerMask.LayerToName(collidersEnemies[i].gameObject.layer)}");
            
            // Check for both Enemy and Demon tags
            if (collidersEnemies[i].gameObject.CompareTag("Enemy") || collidersEnemies[i].gameObject.CompareTag("Demon") || collidersEnemies[i].gameObject.CompareTag("Ghost"))
            {
                Debug.Log($"✅ {collidersEnemies[i].name} has valid tag for damage!");
                
                // Adjust damage direction 
                if (collidersEnemies[i].transform.position.x - transform.position.x < 0)
                {
                    dmgValue = -dmgValue;
                }

                // Create DamageInfo object compatible with our EnemyHealth script
                Vector3 hitPosition = transform.position;
                DamageInfo damageInfo = new DamageInfo(Mathf.Abs(dmgValue), hitPosition);
                
                Debug.Log($"🎯 Attempting to damage {collidersEnemies[i].name} with {Mathf.Abs(dmgValue)} damage");
                
                // Try to get EnemyHealth component directly
                EnemyHealth health = collidersEnemies[i].GetComponent<EnemyHealth>();
                if (health != null)
                {
                    Debug.Log($"✅ Found EnemyHealth component on {collidersEnemies[i].name} - Calling TakeDamage directly");
                    health.TakeDamage(Mathf.Abs(dmgValue), hitPosition);
                    Debug.Log($"✅ TakeDamage called successfully on {collidersEnemies[i].name}");
                }
                else
                {
                    // Check what components the object actually has
                    Component[] allComponents = collidersEnemies[i].GetComponents<Component>();
                    Debug.Log($"❌ No EnemyHealth found on {collidersEnemies[i].name}. Components found:");
                    foreach (Component comp in allComponents)
                    {
                        Debug.Log($"    - {comp.GetType().Name}");
                    }
                    
                    // Fall back to SendMessage with different options
                    Debug.Log($"🔄 Trying SendMessage to {collidersEnemies[i].name}");
                    collidersEnemies[i].SendMessage("ApplyDamage", damageInfo, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"📤 SendMessage sent to {collidersEnemies[i].name}");
                }
                
                // Only shake camera if it exists and has the component
                if (cam != null)
                {
                    var cameraFollow = cam.GetComponent<CameraFollow>();
                    if (cameraFollow != null)
                        cameraFollow.ShakeCamera();
                }
            }
            else
            {
                Debug.Log($"❌ {collidersEnemies[i].name} has invalid tag '{collidersEnemies[i].tag}' - skipping damage");
            }
        }
        
        Debug.Log($"=== ATTACK DEBUG END ===");
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize attack range - UPDATED FOR TESTING
        if (attackCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCheck.position, 1.5f); // Updated to match test radius
        }
    }
}