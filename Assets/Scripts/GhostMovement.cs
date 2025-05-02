using UnityEngine;
using System.Collections;

public class GhostMovement : EnemyMovement
{
    [Header("Instance Override Settings")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Ghost Movement Settings")]
    [SerializeField] private float hoverSmoothing = 0.2f;
    [SerializeField] private float hoverBobAmplitude = 0.1f;
    [SerializeField] private float hoverBobSpeed = 1.5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float climbSpeed = 3f;
    
    // Access properties that check for instance overrides
    public float HoverSmoothing => useInstanceValues ? hoverSmoothing : 
        (ghostData != null ? ghostData.hoverSmoothing : 0.2f);
    public float BobAmplitude => useInstanceValues ? hoverBobAmplitude : 
        (ghostData != null ? ghostData.hoverBobAmplitude : 0.1f);
    public float BobSpeed => useInstanceValues ? hoverBobSpeed : 
        (ghostData != null ? ghostData.hoverBobSpeed : 1.5f);
    public float JumpForce => useInstanceValues ? jumpForce : 
        (ghostData != null ? ghostData.jumpForce : 10f);
    public float ClimbSpeed => useInstanceValues ? climbSpeed : 
        (ghostData != null ? ghostData.climbSpeed : 3f);
    
    // State tracking
    private bool isClimbing = false;
    private Vector3 moveVelocity = Vector3.zero;
    private float targetHoverHeight;
    private float bobTime = 0f;
    
    // References
    private GhostData ghostData;
    private GhostEnemy ghostEnemy;
    
    public new void Initialize(EnemyData enemyData)
    {
        base.Initialize(enemyData);
        
        // Cast to GhostData if possible
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
        }
        
        // Get reference to GhostEnemy component
        ghostEnemy = GetComponent<GhostEnemy>();
    }
    
    private void Update()
    {
        // Update hover bob effect
        bobTime += Time.deltaTime * BobSpeed;
    }
    
    protected override void FixedUpdate()
    {
        // Skip base FixedUpdate if climbing
        if (isClimbing)
            return;
                
        // Apply hover bob effect
        float bobOffset = Mathf.Sin(bobTime) * BobAmplitude;
        
        // Adjust vertical position for hover
        if (targetHoverHeight != 0)
        {
            float targetY = targetHoverHeight + bobOffset;
            Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);
            
            // Smoothly move to hover height
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref moveVelocity, 
                HoverSmoothing
            );
        }
        
        // Call base for horizontal movement if not climbing
        if (!isClimbing)
        {
            // Skip ground check logic from base class
            CheckGrounded();
            rb.linearVelocity = new Vector2(moveDirection.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }
    // Add this in FixedUpdate method after calculating moveDirection
    Debug.DrawRay(transform.position, moveDirection * 1.5f, Color.blue);
    Debug.Log($"Ghost moveDirection: {moveDirection}, isFacingRight: {isFacingRight}");

    }
    
    // Method to adjust hover height
    public void AdjustHoverHeight(float height)
    {
        targetHoverHeight = height;
    }
    
    // Enhanced Jump method with better physics
    public void Jump(float heightNeeded)
    {
        if (rb == null)
            return;
                
        // Calculate appropriate jump force based on height needed
        float scaledForce = Mathf.Max(JumpForce, JumpForce * (heightNeeded / 2f));
        
        // Apply upward force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * scaledForce, ForceMode2D.Impulse);
        
        // Add slight forward motion in the direction we're facing
        float horizontalBoost = IsFacingRight ? 3f : -3f;
        rb.AddForce(Vector2.right * horizontalBoost, ForceMode2D.Impulse);
    }
    
    // Handle climb start
    // Enhanced climbing system
    public void StartClimbing()
    {
        if (rb == null)
            return;
                
        isClimbing = true;
        
        // Freeze position and disable gravity for climbing
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        
        // Add visual effect if you want (optional)
        // e.g., Instantiate(climbEffectPrefab, transform.position, Quaternion.identity);
    }
    
    // Handle climbing movement
    // Climb method - controlled vertical movement
    public void Climb()
    {
        if (!isClimbing || rb == null)
            return;
                
        // Move upward at climb speed
        rb.linearVelocity = new Vector2(0, ClimbSpeed);
    }
    
    // Finish climbing with smooth transition
    public void FinishClimbing()
    {
        if (rb == null)
            return;
                
        isClimbing = false;
        
        // Restore normal physics
        rb.gravityScale = 1;
        
        // Add a small horizontal push to get over the ledge
        float pushDirection = IsFacingRight ? 1 : -1;
        rb.linearVelocity = new Vector2(pushDirection * 2, 1);
    }
    // Add this method to override the base class Flip behavior


    public void JumpToTarget(float heightNeeded, Vector2 target){
        if (rb == null)
            return;
                
        // Calculate distance to target
        float distanceX = target.x - transform.position.x;
        
        // Calculate physics-based jump velocity using projectile motion formula
        // For a projectile to hit target (x,y): 
        // v_y = sqrt(2gy) and v_x = x * sqrt(g/2y)
        float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        
        // Add a bit more height to ensure clearing the platform
        float jumpHeight = heightNeeded + 1.0f;
        
        // Calculate velocities
        float verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
        float timeToApex = verticalVelocity / gravity;
        float horizontalVelocity = distanceX / (timeToApex * 2);
        
        // Hard-cap horizontal velocity to avoid huge overshoots
        horizontalVelocity = Mathf.Clamp(horizontalVelocity, -8f, 8f);
        
        // Reset any current velocity and apply the new jump velocities
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
        
        Debug.Log($"Ghost jumping with velocity ({horizontalVelocity:F1}, {verticalVelocity:F1}) " +
                $"to reach target at ({target.x:F1}, {target.y:F1})");
    }   

    public override void Flip(bool faceRight)
    {
        // Update facing direction flag
        isFacingRight = faceRight;
        
        // Debug to verify our direction understanding
        Debug.Log($"Ghost flipping to face: {(faceRight ? "Right" : "Left")}");
        
        // Apply correct rotation based on sprite orientation
        // IMPORTANT: Check which way your ghost sprite is originally facing!
        // If your sprite naturally faces right, use this:
        //float targetRotation = faceRight ? 0f : 180f;
        
        // If your sprite naturally faces left, use this instead:
        float targetRotation = faceRight ? 180f : 0f;
        
        transform.rotation = Quaternion.Euler(0, targetRotation, 0);
    }
    // Override movement to account for climbing state
    public new void SetMoveDirection(Vector2 direction, bool isPatrolling)
    {
        if (isClimbing)
            return;
            
        base.SetMoveDirection(direction, isPatrolling);
    }
    private void OnDrawGizmos()
    {
        // Draw a line showing which way the ghost is facing
        Vector3 facingDirection = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + facingDirection * 0.5f);
        
        // Draw text to show current facing state
        #if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, 
                "Facing: " + (isFacingRight ? "Right →" : "← Left"));
        }
        #endif
}

}