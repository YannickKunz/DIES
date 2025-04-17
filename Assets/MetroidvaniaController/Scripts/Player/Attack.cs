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
        Collider2D[] collidersEnemies = Physics2D.OverlapCircleAll(attackCheck.position, 0.9f);
        
        for (int i = 0; i < collidersEnemies.Length; i++)
        {
            if (collidersEnemies[i].gameObject.CompareTag("Enemy"))
            {
                // Adjust damage direction 
                if (collidersEnemies[i].transform.position.x - transform.position.x < 0)
                {
                    dmgValue = -dmgValue;
                }

                // Create DamageInfo object compatible with our EnemyHealth script
                Vector3 hitPosition = transform.position;
                DamageInfo damageInfo = new DamageInfo(Mathf.Abs(dmgValue), hitPosition);
                
                // Try to get EnemyHealth component directly
                EnemyHealth health = collidersEnemies[i].GetComponent<EnemyHealth>();
                if (health != null)
                {
                    Debug.Log($"Direct hit on {collidersEnemies[i].name} with damage {dmgValue}");
                    health.TakeDamage(dmgValue, hitPosition);
                }
                else
                {
                    // Fall back to SendMessage with different options
                    Debug.Log($"Sending damage message to {collidersEnemies[i].name}");
                    collidersEnemies[i].SendMessage("ApplyDamage", damageInfo, SendMessageOptions.DontRequireReceiver);
                }
                
                // Only shake camera if it exists and has the component
                if (cam != null)
                {
                    var cameraFollow = cam.GetComponent<CameraFollow>();
                    if (cameraFollow != null)
                        cameraFollow.ShakeCamera();
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize attack range
        if (attackCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCheck.position, 0.9f);
        }
    }
}