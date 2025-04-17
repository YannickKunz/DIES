using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class DamageInfo
{
    public float DamageAmount;
    public Vector3 DamageSource;
    
    public DamageInfo(float damage, Vector3 source)
    {
        DamageAmount = damage;
        DamageSource = source;
    }
}

public class PlayerController : MonoBehaviour
{
    private LevelManager levelManager;
    
    [Header("Movement Settings")]
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [Range(0, .3f)] [SerializeField] private float movementSmoothing = 0.05f;
    [SerializeField] private bool airControl = true;
    [SerializeField] private float limitFallSpeed = 25f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float dashCooldown = 0.6f;
    
    [Header("Wall Slide Settings")]
    [SerializeField] private float wallSlidingSpeed = 5f;
    [SerializeField] private Transform wallCheck;
    
    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Wall Jump Settings")]
    [SerializeField] private float wallJumpHorizontalMultiplier = 1.2f;
    [SerializeField] private float wallJumpVerticalMultiplier = 1f;
    
    [Header("Player Stats")]
    public float life = 10f;
    public bool invincible = false;
    
    [Header("Particles")]
    public ParticleSystem particleJumpUp;
    public ParticleSystem particleJumpDown;
    
    [Header("Events")]
    [Space]
    public UnityEvent OnFallEvent;
    public UnityEvent OnLandEvent;
    
    // State variables
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool wasGrounded;
    private bool canDoubleJump = true;
    private bool isWallSliding = false;
    private bool oldWallSliding = false;
    private bool isWall = false;
    private bool canDash = true;
    private bool isDashing = false;
    private bool facingRight = true;
    private bool canMove = true;
    private bool canCheck = false;
    private bool limitVelOnWallJump = false;
    private float lastWallJumpTime = 0f;
    
    // Movement helpers
    private float horizontalInput;
    private Vector3 velocity = Vector3.zero;
    private float jumpWallStartX = 0;
    private float jumpWallDistX = 0;
    private float prevVelocityX = 0f;

    void Start()
    {
        levelManager = Object.FindFirstObjectByType<LevelManager>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // Initialize events
        if (OnFallEvent == null)
            OnFallEvent = new UnityEvent();
        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();
            
        // Create wallCheck if it doesn't exist
        if (wallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.parent = transform;
            wallCheckObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheck = wallCheckObj.transform;
            Debug.LogWarning("WallCheck created automatically. Consider assigning it properly in inspector.");
        }

            if (groundCheck == null){
        Debug.LogError("Ground check transform not assigned!");
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.parent = transform;
        groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
        groundCheck = groundCheckObj.transform;
        }
    
    if (groundLayer.value == 0){
        Debug.LogError("Ground layer mask not set!");
    }
    
    Debug.Log("PlayerController initialized. GroundCheck at: " + 
              (groundCheck != null ? groundCheck.localPosition.ToString() : "NULL") + 
              ", Radius: " + groundCheckRadius);
              }


    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        /*
        // Jump input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed! Grounded: " + isGrounded + ", WallSliding: " + isWallSliding + ", CanDoubleJump: " + canDoubleJump);

            if (isGrounded)
            {
                Jump();
            }
            else if (isWallSliding)
            {
                WallJump();
            }
            else if (canDoubleJump)
            {
                DoubleJump();
            }
        }
        
        // Dash input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isWallSliding)
        {
            StartCoroutine(DashCooldown());
        }
        */
        // Reset jump animation when not jumping
        if (Input.GetKeyDown(KeyCode.R)) // R for Reset
        {
            Debug.Log("Movement reset requested");
            canMove = true;
            limitVelOnWallJump = false;
            isDashing = false;
            isWallSliding = false;
            oldWallSliding = false;
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        CheckWall();
        HandleWallJumpDistance();
        
        if (canMove)
        {
            if (isDashing)
            {
                rb.linearVelocity = new Vector2(transform.localScale.x * dashForce, 0);
            }
            else if (isGrounded || airControl)
            {
                // Limit fall speed
                if (rb.linearVelocity.y < -limitFallSpeed)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -limitFallSpeed);
                
                // Movement with smoothing
                Vector3 targetVelocity = new Vector2(horizontalInput * runSpeed, rb.linearVelocity.y);
                rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
                
                // Handle player flipping
                if (horizontalInput > 0 && !facingRight && !isWallSliding)
                {
                    Flip();
                }
                else if (horizontalInput < 0 && facingRight && !isWallSliding)
                {
                    Flip();
                }
            }
            
            // Handle wall sliding
            if (isWall && !isGrounded)
            {
                if (!oldWallSliding && rb.linearVelocity.y < 0 || isDashing)
                {
                    isWallSliding = true;
                    wallCheck.localPosition = new Vector3(-wallCheck.localPosition.x, wallCheck.localPosition.y, 0);
                    Flip();
                    StartCoroutine(WaitToCheck(0.1f));
                    canDoubleJump = true;
                    if (animator != null) animator.SetBool("IsWallSliding", true);
                }
                isDashing = false;
                
                if (isWallSliding)
                {
                    if (horizontalInput * transform.localScale.x > 0.1f)
                    {
                        StartCoroutine(WaitToEndSliding());
                    }
                    else
                    {
                        oldWallSliding = true;
                        rb.linearVelocity = new Vector2(-transform.localScale.x * 2, -wallSlidingSpeed);
                    }
                }
            }
            else if (isWallSliding && !isWall && canCheck)
            {
                isWallSliding = false;
                if (animator != null) animator.SetBool("IsWallSliding", false);
                oldWallSliding = false;
                wallCheck.localPosition = new Vector3(Mathf.Abs(wallCheck.localPosition.x), wallCheck.localPosition.y, 0);
                canDoubleJump = true;
            }
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        bool wasGroundedBefore = isGrounded;
        isGrounded = false;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                isGrounded = true;
                
                // Debug.Log the object that's causing us to be grounded
                if (!wasGroundedBefore)
                    Debug.Log("Player grounded on: " + colliders[i].gameObject.name);
                
                if (!wasGrounded)
                {
                    OnLandEvent.Invoke();
                    if (!isWall && !isDashing && particleJumpDown != null)
                        particleJumpDown.Play();
                    canDoubleJump = true;
                    
                    if (rb.linearVelocity.y < 0f)
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
        
        // Log when grounded state changes
        if (wasGroundedBefore != isGrounded)
        {
            Debug.Log("Grounded state changed: " + (isGrounded ? "GROUNDED" : "IN AIR"));
        }
        
        if (!isGrounded && wasGrounded)
        {
            OnFallEvent.Invoke();
        }
    }

    void CheckWall()
    {
        isWall = false;
        
        if (!isGrounded)
        {
            Collider2D[] collidersWall = Physics2D.OverlapCircleAll(wallCheck.position, groundCheckRadius, groundLayer);
            for (int i = 0; i < collidersWall.Length; i++)
            {
                if (collidersWall[i].gameObject != null && collidersWall[i].gameObject != gameObject)
                {
                    isDashing = false;
                    isWall = true;
                    break;
                }
            }
            prevVelocityX = rb.linearVelocity.x;
        }
    }

void HandleWallJumpDistance()
{
    if (limitVelOnWallJump)
    {
        // Add a timeout safety to prevent getting permanently stuck
        if (Time.time - lastWallJumpTime > 0.5f)
        {
            limitVelOnWallJump = false;
            canMove = true;
            return;
        }

        if (rb.linearVelocity.y < -0.5f)
        {
            limitVelOnWallJump = false;
            canMove = true; // Always ensure canMove is reset when falling
            return;
        }
                
        jumpWallDistX = (jumpWallStartX - transform.position.x) * transform.localScale.x;
        
        // First two conditions already set canMove = true correctly
        if (jumpWallDistX < -0.5f && jumpWallDistX > -1f)
        {
            canMove = true;
        }
        else if (jumpWallDistX < -1f && jumpWallDistX >= -2f)
        {
            canMove = true;
            rb.linearVelocity = new Vector2(10f * transform.localScale.x, rb.linearVelocity.y);
        }
        else if (jumpWallDistX < -2f)
        {
            limitVelOnWallJump = false;
            canMove = true; // Add this line to ensure canMove is reset
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else if (jumpWallDistX > 0)
        {
            limitVelOnWallJump = false;
            canMove = true; // Add this line to ensure canMove is reset
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
}

    void Jump()
    {
        Debug.Log("Jump executed! Force: " + jumpForce);
        
        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("JumpUp", true);
        }
        
        isGrounded = false;
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        canDoubleJump = true;
        
        if (particleJumpDown != null) particleJumpDown.Play();
        if (particleJumpUp != null) particleJumpUp.Play();
    }

    void DoubleJump()
    {
        canDoubleJump = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(new Vector2(0f, jumpForce / 1.2f), ForceMode2D.Impulse);
        
        if (animator != null)
            animator.SetBool("IsDoubleJumping", true);
    }

    void WallJump()
    {
        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.SetBool("JumpUp", true);
        }
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(transform.localScale.x * jumpForce * wallJumpHorizontalMultiplier, 
                            jumpForce * wallJumpVerticalMultiplier), 
                            ForceMode2D.Impulse);
                            
        jumpWallStartX = transform.position.x;
        limitVelOnWallJump = true;
        canDoubleJump = true;
        isWallSliding = false;
        lastWallJumpTime = Time.time; // Add this line to track when wall jump happened
        
        if (animator != null)
            animator.SetBool("IsWallSliding", false);
            
        oldWallSliding = false;
        wallCheck.localPosition = new Vector3(Mathf.Abs(wallCheck.localPosition.x), wallCheck.localPosition.y, 0);
        canMove = false;
    }

    private void Flip()
    {
        // Switch the way the player is labeled as facing
        facingRight = !facingRight;

        // Multiply the player's x local scale by -1
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }


    public void Move(float move, bool jump, bool dash)
    {
        // Store horizontal input for use in FixedUpdate
        horizontalInput = move;
        
        // Handle jump input
        if (jump)
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (isWallSliding)
            {
                WallJump();
            }
            else if (canDoubleJump)
            {
                DoubleJump();
            }
        }
        
        // Handle dash input
        if (dash && canDash && !isWallSliding)
        {
            StartCoroutine(DashCooldown());
        }
    }

public void ApplyDamage(DamageInfo info)
{
    if (!invincible)
    {
        if (animator != null)
            animator.SetBool("Hit", true);
            
        // Subtract damage using the DamageInfo provided
        life -= info.DamageAmount;
        
        // Calculate the direction from which the damage came
        Vector2 damageDir = (transform.position - info.DamageSource).normalized * 40f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(damageDir * 10);
        
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

    // Coroutines
    IEnumerator DashCooldown()
    {
        if (animator != null)
            animator.SetBool("IsDashing", true);
            
        isDashing = true;
        canDash = false;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        
        if (animator != null)
            animator.SetBool("IsDashing", false);
            
        yield return new WaitForSeconds(dashCooldown);
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
        oldWallSliding = false;
        wallCheck.localPosition = new Vector3(Mathf.Abs(wallCheck.localPosition.x), wallCheck.localPosition.y, 0);
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
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        yield return new WaitForSeconds(1.1f);
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    // Visualize the ground and wall check areas
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wallCheck.position, groundCheckRadius);
        }
    }
}