using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    private LevelManager levelManager;
    public Rigidbody2D rb;
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float wallslidingSpeed = 6f;

    private BoxCollider2D boxCollider;

    private float horizontalInput;
    private bool isJumping = false;
    private bool isDashing = false;
    private bool isGrounded = false; // Track if the player is on the ground
    private bool isWallsliding = false; 
    private bool isWallJumping = false;
    private bool jumpLeft = false;
    private bool jumpRight = false;


    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

    void Start()
    {
        //find object of type LevelManager (one per scene)
        levelManager = FindFirstObjectByType<LevelManager>();

        rb = GetComponent<Rigidbody2D>();



    }

    void Update()
    {
        //the level manager contains methods to manage the game state
        if (levelManager == null || !levelManager.CanPlay ()) {
            //freeze the player speed and exit from Update
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Get movement input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump input with ground check
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isJumping = true;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isWallsliding)
        {
            isWallJumping= true;
        }

        // Dash input
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        float speed = runSpeed;

        // Apply dash
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(horizontalInput) * dashSpeed, rb.linearVelocity.y);
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
        
        // Apply movement (if not dashing)
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);
        }

        // Apply jump (independent of dash)
        if (isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = false;
        }

        if (isWallJumping)
        {
        // Reset velocity before applying wall jump force
            rb.linearVelocity = Vector2.zero;

            if (jumpRight)
            {
                rb.linearVelocity = new Vector2(2*jumpForce, jumpForce);
            }

            if (jumpLeft)
            {
                rb.linearVelocity = new Vector2(-2*jumpForce, jumpForce);
            }

            isWallsliding = false;
            isWallJumping = false;
        }


        if (isWallsliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallslidingSpeed);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        {
            Vector3 normal = other.contacts[0].normal;
            if (normal == Vector3.up)
            {    
                isGrounded = true;
                isJumping = false;
            }
            else if (normal == Vector3.left)
            {
                if (!isWallJumping)
                {
                    isWallsliding = true;
                    jumpLeft = true;
                }
            }
            else if (normal == Vector3.right)
            {    
                if (!isWallJumping)
                {
                    isWallsliding = true;
                    jumpRight = true;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        {
            isGrounded = false;
            isWallsliding = false;
            jumpLeft = false;
            jumpRight = false;
        }
    }

    public void OnTriggerEnter2D(Collider2D collider){
        if (collider.tag == "Garbage") {
        collider.gameObject.SetActive (false);
            //
            // 
            if (levelManager != null) {
                //play a sound
            //    levelManager.PlaySFX (SoundManager.GARBAGE);
                //increment garbage
                levelManager.IncrementAmountOfGarbage ();
            }
        }
    }
}