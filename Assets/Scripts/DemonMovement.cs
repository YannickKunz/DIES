using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Pathfinding; // Added for A*

public class DemonMovement : EnemyMovement
{
    [Header("Instance Override Settings")]
    [Tooltip("When enabled, uses the values set in this component rather than from the DemonData asset")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Demon Movement Settings")]
    [Tooltip("How quickly the demon reaches its target hover height (lower = faster adjustment)")]
    [SerializeField] private float hoverSmoothing = 0.2f;
    [Tooltip("How much the demon bobs up and down while hovering")]
    [SerializeField] private float hoverBobAmplitude = 0.1f;
    [Tooltip("How fast the demon bobs up and down")]
    [SerializeField] private float hoverBobSpeed = 1.5f;
    [Tooltip("How powerful the demon's jumps are")]
    [SerializeField] private float jumpForce = 10f;
    
    [Header("Ground check")]
    [Tooltip("Transform used to check for ground beneath the demon (auto-created if null)")]
    [SerializeField] private Transform groundCheckTransform;
    [Tooltip("Maximum distance to check downward for ground")]
    [SerializeField] private float groundCheckDistance = 10f;
    [Tooltip("Radius of the circle used to detect ground beneath the demon")]
    [SerializeField] private float demonGroundCheckRadius = 0.2f;

    // Add surfaceY variable for ground tracking
    private float surfaceY = 0f;
    
    // Access properties that check for instance overrides
    public float HoverSmoothing => useInstanceValues ? hoverSmoothing : 
        (demonData != null ? demonData.hoverSmoothing : 0.2f);
    public float BobAmplitude => useInstanceValues ? hoverBobAmplitude : 
        (demonData != null ? demonData.hoverBobAmplitude : 0.1f);
    public float BobSpeed => useInstanceValues ? hoverBobSpeed : 
        (demonData != null ? demonData.hoverBobSpeed : 1.5f);
    public float JumpForce => useInstanceValues ? jumpForce : 
        (demonData != null ? demonData.jumpForce : 10f);
    
    // Override CurrentMoveSpeed to handle null data gracefully
    protected new float CurrentMoveSpeed 
    {
        get 
        {
            if (data != null) 
                return data.moveSpeed;
            else if (demonData != null) 
                return demonData.moveSpeed;
            else 
                return 3f; // Safe fallback value
        }
    }
    
    // State tracking
    private Vector3 moveVelocity = Vector3.zero;
    private float targetHoverHeight;
    private float bobTime = 0f;
    private bool isFloating = false;

    // A* Pathfinding Fields
    private Pathfinding.Path currentAStarPath;
    private int currentAStarWaypointIndex = 0;
    private bool isFollowingAStarPath = false;
    [Header("A* Pathfinding")]
    [Tooltip("Distance to waypoint before considering it 'reached'")]
    [SerializeField] private float aStarWaypointReachedDistance = 1.0f;
    [Tooltip("Speed when following A* paths (chasing/patrolling)")]
    [SerializeField] private float aStarMovementSpeed = 4f;
    
    [Header("Flipping Control")]
    [Tooltip("Minimum time between direction flips to prevent flickering")]
    [SerializeField] private float flipCooldown = 0.3f;
    [Tooltip("Minimum horizontal movement required to trigger a flip")]
    [SerializeField] private float directionDeadzone = 0.3f;
    
    // Flipping control variables
    private float lastFlipTime = 0f;

    // References
    private DemonAI demonAI;
    private DemonData demonData;
    private DemonEnemy demonEnemy;
    
    // Additional properties
    public new bool IsFacingRight => isFacingRight;
    public bool IsFollowingAStarPath => isFollowingAStarPath;
    
    public new void Initialize(EnemyData enemyData)
    {
        base.Initialize(enemyData);
        
        // Get essential components
        demonAI = GetComponent<DemonAI>();
        if (demonAI == null)
        {
            Debug.LogWarning($"{gameObject.name}: No DemonAI component found! Some features may not work.");
        }
        
        // Cast to DemonData if possible
        if (enemyData is DemonData)
        {
            demonData = (DemonData)enemyData;
        }
        
        // Get reference to DemonEnemy component
        demonEnemy = GetComponent<DemonEnemy>();
        
        // Create ground check if needed
        SetupDemonGroundCheck();
        
        // Initialize flipping control
        lastFlipTime = 0f;
    }
    
    private void SetupDemonGroundCheck()
    {
        if (groundCheckTransform == null)
        {
            GameObject groundCheckObj = new GameObject("DemonGroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1.5f, 0); // Positioned lower for demons
            groundCheckTransform = groundCheckObj.transform;
            Debug.Log($"{gameObject.name}: Created demon ground check at {groundCheckObj.transform.localPosition}");
        }
    }
    
    private void Update()
    {
        // Update hover bob effect
        bobTime += Time.deltaTime * BobSpeed;
    }
    
    protected override void FixedUpdate()
    {
        // Essential null checks to prevent exceptions
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError($"{gameObject.name}: Missing Rigidbody2D component! Skipping FixedUpdate.");
                return;
            }
        }

        if (isFollowingAStarPath && currentAStarPath != null)
        {
            HandleAStarPathMovement();
            if (rb.gravityScale != 0) rb.gravityScale = 0; // Keep gravity off for A* flight/hover
            return; // A* movement takes precedence
        }
                
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
        
        // Call ground check only if we have the necessary components
        if (groundCheckTransform != null)
        {
            CheckDemonGrounded();
        }
        
        // Safely apply movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirection.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }
    
        Debug.DrawRay(transform.position, moveDirection * 1.5f, Color.blue);
    }
    
    // Custom ground check method for demons
    private void CheckDemonGrounded()
    {
        if (groundCheckTransform == null) return;
        
        // Method 1: Using the ground check transform with a circle overlap
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, demonGroundCheckRadius, groundLayer);
        
        // Method 2: Ray from the position
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheckTransform.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );
        
        // Debug visualization
        Debug.DrawRay(groundCheckTransform.position, Vector2.down * groundCheckDistance, 
                      hit.collider != null ? Color.green : Color.red);
        
        if (hit.collider != null)
        {
            surfaceY = hit.point.y;
        }
    }
    
    // Method to adjust hover height
    public void AdjustHoverHeight(float height)
    {
        targetHoverHeight = height;
    }
    
    // Handle floating movement
    public void StartFloating(Vector3 targetPos)
    {
        if (rb == null) return;
        isFloating = true;
        rb.gravityScale = 0;    // Disable gravity
        rb.linearVelocity = Vector2.zero; // Stop any existing movement
        Debug.Log($"{gameObject.name}: Started floating. Target: {targetPos}");
    }

    public void UpdateFloatingMovement(Vector3 targetPos3D, float speed, float avoidanceDistance, LayerMask obstacleLayer)
    {
        if (!isFloating || rb == null) return;

        Vector2 targetPos = targetPos3D;
        Vector2 currentPosition = transform.position;
        Vector2 directionToTarget = (targetPos - currentPosition).normalized;

        // Obstacle Avoidance using CircleCast feelers
        float demonRadius = 0.5f;
        Collider2D demonCol = GetComponent<Collider2D>();
        if (demonCol is CircleCollider2D circle) demonRadius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        else if (demonCol is BoxCollider2D box) demonRadius = Mathf.Max(box.size.x * transform.localScale.x, box.size.y * transform.localScale.y) * 0.5f;
        demonRadius = Mathf.Max(demonRadius, 0.1f);

        // Main feeler directly towards target
        RaycastHit2D hitCenter = Physics2D.CircleCast(currentPosition, demonRadius, directionToTarget, avoidanceDistance, obstacleLayer);

        if (hitCenter.collider != null && hitCenter.collider.gameObject != gameObject)
        {
            Debug.DrawLine(currentPosition, hitCenter.point, Color.yellow);
            Debug.Log($"{gameObject.name} Floating: Obstacle {hitCenter.collider.name} directly ahead. Dist: {hitCenter.distance}. Trying to steer.");

            Vector2 obstacleNormal = hitCenter.normal;
            Vector2 escapeDir1 = new Vector2(-obstacleNormal.y, obstacleNormal.x).normalized;
            Vector2 escapeDir2 = new Vector2(obstacleNormal.y, -obstacleNormal.x).normalized;

            float dot1 = Vector2.Dot(escapeDir1, directionToTarget);
            float dot2 = Vector2.Dot(escapeDir2, directionToTarget);

            Vector2 chosenEscapeDirection = (dot1 > dot2) ? escapeDir1 : escapeDir2;
            
            if (Mathf.Abs(Vector2.Dot(directionToTarget, obstacleNormal)) > 0.9f && targetPos.y > currentPosition.y){
                 chosenEscapeDirection = (chosenEscapeDirection + Vector2.up * 0.5f).normalized;
                 Debug.Log($"{gameObject.name} Floating: Head-on collision with {hitCenter.collider.name}, attempting upward escape.");
            }

            directionToTarget = (directionToTarget * 0.2f + chosenEscapeDirection * 0.8f).normalized;
            if (targetPos.y > currentPosition.y && directionToTarget.y < 0) {
                directionToTarget.y = Mathf.Abs(directionToTarget.y * 0.1f);
            }
        }
        else
        {
            Debug.DrawRay(currentPosition, directionToTarget * avoidanceDistance, Color.green);
        }

        // Apply force for smoother, more reactive movement in air
        Vector2 targetVelocity = directionToTarget * speed;
        Vector2 force = (targetVelocity - rb.linearVelocity) * (rb.mass * 10f);
        rb.AddForce(force);

        // Clamp velocity to prevent excessive speeds if using AddForce
        if (rb.linearVelocity.sqrMagnitude > speed * speed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    public void StopFloating()
    {
        if (rb == null) return;
        isFloating = false;
        ResetPhysicsToHover();
    }

    public override void Flip(bool faceRight)
    {
        // Update facing direction flag
        isFacingRight = faceRight;
        
        // Apply correct rotation based on sprite orientation
        float targetRotation = faceRight ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, targetRotation, 0);
    }
    
    public new void SetMoveDirection(Vector2 direction, bool isPatrolling)
    {
        base.SetMoveDirection(direction, isPatrolling);
    }
    
    private void OnDrawGizmos()
    {
        // Draw a line showing which way the demon is facing
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

    public void ResetPhysicsToHover() 
    {
        rb.gravityScale = 0; // Keep gravity off for hovering
        isFloating = false;
        
        // Ensure we're not following any old paths
        isFollowingAStarPath = false;
        currentAStarPath = null;
        currentAStarWaypointIndex = 0;
        
        Debug.Log($"{gameObject.name}: Physics reset to hover mode.");
    }

    // Method to be called by DemonAI to start following an A* path
    public void FollowAStarPath(Pathfinding.Path newPath)
    {
        if (newPath == null || newPath.error || newPath.vectorPath.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Attempted to follow an invalid or empty A* path.");
            StopAPath();
            return;
        }

        currentAStarPath = newPath;
        currentAStarWaypointIndex = 0;
        isFollowingAStarPath = true;
        isFloating = false;

        if (rb.gravityScale != 0) rb.gravityScale = 0; // Ensure gravity is off for A* controlled flight/hover
        Debug.Log($"{gameObject.name}: Following new A* path with {newPath.vectorPath.Count} waypoints.");
    }

    // Method to stop A* path following
    public void StopAPath()
    {
        isFollowingAStarPath = false;
        currentAStarPath = null;
        currentAStarWaypointIndex = 0;
        
        // Smoothly stop movement instead of abrupt stop
        if (rb != null)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.5f);
        }
        
        ResetPhysicsToHover(); 
        Debug.Log($"{gameObject.name}: Stopped A* path following.");
    }

    private void HandleAStarPathMovement()
    {
        if (currentAStarPath == null || currentAStarWaypointIndex >= currentAStarPath.vectorPath.Count)
        {
            if (isFollowingAStarPath)
            {
                Debug.Log($"{gameObject.name}: A* path finished or invalid.");
                isFollowingAStarPath = false;
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        // Get current target waypoint
        Vector2 targetWaypoint = currentAStarPath.vectorPath[currentAStarWaypointIndex];
        
        // Calculate direction to waypoint
        Vector2 directionToWaypoint = (targetWaypoint - (Vector2)transform.position).normalized;
        
        // Look ahead to next waypoint if available to smooth movement
        if (currentAStarWaypointIndex < currentAStarPath.vectorPath.Count - 1 && 
            Vector2.Distance(transform.position, targetWaypoint) < 3.0f)
        {
            Vector2 nextWaypoint = currentAStarPath.vectorPath[currentAStarWaypointIndex + 1];
            directionToWaypoint = Vector2.Lerp(directionToWaypoint, 
                                             (nextWaypoint - (Vector2)transform.position).normalized, 
                                             0.4f);
        }

        // Apply movement with better speed control
        float speed = aStarMovementSpeed;
        
        // Slow down when approaching final waypoint
        if (currentAStarWaypointIndex == currentAStarPath.vectorPath.Count - 1)
        {
            float distToFinal = Vector2.Distance(transform.position, targetWaypoint);
            if (distToFinal < 3.0f)
            {
                speed *= Mathf.Max(0.5f, distToFinal / 3.0f);
            }
        }
        
        // Apply velocity with some smoothing
        Vector2 targetVelocity = directionToWaypoint * speed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);

        // Improved flipping logic to prevent rapid flickering
        if (Mathf.Abs(directionToWaypoint.x) > directionDeadzone && Time.time > lastFlipTime + flipCooldown)
        {
            bool shouldFaceRight = directionToWaypoint.x > 0;
            if (isFacingRight != shouldFaceRight)
            {
                Flip(shouldFaceRight);
                lastFlipTime = Time.time;
            }
        }

        // Check if we've reached the current waypoint
        float distanceToWaypoint = Vector2.Distance(transform.position, targetWaypoint);
        if (distanceToWaypoint < aStarWaypointReachedDistance)
        {
            currentAStarWaypointIndex++;
            
            // If we've reached the end of the path
            if (currentAStarWaypointIndex >= currentAStarPath.vectorPath.Count)
            {
                isFollowingAStarPath = false;
                rb.linearVelocity = Vector2.zero;
                Debug.Log($"{gameObject.name}: Reached end of A* path.");
            }
        }
    }

    // Update StopMoving to properly work with A* pathfinding
    public override void StopMoving()
    {
        moveDirection = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        if (animator != null)
        {
            animator.SetWalking(false);
        }
    }

    // Public getter for DemonAI to check path following status
    public bool IsCurrentlyFollowingAStarPath()
    {
        return isFollowingAStarPath && currentAStarPath != null && 
               currentAStarWaypointIndex < currentAStarPath.vectorPath.Count;
    }
} 