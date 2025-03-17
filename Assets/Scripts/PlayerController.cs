using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private BoxCollider2D boxCollider;

    private float horizontalInput;
    private bool isJumping = false;
    private bool isDashing = false;
    private bool isGrounded = false; // Track if the player is on the ground

    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Get movement input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump input with ground check
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isJumping = true;
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
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            Vector3 normal = other.contacts[0].normal;
            if (normal == Vector3.up)
            {    
                isGrounded = true;
                isJumping = false;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}