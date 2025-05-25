using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;   // ← to migrate the old radius name
using System.Collections.Generic;
using Pathfinding; // Added for A*

public class GhostMovement : EnemyMovement
{
    [Header("Instance Override Settings")]
    [Tooltip("When enabled, uses the values set in this component rather than from the GhostData asset")]
    [SerializeField] private bool useInstanceValues = true;
    
    [Header("Ghost Movement Settings")]
    [Tooltip("How quickly the ghost reaches its target hover height (lower = faster adjustment)")]
    [SerializeField] private float hoverSmoothing = 0.2f;
    [Tooltip("How much the ghost bobs up and down while hovering")]
    [SerializeField] private float hoverBobAmplitude = 0.1f;
    [Tooltip("How fast the ghost bobs up and down")]
    [SerializeField] private float hoverBobSpeed = 1.5f;
    [Tooltip("How powerful the ghost's jumps are")]
    [SerializeField] private float jumpForce = 10f;
    
    [Header("Ground check")] // Added Header for clarity in Inspector
    [Tooltip("Transform used to check for ground beneath the ghost (auto-created if null)")]
    [SerializeField] private Transform groundCheckTransform;
    [Tooltip("Maximum distance to check downward for ground")]
    [SerializeField] private float groundCheckDistance = 10f;

    // ✅ renamed so we no longer clash with EnemyMovement.groundCheckRadius
    [FormerlySerializedAs("groundCheckRadius")]
    [Tooltip("Radius of the circle used to detect ground beneath the ghost")] // Added tooltip
    [SerializeField] private float ghostGroundCheckRadius = 0.2f;

    // Add surfaceY variable for ground tracking
    private float surfaceY = 0f;
    
    // Access properties that check for instance overrides
    public float HoverSmoothing => useInstanceValues ? hoverSmoothing : 
        (ghostData != null ? ghostData.hoverSmoothing : 0.2f);
    public float BobAmplitude => useInstanceValues ? hoverBobAmplitude : 
        (ghostData != null ? ghostData.hoverBobAmplitude : 0.1f);
    public float BobSpeed => useInstanceValues ? hoverBobSpeed : 
        (ghostData != null ? ghostData.hoverBobSpeed : 1.5f);
    public float JumpForce => useInstanceValues ? jumpForce : 
        (ghostData != null ? ghostData.jumpForce : 10f);
    
    // Override CurrentMoveSpeed to handle null data gracefully
    protected new float CurrentMoveSpeed 
    {
        get 
        {
            if (data != null) 
                return data.moveSpeed;
            else if (ghostData != null) 
                return ghostData.moveSpeed;
            else 
                return 3f; // Safe fallback value
        }
    }
    
    // State tracking
    private Vector3 moveVelocity = Vector3.zero;
    private float targetHoverHeight;
    private float bobTime = 0f;
    private bool isFloating = false; // New flag for floating state - used by old float logic
    // private Vector3 floatTargetPosition; // Target position for floating - used by old float logic

    // Fields for spline following
    private List<Vector2> _spline = new List<Vector2>();
    private int _splineIdx = 0;
    private float _currentSplineSpeed = 3f; // Default speed for spline following
    public bool IsFollowingSpline => _spline != null && _splineIdx < _spline.Count;

    private bool _isReachingInitialSplineWaypoint = false;
    private Vector2 _initialSplineWaypointTarget;
    private const float INITIAL_WAYPOINT_APPROACH_SPEED_FACTOR = 0.75f; // Move a bit slower to initial point

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
    private GhostAI ghostAI; // Reference to GhostAI for triggering jumps
    private GhostData ghostData;
    private GhostEnemy ghostEnemy;
    
    // Additional properties
    public new bool IsFacingRight => isFacingRight;
    public bool IsFollowingAStarPath => isFollowingAStarPath;
    
    public new void Initialize(EnemyData enemyData)
    {
        base.Initialize(enemyData);
        
        // Get essential components
        ghostAI = GetComponent<GhostAI>();
        if (ghostAI == null)
        {
            Debug.LogWarning($"{gameObject.name}: No GhostAI component found! Some features may not work.");
        }
        
        // Cast to GhostData if possible
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
        }
        
        // Get reference to GhostEnemy component
        ghostEnemy = GetComponent<GhostEnemy>();
        
        // Create ground check if needed
        SetupGhostGroundCheck();
        
        // Initialize flipping control
        lastFlipTime = 0f;
    }
    
    // Renamed to avoid hiding the parent method
    private void SetupGhostGroundCheck()
    {
        if (groundCheckTransform == null)
        {
            GameObject groundCheckObj = new GameObject("GhostGroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1.5f, 0); // Positioned lower for ghosts
            groundCheckTransform = groundCheckObj.transform;
            Debug.Log($"{gameObject.name}: Created ghost ground check at {groundCheckObj.transform.localPosition}");
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

        // Old Spline logic (to be fully removed, kept temporarily for StopFollowingSpline)
        if (_isReachingInitialSplineWaypoint)
        {
            if (rb.gravityScale != 0) rb.gravityScale = 0;
            return;
        }
        
        if (IsFollowingSpline) 
        {
            if (rb.gravityScale != 0) rb.gravityScale = 0; 
            return; 
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
        CheckGhostGrounded();
        }
        
        // Safely apply movement
        if (rb != null)
        {
        rb.linearVelocity = new Vector2(moveDirection.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }
    
        Debug.DrawRay(transform.position, moveDirection * 1.5f, Color.blue);
    }
    
    // Custom ground check method for ghosts
    private void CheckGhostGrounded()
    {
        if (groundCheckTransform == null) return;
        
        // Method 1: Using the ground check transform with a circle overlap
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, ghostGroundCheckRadius, groundLayer); // Updated to ghostGroundCheckRadius
        
        // Method 2: Ray from the position (what was being done before in CheckGroundBeneath)
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
            // The rest of the ground check logic will be handled by CheckGroundBeneath in GhostAI
        }
    }
    
    // Method to adjust hover height
    public void AdjustHoverHeight(float height)
    {
        targetHoverHeight = height;
    }
    
    // Enhanced Jump method with better physics
    // public void Jump(float heightNeeded) // Method Removed - Replaced by JumpToTarget for A*
    // {
    //     if (rb == null)
    //         return;
    //     float scaledForce = Mathf.Max(JumpForce, JumpForce * (heightNeeded / 2f));
    //     rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
    //     rb.AddForce(Vector2.up * scaledForce, ForceMode2D.Impulse);
    //     float horizontalBoost = IsFacingRight ? 3f : -3f;
    //     rb.AddForce(Vector2.right * horizontalBoost, ForceMode2D.Impulse);
    // }
    
    // Handle climb start
    // Enhanced climbing system
    // public void StartClimbing() // Removed
    // {
    //     if (rb == null)
    //         return;
    //             
    //     // isClimbing = true; // isClimbing is removed
    //     
    //     rb.linearVelocity = Vector2.zero;
    //     rb.gravityScale = 0;
    // }
    
    // Handle climbing movement
    // Climb method - controlled vertical movement
    // public void Climb() // Removed
    // {
    //     if (!isClimbing || rb == null) // isClimbing is removed
    //         return;
    //             
    //     rb.linearVelocity = new Vector2(0, ClimbSpeed); // ClimbSpeed will be removed
    // }
    
    // Finish climbing with smooth transition
    // public void FinishClimbing() // Removed
    // {
    //     if (rb == null)
    //         return;
    //             
    //     // isClimbing = false; // isClimbing is removed
    //     
    //     rb.gravityScale = 1;
    //     
    //     float pushDirection = IsFacingRight ? 1 : -1;
    //     rb.linearVelocity = new Vector2(pushDirection * 2, 1);
    // }
    // Add this method to override the base class Flip behavior

    // --- Floating Methods ---
    public void StartFloating(Vector3 targetPos)
    {
        if (rb == null) return;
        isFloating = true;
        rb.gravityScale = 0;    // Disable gravity
        rb.linearVelocity = Vector2.zero; // Stop any existing movement
        // this.floatTargetPosition = targetPos;
        Debug.Log($"{gameObject.name}: Started floating. Target: {targetPos}");
    }

    public void UpdateFloatingMovement(Vector3 targetPos3D, float speed, float avoidanceDistance, LayerMask obstacleLayer)
    {
        if (!isFloating || rb == null) return; // isFloating will be false due to A*

        // this.floatTargetPosition = targetPos; // Store the 3D target if needed for other purposes
        Vector2 targetPos = targetPos3D; // Convert to Vector2 for 2D logic
        Vector2 currentPosition = transform.position; // transform.position is Vector3, implicitly convertible to Vector2
        Vector2 directionToTarget = (targetPos - currentPosition).normalized;

        // Obstacle Avoidance using CircleCast feelers
        float ghostRadius = 0.5f; // Approximate radius, get from collider if possible
        Collider2D ghostCol = GetComponent<Collider2D>();
        if (ghostCol is CircleCollider2D circle) ghostRadius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        else if (ghostCol is BoxCollider2D box) ghostRadius = Mathf.Max(box.size.x * transform.localScale.x, box.size.y * transform.localScale.y) * 0.5f;
        ghostRadius = Mathf.Max(ghostRadius, 0.1f);

        // Main feeler directly towards target
        RaycastHit2D hitCenter = Physics2D.CircleCast(currentPosition, ghostRadius, directionToTarget, avoidanceDistance, obstacleLayer);

        if (hitCenter.collider != null && hitCenter.collider.gameObject != gameObject) // Obstacle directly ahead
        {
            Debug.DrawLine(currentPosition, hitCenter.point, Color.yellow);
            Debug.Log($"{gameObject.name} Floating: Obstacle {hitCenter.collider.name} directly ahead. Dist: {hitCenter.distance}. Trying to steer.");

            // Try to find an escape direction: perpendicular to obstacle normal, or try side feelers
            Vector2 obstacleNormal = hitCenter.normal;
            Vector2 escapeDir1 = new Vector2(-obstacleNormal.y, obstacleNormal.x).normalized; // Perpendicular 1
            Vector2 escapeDir2 = new Vector2(obstacleNormal.y, -obstacleNormal.x).normalized; // Perpendicular 2

            // Check which escape direction is more aligned with the general direction to target
            float dot1 = Vector2.Dot(escapeDir1, directionToTarget);
            float dot2 = Vector2.Dot(escapeDir2, directionToTarget);

            Vector2 chosenEscapeDirection = (dot1 > dot2) ? escapeDir1 : escapeDir2;
            
            // If directly hitting a wall head-on, try to force more upward movement if player is higher
            if (Mathf.Abs(Vector2.Dot(directionToTarget, obstacleNormal)) > 0.9f && targetPos.y > currentPosition.y){
                 chosenEscapeDirection = (chosenEscapeDirection + Vector2.up * 0.5f).normalized;
                 Debug.Log($"{gameObject.name} Floating: Head-on collision with {hitCenter.collider.name}, attempting upward escape.");
            }

            directionToTarget = (directionToTarget * 0.2f + chosenEscapeDirection * 0.8f).normalized; // Blend original direction with escape, prioritizing escape
            // Ensure that the blended direction doesn't accidentally point downwards if target is above
            if (targetPos.y > currentPosition.y && directionToTarget.y < 0) {
                directionToTarget.y = Mathf.Abs(directionToTarget.y * 0.1f); // Greatly reduce downward component, or set to 0
            }
        }
        else
        {
            Debug.DrawRay(currentPosition, directionToTarget * avoidanceDistance, Color.green);
        }

        // rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, directionToTarget * speed, speed * Time.deltaTime * 5f); // Original
        // Apply force for smoother, more reactive movement in air
        Vector2 targetVelocity = directionToTarget * speed;
        Vector2 force = (targetVelocity - rb.linearVelocity) * (rb.mass * 10f); // Adjust multiplier for responsiveness
        rb.AddForce(force);

        // Clamp velocity to prevent excessive speeds if using AddForce
        if (rb.linearVelocity.sqrMagnitude > speed * speed)
        {rb.linearVelocity = rb.linearVelocity.normalized * speed;}
    }

    public void StopFloating()
    {
        if (rb == null) return;
        isFloating = false;
        // rb.gravityScale = 1; // Gravity is restored when spline following ends or if not following spline
        // If spline following was active, its end should handle gravity.
        // If old floating was active, this would be its place to restore gravity.
        // For now, ensure ResetPhysics or spline completion handles gravity.
        // Debug.Log($"{gameObject.name}: Stopped floating (old method).");
        ResetPhysicsToHover(); // Ensure physics are reset if old floating is abruptly stopped
    }

    // public void JumpToTarget(float heightNeeded, Vector2 target) // Method Removed - Ghost will fly along A* path
    // {
    //     if (rb == null)
    //         return;
    //     float distanceX = target.x - transform.position.x;
    //     float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
    //     float jumpHeight = heightNeeded + 1.0f; 
    //     float verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
    //     float timeToApex = (gravity > 0.001f && verticalVelocity > 0) ? verticalVelocity / gravity : 0.5f; 
    //     float horizontalVelocity = (timeToApex * 2 > 0.001f) ? distanceX / (timeToApex * 2) : 0;
    //     horizontalVelocity = Mathf.Clamp(horizontalVelocity, -8f, 8f);
    //     rb.linearVelocity = Vector2.zero;
    //     rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
    //     Debug.Log($"Ghost jumping with velocity ({horizontalVelocity:F1}, {verticalVelocity:F1}) " +
    //             $"to reach target at ({target.x:F1}, {target.y:F1})");
    // }   
    
    public override void Flip(bool faceRight)
    {
        // Update facing direction flag
        isFacingRight = faceRight;
        
        // Debug to verify our direction understanding
        // Comment out this line to reduce spam:
        // Debug.Log($"Ghost flipping to face: {(faceRight ? "Right" : "Left")}");
        
        // Apply correct rotation based on sprite orientation
        float targetRotation = faceRight ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, targetRotation, 0);
    }
    // Override movement to account for climbing state
    public new void SetMoveDirection(Vector2 direction, bool isPatrolling)
    {
        // if (isClimbing) // Removed
            // return;
            
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

    // --- Spline Following Methods --- (These will be removed or repurposed for A*)
    public void BeginSpline(List<Vector2> newSpline, float speed) // To be removed
    {
        if (newSpline == null || newSpline.Count == 0)
        {
            // Debug.LogWarning($"{gameObject.name}: BeginSpline called with null or empty spline.");
            _spline.Clear();
            _splineIdx = 0;
            _isReachingInitialSplineWaypoint = false;
            // ResetPhysicsToHover(); 
            return;
        }
        _spline = new List<Vector2>(newSpline); 
        _splineIdx = 0;
        _currentSplineSpeed = speed;
        // rb.gravityScale = 0; 
        // isFloating = true; // Old floating flag, A* will use isFollowingAStarPath

        int practicalStartIndex = 0;
        if (_spline.Count > 1 && Vector2.Distance(rb.position, _spline[0]) < 0.4f) {
            practicalStartIndex = 1;
             // Debug.Log($"{gameObject.name}: BeginSpline - Ghost already close to first spline point ({_spline[0]}). Starting actual spline follow at index 1.");
        }
        
        if (practicalStartIndex < _spline.Count) {
            _initialSplineWaypointTarget = _spline[practicalStartIndex];
            _isReachingInitialSplineWaypoint = true;
            _splineIdx = practicalStartIndex; 
            // Debug.Log($"{gameObject.name}: Beginning navigation to initial spline waypoint: {_initialSplineWaypointTarget}. Full spline has {_spline.Count} points. Speed: {speed}");
        } else {
            // Debug.LogWarning($"{gameObject.name}: Spline too short or already at destination after skipping. Aborting spline.");
            _isReachingInitialSplineWaypoint = false;
            _spline.Clear();
            // ResetPhysicsToHover();
        }
    }

    private void MoveToInitialWaypoint() // To be removed
    {
        // if (rb == null) {
        //     _isReachingInitialSplineWaypoint = false; return;
        // }
        // float currentGhostRadius = GetRadius();
        // Vector2 actualTargetForPivot = _initialSplineWaypointTarget - Vector2.up * currentGhostRadius;

        // float distanceToActualTarget = Vector2.Distance(rb.position, actualTargetForPivot);
        // float approachSpeed = _currentSplineSpeed * INITIAL_WAYPOINT_APPROACH_SPEED_FACTOR;

        // if (distanceToActualTarget < 0.3f) 
        // {
        //     // Debug.Log($"{gameObject.name}: Reached initial spline waypoint (pivot adjusted) target: {actualTargetForPivot} for spline point: {_initialSplineWaypointTarget}. Switching to full FollowSpline at index {_splineIdx}.");
        //     _isReachingInitialSplineWaypoint = false;
        //     if (!IsFollowingSpline) { 
        //         // ResetPhysicsToHover();
        //     }
        //     return;
        // }

        // Vector2 direction = (actualTargetForPivot - rb.position).normalized;
        
        // RaycastHit2D hit = Physics2D.CircleCast(rb.position, currentGhostRadius, direction, 1.0f, groundLayer);

        // if (hit.collider != null && hit.collider.gameObject != gameObject)
        // {
        //     // Debug.LogWarning($"{gameObject.name}: MoveToInitialWaypoint - Obstacle {hit.collider.name} detected towards {actualTargetForPivot}. Trying to nudge up/sideways.");
        //     Vector2 upwardNudge = Vector2.up * 0.7f; 
        //     Vector2 sideNudge = new Vector2(-hit.normal.y, hit.normal.x) * 0.3f; 
        //     direction = (upwardNudge + sideNudge).normalized;
        //     if (Vector2.Dot(direction, (actualTargetForPivot - rb.position).normalized) < 0.3f && actualTargetForPivot.y > rb.position.y){
        //          direction = (Vector2.up + (actualTargetForPivot - rb.position).normalized * 0.5f).normalized; 
        //     }
        // }

        // rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, direction * approachSpeed, approachSpeed * Time.deltaTime * 10f); 

        // if (Mathf.Abs(direction.x) > 0.1f) Flip(direction.x > 0);
        _isReachingInitialSplineWaypoint = false; // Ensure this is turned off as logic is removed
    }

    private void FollowSpline() // To be removed
    {
        // if (!IsFollowingSpline || rb == null) {
        //     if (IsFollowingSpline) // ResetPhysicsToHover(); 
        //     return;
        // }

        // const float arriveDist = 0.45f; 
        // const float feelerDist = 1.2f; 
        // const float steerWeight = 0.5f; 

        // Vector2 pos = rb.position; 
        // Vector2 splineTargetPoint = _spline[_splineIdx]; 
        // float currentGhostRadius = GetRadius();
        // Vector2 actualTargetForPivot = splineTargetPoint - Vector2.up * currentGhostRadius;

        // if (Vector2.Distance(pos, actualTargetForPivot) < arriveDist) { 
        //     _splineIdx++;
        //     if (!IsFollowingSpline)
        //     {
        //         // Debug.Log($"{gameObject.name}: Spline finished.");
        //         // ResetPhysicsToHover();
        //         // isFloating = false; // old flag 
        //         return;
        //     }
        //     splineTargetPoint = _spline[_splineIdx];
        //     actualTargetForPivot = splineTargetPoint - Vector2.up * currentGhostRadius;
        // }

        // Vector2 desired = (actualTargetForPivot - pos).normalized;
        // Vector2 avoid = Vector2.zero;

        // Vector2[] feelerDirections = { desired, Rotate(desired, 30f * Mathf.Deg2Rad), Rotate(desired, -30f * Mathf.Deg2Rad) };
        // int obstaclesHit = 0;
        // RaycastHit2D firstHit = new RaycastHit2D(); 

        // foreach (Vector2 d in feelerDirections)
        // {
        //     Vector2 castOrigin = pos + Vector2.up * currentGhostRadius;
        //     RaycastHit2D hit = Physics2D.CircleCast(castOrigin, currentGhostRadius, d, feelerDist, groundLayer);
        //     if (hit.collider != null && hit.collider.gameObject != gameObject)
        //     {
        //         if(obstaclesHit == 0) firstHit = hit; 
        //         float weight = 1f / Mathf.Max(0.1f, hit.distance); 
        //         avoid += (-hit.normal) * weight; 
        //         Debug.DrawLine(castOrigin, castOrigin + d * hit.distance, Color.red, 0.1f); 
        //         obstaclesHit++;
        //     }
        //     else
        //     {
        //         Debug.DrawRay(castOrigin, d * feelerDist, Color.green, 0.1f); 
        //     }
        // }

        // Vector2 finalSteerDirection;
        // if (obstaclesHit > 0)
        // {
        //     avoid.Normalize(); 
        //     if (firstHit.collider != null && Vector2.Dot(avoid, Vector2.up) < 0.1f && actualTargetForPivot.y > pos.y) 
        //     {
        //         if (Vector2.Dot(firstHit.normal, Vector2.down) > 0.7f) 
        //         {
        //             avoid = (avoid + Vector2.up).normalized; 
        //             // Debug.Log($"{gameObject.name}: FollowSpline - Nudging UP to avoid '{firstHit.collider.name}'");
        //         }
        //     }
        //     finalSteerDirection = (desired * (1f - steerWeight) + avoid * steerWeight).normalized;
        // }
        // else
        // {
        //     finalSteerDirection = desired;
        // }
        
        // rb.linearVelocity = finalSteerDirection * _currentSplineSpeed; 

        // if (Mathf.Abs(finalSteerDirection.x) > 0.1f) 
        //     Flip(finalSteerDirection.x > 0);
        if (IsFollowingSpline) _splineIdx = _spline.Count; // Effectively stop old spline logic
    }

    public void ResetPhysicsToHover() 
    {
        rb.gravityScale = 0; // Keep gravity off for hovering
        isFloating = false; // Important to reset this if old float logic might run
         _spline.Clear();
        _splineIdx = 0;
        _isReachingInitialSplineWaypoint = false;
        
        // Ensure we're not following any old paths
        isFollowingAStarPath = false;
        currentAStarPath = null;
        currentAStarWaypointIndex = 0;
        
        Debug.Log($"{gameObject.name}: Physics reset to hover mode.");
    }

    // Method to be called by GhostAI to start following an A* path
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
        isFloating = false; // Ensure old floating logic is off
        _isReachingInitialSplineWaypoint = false; // Ensure old spline init logic is off
        _spline.Clear(); // Clear old spline data

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
    
    // Method to specifically stop the old spline following (called by GhostAI to resolve linter error)
    public void StopFollowingSpline()
    {
        _spline.Clear();
        _splineIdx = 0;
        _isReachingInitialSplineWaypoint = false;
        // No need to reset physics here as A* or other states will manage it.
        // Debug.Log($"{gameObject.name}: Old spline following explicitly stopped.");
    }

    private void HandleAStarPathMovement()
    {
        if (currentAStarPath == null || currentAStarWaypointIndex >= currentAStarPath.vectorPath.Count)
        {
            // Path is invalid or finished
            if (isFollowingAStarPath) // Only log and reset if we thought we were following
            {
                Debug.Log($"{gameObject.name}: A* path finished or invalid.");
                isFollowingAStarPath = false;
                rb.linearVelocity = Vector2.zero; // Stop movement when path ends
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

    // Public getter for GhostAI to check path following status
    public bool IsCurrentlyFollowingAStarPath()
    {
        return isFollowingAStarPath && currentAStarPath != null && 
               currentAStarWaypointIndex < currentAStarPath.vectorPath.Count;
    }

    // Helper methods for spline following
    private float GetRadius() // This might still be useful for general collision avoidance or gizmos
    {
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null) 
        {
            return circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null) {
             return Mathf.Max(capsule.size.x, capsule.size.y) * 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) {
            return Mathf.Max(box.size.x, box.size.y) * 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        return 0.5f; // Default radius if no specific 2D collider found
    }

    private Vector2 Rotate(Vector2 v, float rad)
    {
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}