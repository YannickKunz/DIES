using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private float m_JumpForce = 400f;							// Amount of force added when the player jumps.
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// How much to smooth out the movement
    [SerializeField] private bool m_AirControl = false;							// Whether or not a player can steer while jumping;
    [SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_WallCheck;								// Position that checks if the character touches a wall
    [SerializeField] private float m_DashForce = 25f;                           // Force applied during dash
    [SerializeField] private float m_WallSlidingSpeed = 5f;                     // Speed of wall sliding
    [SerializeField] private float m_WallJumpHorizontalMultiplier = 1.2f;       // Horizontal force multiplier for wall jump
    [SerializeField] private float m_WallJumpVerticalMultiplier = 1f;           // Vertical force multiplier for wall jump

    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private bool m_Grounded;            // Whether or not the player is grounded.
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private Vector3 velocity = Vector3.zero;
    private float limitFallSpeed = 25f; // Limit fall speed

    public bool canDoubleJump = true; //If player can double jump
    private bool canDash = true;
    private bool isDashing = false; //If player is dashing
    private bool m_IsWall = false; //If there is a wall in front of the player
    private bool isWallSliding = false; //If player is sliding in a wall
    private bool oldWallSlidding = false; //If player is sliding in a wall in the previous frame
    private float prevVelocityX = 0f;
    private bool canCheck = false; //For check if player is wallsliding
    private float lastWallJumpTime = 0f; // Track when wall jump started for timeout safety
    private LevelManager levelManager;

    public float life = 10f; //Life of the player
    public bool invincible = false; //If player can die
    private bool canMove = true; //If player can move

    private Animator animator;
    public ParticleSystem particleJumpUp; //Trail particles
    public ParticleSystem particleJumpDown; //Explosion particles

    private float jumpWallStartX = 0;
    private float jumpWallDistX = 0; //Distance between player and wall
    private bool limitVelOnWallJump = false; //For limit wall jump distance with low fps

    [Header("Events")]
    [Space]

    public UnityEvent OnFallEvent;
    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Initialize events
        if (OnFallEvent == null)
            OnFallEvent = new UnityEvent();
        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();
            
        // Create wallCheck if it doesn't exist
        if (m_WallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.parent = transform;
            wallCheckObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            m_WallCheck = wallCheckObj.transform;
            Debug.LogWarning("WallCheck created automatically. Consider assigning it properly in inspector.");
        }

        if (m_GroundCheck == null){
            Debug.LogError("Ground check transform not assigned!");
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            m_GroundCheck = groundCheckObj.transform;
        }
        
        if (m_WhatIsGround.value == 0){
            Debug.LogError("Ground layer mask not set!");
        }
    }
    
    private void Start()
    {
        levelManager = Object.FindFirstObjectByType<LevelManager>();
        Debug.Log("CharacterController2D initialized. GroundCheck at: " + 
              (m_GroundCheck != null ? m_GroundCheck.localPosition.ToString() : "NULL") + 
              ", Radius: " + k_GroundedRadius);
    }

    private void Update()
    {
        // Add emergency unstick key
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Emergency movement reset");
            canMove = true;
            limitVelOnWallJump = false;
            isDashing = false;
            isWallSliding = false;
            oldWallSlidding = false;
        }
    }

    private void FixedUpdate()
    {
        bool wasGrounded = m_Grounded;
        m_Grounded = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                if (!wasGrounded)
                {
                    OnLandEvent.Invoke();
                    if (!m_IsWall && !isDashing && particleJumpDown != null) 
                        particleJumpDown.Play();
                    canDoubleJump = true;
                    
                    if (m_Rigidbody2D.linearVelocity.y < 0f)
                        limitVelOnWallJump = false;
                        
                    if (animator != null)
                    {
                        animator.SetBool("IsJumping", false);
                        animator.SetBool("IsDoubleJumping", false);
                        animator.SetBool("JumpUp", false);
                    }
                }
                break;
            }
        }

        m_IsWall = false;

        if (!m_Grounded)
        {
            OnFallEvent.Invoke();
            Collider2D[] collidersWall = Physics2D.OverlapCircleAll(m_WallCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < collidersWall.Length; i++)
            {
                if (collidersWall[i].gameObject != null && collidersWall[i].gameObject != gameObject)
                {
                    isDashing = false;
                    m_IsWall = true;
                    break;
                }
            }
            prevVelocityX = m_Rigidbody2D.linearVelocity.x;
        }

        if (limitVelOnWallJump)
        {
            // Add timeout safety to prevent getting permanently stuck
            if (Time.time - lastWallJumpTime > 0.5f)
            {
                limitVelOnWallJump = false;
                canMove = true;
                return;
            }

            if (m_Rigidbody2D.linearVelocity.y < -0.5f)
            {
                limitVelOnWallJump = false;
                canMove = true; // Always ensure canMove is reset when falling
                return;
            }
                    
            jumpWallDistX = (jumpWallStartX - transform.position.x) * transform.localScale.x;
            
            if (jumpWallDistX < -0.5f && jumpWallDistX > -1f) 
            {
                canMove = true;
            }
            else if (jumpWallDistX < -1f && jumpWallDistX >= -2f) 
            {
                canMove = true;
                m_Rigidbody2D.linearVelocity = new Vector2(10f * transform.localScale.x, m_Rigidbody2D.linearVelocity.y);
            }
            else if (jumpWallDistX < -2f) 
            {
                limitVelOnWallJump = false;
                canMove = true; // Add this line to ensure canMove is reset
                m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
            }
            else if (jumpWallDistX > 0) 
            {
                limitVelOnWallJump = false;
                canMove = true; // Add this line to ensure canMove is reset
                m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
            }
        }
    }


    public void Move(float move, bool jump, bool dash)
    {
        if (canMove) {
            if (dash && canDash && !isWallSliding)
            {
                //m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_DashForce, 0f));
                StartCoroutine(DashCooldown());
            }
            // If crouching, check to see if the character can stand up
            if (isDashing)
            {
                m_Rigidbody2D.linearVelocity = new Vector2(transform.localScale.x * m_DashForce, 0);
            }
            //only control the player if grounded or airControl is turned on
            else if (m_Grounded || m_AirControl)
            {
                if (m_Rigidbody2D.linearVelocity.y < -limitFallSpeed)
                    m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, -limitFallSpeed);
                // Move the character by finding the target velocity
                Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.linearVelocity.y);
                // And then smoothing it out and applying it to the character
                m_Rigidbody2D.linearVelocity = Vector3.SmoothDamp(m_Rigidbody2D.linearVelocity, targetVelocity, ref velocity, m_MovementSmoothing);

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight && !isWallSliding)
                {
                    // ... flip the player.
                    Flip();
                }
                // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight && !isWallSliding)
                {
                    // ... flip the player.
                    Flip();
                }
            }
            // If the player should jump...
            if (m_Grounded && jump)
            {
                // Add a vertical force to the player.
                if (animator != null)
                {
                    animator.SetBool("IsJumping", true);
                    animator.SetBool("JumpUp", true);
                }
                
                m_Grounded = false;
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
                canDoubleJump = true;
                
                if (particleJumpDown != null) particleJumpDown.Play();
                if (particleJumpUp != null) particleJumpUp.Play();
            }
            else if (!m_Grounded && jump && canDoubleJump && !isWallSliding)
            {
                canDoubleJump = false;
                m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce / 1.2f));
                if (animator != null)
                    animator.SetBool("IsDoubleJumping", true);
            }

            else if (m_IsWall && !m_Grounded)
            {
                if (!oldWallSlidding && m_Rigidbody2D.linearVelocity.y < 0 || isDashing)
                {
                    isWallSliding = true;
                    m_WallCheck.localPosition = new Vector3(-m_WallCheck.localPosition.x, m_WallCheck.localPosition.y, 0);
                    Flip();
                    StartCoroutine(WaitToCheck(0.1f));
                    canDoubleJump = true;
                    if (animator != null) 
                        animator.SetBool("IsWallSliding", true);
                }
                isDashing = false;

                if (isWallSliding)
                {
                    if (move * transform.localScale.x > 0.1f)
                    {
                        StartCoroutine(WaitToEndSliding());
                    }
                    else 
                    {
                        oldWallSlidding = true;
                        m_Rigidbody2D.linearVelocity = new Vector2(-transform.localScale.x * 2, -m_WallSlidingSpeed);
                    }
                }

                if (jump && isWallSliding)
                {
                    if (animator != null)
                    {
                        animator.SetBool("IsJumping", true);
                        animator.SetBool("JumpUp", true); 
                    }
                    
                    m_Rigidbody2D.linearVelocity = Vector2.zero;
                    m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_JumpForce * m_WallJumpHorizontalMultiplier, 
                                                      m_JumpForce * m_WallJumpVerticalMultiplier), 
                                                      ForceMode2D.Impulse);
                    jumpWallStartX = transform.position.x;
                    limitVelOnWallJump = true;
                    lastWallJumpTime = Time.time; // Add this line to track when wall jump happened
                    canDoubleJump = true;
                    isWallSliding = false;
                    
                    if (animator != null)
                        animator.SetBool("IsWallSliding", false);
                        
                    oldWallSlidding = false;
                    m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                    canMove = false;
                }
                else if (dash && canDash)
                {
                    isWallSliding = false;
                    if (animator != null)
                        animator.SetBool("IsWallSliding", false);
                    oldWallSlidding = false;
                    m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                    canDoubleJump = true;
                    StartCoroutine(DashCooldown());
                }
            }
            else if (isWallSliding && !m_IsWall && canCheck) 
            {
                isWallSliding = false;
                if (animator != null)
                    animator.SetBool("IsWallSliding", false);
                oldWallSlidding = false;
                m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
                canDoubleJump = true;
            }
        }
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    // Update ApplyDamage to handle both formats (for backward compatibility)
    public void ApplyDamage(DamageInfo damageInfo)
    {
        ApplyDamage(damageInfo.DamageAmount, damageInfo.DamageSource);
    }

    public void ApplyDamage(float damage, Vector3 position) 
    {
        if (!invincible)
        {
            if (animator != null)
                animator.SetBool("Hit", true);
                
            life -= damage;
            Vector2 damageDir = Vector3.Normalize(transform.position - position) * 40f;
            m_Rigidbody2D.linearVelocity = Vector2.zero;
            m_Rigidbody2D.AddForce(damageDir * 10);
            
            if (life <= 0)
            {
                StartCoroutine(WaitToDead());
            }
            else
            {
                StartCoroutine(Stun(0.25f));
                StartCoroutine(MakeInvincible(1f));
            }
        }
    }
    
    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Garbage"))
        {
            collider.gameObject.SetActive(false);
            if (levelManager != null)
                levelManager.IncrementAmountOfGarbage();
        }
    }

    IEnumerator DashCooldown()
    {
        if (animator != null)
            animator.SetBool("IsDashing", true);
            
        isDashing = true;
        canDash = false;
        yield return new WaitForSeconds(0.1f);
        isDashing = false;
        
        if (animator != null)
            animator.SetBool("IsDashing", false);
            
        yield return new WaitForSeconds(0.5f);
        canDash = true;
    }

    IEnumerator Stun(float time) 
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }
    
    IEnumerator MakeInvincible(float time) 
    {
        invincible = true;
        yield return new WaitForSeconds(time);
        invincible = false;
    }
    
    IEnumerator WaitToMove(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    IEnumerator WaitToCheck(float time)
    {
        canCheck = false;
        yield return new WaitForSeconds(time);
        canCheck = true;
    }

    IEnumerator WaitToEndSliding()
    {
        yield return new WaitForSeconds(0.1f);
        canDoubleJump = true;
        isWallSliding = false;
        if (animator != null)
            animator.SetBool("IsWallSliding", false);
        oldWallSlidding = false;
        m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
    }

    IEnumerator WaitToDead()
    {
        if (animator != null)
            animator.SetBool("IsDead", true);
            
        canMove = false;
        invincible = true;
        
        // If you have an Attack component, disable it
        var attackComponent = GetComponent<Attack>();
        if (attackComponent != null)
            attackComponent.enabled = false;
            
        yield return new WaitForSeconds(0.4f);
        m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
        yield return new WaitForSeconds(1.1f);
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Add this to visualize the ground and wall check points
    void OnDrawGizmosSelected()
    {
        if (m_GroundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(m_GroundCheck.position, k_GroundedRadius);
        }
        
        if (m_WallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(m_WallCheck.position, k_GroundedRadius);
        }
    }
}