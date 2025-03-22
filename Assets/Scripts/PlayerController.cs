using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private LevelManager levelManager;
    public Rigidbody2D rb;
    public float runSpeed = 8f;
    public float jumpForce = 12f; // Base jump force for ground jumps
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float wallslidingSpeed = 6f;
    public float wallJumpCooldown = 0.3f;  // Time after a wall jump when wall sliding is disabled

    [Header("Wall Jump Multipliers")]
    public float wallJumpHorizontalMultiplier = 2f; // Multiplies the horizontal impulse for a wall jump
    public float wallJumpVerticalMultiplier = 1.5f;   // Multiplies the vertical impulse for a wall jump

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private float horizontalInput;
    private bool isJumping = false;
    private bool isDashing = false;
    private bool isGrounded = false;
    private bool isWallsliding = false;
    private bool isWallJumping = false;

    // Store the wall jump direction: 1 = jump right (wall is on left), -1 = jump left (wall is on right)
    private int wallJumpDir = 0;

    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;
    private float wallJumpTimer = 0f; // Timer to ignore wall collisions after a wall jump

    void Start()
    {
        levelManager = Object.FindFirstObjectByType<LevelManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // When wall sliding, cancel any horizontal input toward the wall.
        if (isWallsliding && wallJumpDir != 0)
        {
            if (wallJumpDir == 1 && horizontalInput < 0)
                horizontalInput = 0;
            else if (wallJumpDir == -1 && horizontalInput > 0)
                horizontalInput = 0;
        }

        // Check if grounded.
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Jump input: if grounded, perform a ground jump; if wall sliding, perform a wall jump.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
                isJumping = true;
            else if (isWallsliding && wallJumpDir != 0)
                isWallJumping = true;
        }

        // Dash input.
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        if (wallJumpTimer > 0)
            wallJumpTimer -= Time.fixedDeltaTime;

        // Dashing logic.
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(horizontalInput) * dashSpeed, rb.linearVelocity.y);
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
                isDashing = false;
        }
        // When grounded, use direct input.
        else if (isGrounded)
        {
            rb.linearVelocity = new Vector2(horizontalInput * runSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Air control: smoothly adjust horizontal velocity without completely overriding momentum.
            float targetX = horizontalInput * runSpeed;
            float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, 0.1f);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        // Ground jump.
        if (isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = false;
        }

        // Wall jump logic.
        if (isWallJumping && wallJumpDir != 0)
        {
            // Do not reset velocity here—preserve some momentum.
            Vector2 jumpImpulse = new Vector2(
                wallJumpDir * jumpForce * wallJumpHorizontalMultiplier,
                jumpForce * wallJumpVerticalMultiplier
            );
            rb.AddForce(jumpImpulse, ForceMode2D.Impulse);
            isWallJumping = false;
            isWallsliding = false;
            wallJumpTimer = wallJumpCooldown;
            wallJumpDir = 0;
        }
        else if (isWallsliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallslidingSpeed);
        }
    }

    // On collision, determine wall jump direction and enable wall sliding.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (wallJumpTimer > 0)
            return;
        if (!collision.gameObject.CompareTag("Wall"))
            return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;
            if (Mathf.Abs(normal.x) > 0.5f && normal.y < 0.5f)
            {
                // Determine wall jump direction based on the wall normal.
                wallJumpDir = (normal.x > 0) ? 1 : -1;
                if (rb.linearVelocity.y < 0)
                    isWallsliding = true;
                break;
            }
        }
    }

    // While colliding, keep updating the wall jump direction and sliding state.
    void OnCollisionStay2D(Collision2D collision)
    {
        if (wallJumpTimer > 0)
            return;
        if (!collision.gameObject.CompareTag("Wall"))
            return;

        bool validContact = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;
            if (Mathf.Abs(normal.x) > 0.5f && normal.y < 0.5f)
            {
                wallJumpDir = (normal.x > 0) ? 1 : -1;
                if (rb.linearVelocity.y < 0)
                {
                    isWallsliding = true;
                    validContact = true;
                    break;
                }
            }
        }
        if (!validContact)
        {
            isWallsliding = false;
            wallJumpDir = 0;
        }
    }

    // Reset wall sliding state when leaving the wall.
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isWallsliding = false;
            wallJumpDir = 0;
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

    // Visualize the ground check area.
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

