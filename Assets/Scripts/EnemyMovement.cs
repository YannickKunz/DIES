// EnemyMovement.cs
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    protected EnemyData data;
    protected Rigidbody2D rb;
    protected Vector2 moveDirection;
    protected Vector2 originalScale;
    protected bool isFacingRight = true;
    protected Transform groundCheck;
    protected bool isGrounded;
    protected EnemyAnimator animator;
    
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
        
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<EnemyAnimator>();
        originalScale = transform.localScale;
        
        // Create ground check if needed
        SetupGroundCheck();
    }
    
    private void SetupGroundCheck()
    {
        if (!groundCheck)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    protected virtual void FixedUpdate()
    {
        // Check if grounded
        CheckGrounded();
        
        // Apply movement if grounded
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(moveDirection.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }
        
        // Force to ground if needed
        KeepOnGround();
    }
    
    protected void CheckGrounded()
    {
        if (groundCheck == null) return;
        
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    protected virtual void KeepOnGround()    {
        if (!isGrounded)
        {
            Vector2 rayStart = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 10f, groundLayer);
            if (hit.collider != null)
            {
                float correctY = hit.point.y + 0.9f; 
                transform.position = new Vector3(transform.position.x, correctY, transform.position.z);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
    }
    
    public void SetMoveDirection(Vector2 direction, bool isPatrolling)
    {
        moveDirection = direction;
        
        // Face the right direction
        if (direction.x != 0)
        {
            Flip(direction.x > 0);
        }
        
        // Update animator
        if (animator)
        {
            animator.SetWalking(Mathf.Abs(direction.x) > 0.1f);
        }
    }
    
    public void StopMoving()
    {
        moveDirection = Vector2.zero;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        if (animator)
        {
            animator.SetWalking(false);
        }
    }
    
    protected float CurrentMoveSpeed => data.moveSpeed;
    
    public float PatrolSpeed => data.patrolSpeed;
    
    public virtual void Flip(bool faceRight)
    {
        // Only flip if the facing direction changed
        if (isFacingRight != faceRight)
        {
            isFacingRight = faceRight;
            
            // Only flip X scale, maintain Y and Z
            Vector3 theScale = transform.localScale;
            theScale.x = originalScale.x * (faceRight ? 1 : -1);
            transform.localScale = theScale;
        }
    }
    
    public bool IsFacingRight => isFacingRight;
    
    public bool IsGrounded => isGrounded;
}