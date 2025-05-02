using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostAI : EnemyAI
{
    // Add these fields to your GhostAI class
    [Header("Advanced Platform Navigation")]
    [SerializeField] private float platformDwellTime = 0.5f;       // How long player must be on platform before ghost jumps
    [SerializeField] private float playerTrackingInterval = 0.2f;  // How often to record player position
    [SerializeField] private int positionHistorySize = 10;         // How many positions to remember
    [SerializeField] private float verticalJumpThreshold = 1.0f;   // Min height difference to consider jumping
    [SerializeField] private float platformDetectionDistance = 5f; // How far horizontally the ghost can still detect platforms
    [SerializeField] private bool showJumpPrediction = true;       // Show jump arc visualization

    // State tracking for platform detection
    private Vector3 lastRecordedPlayerPosition;
    private float playerOnPlatformTimer = 0f;
    private bool playerOnHigherPlatform = false;
    private float detectedPlatformY = 0f;
    private Vector2 platformJumpTarget;
    private List<Vector3> playerPositionHistory = new List<Vector3>();
    private float lastPositionRecordTime = 0f;
    [Header("Instance Override Settings")]
    [SerializeField] private bool ghostUseInstanceValues = true; // Renamed
    [Tooltip("Override the detection range from GhostData")]
    [SerializeField] private float ghostDetectionRange = 15.38f; // Renamed
    [Tooltip("Override the attack range from GhostData")]
    [SerializeField] private float ghostAttackRange = 13.96f; // Renamed
    [SerializeField] private float wallCheckDistance = 0.7f;
    [SerializeField] private float ledgeCheckDistance = 1.5f;
    [SerializeField] private float maxJumpHeight = 3f;
    [SerializeField] private float chaseMemoryDuration = 3f;
    
    [Header("Ghost Navigation")]
    [SerializeField] private Transform ledgeCheckPoint;
    [SerializeField] private LayerMask climbableLayer;
    
    [Header("Ghost State Visualization")]
    [SerializeField] private bool showDebugRays = true;
    
    // Ghost-specific states
    public enum GhostState { None, Jumping, Climbing, SpecialAttack }
    [SerializeField] private GhostState currentGhostState = GhostState.None;
    [SerializeField] private LayerMask groundLayerMask;
    
    // Access properties that use instance values if enabled
    public override float DetectionRange => ghostUseInstanceValues ? ghostDetectionRange : 
        (ghostData != null ? ghostData.detectionRange : 7f);
    public override float AttackRange => ghostUseInstanceValues ? ghostAttackRange : 
        (ghostData != null ? ghostData.attackRange : 1.5f);
    public float WallCheckDistance => useInstanceValues ? wallCheckDistance : 
        (ghostData != null ? ghostData.wallCheckDistance : 0.7f);
    public float LedgeCheckDistance => useInstanceValues ? ledgeCheckDistance : 
        (ghostData != null ? ghostData.ledgeCheckDistance : 1.5f);
    public float MaxJumpHeight => useInstanceValues ? maxJumpHeight : 
        (ghostData != null ? ghostData.maxJumpHeight : 3f);
    public float ChaseMemoryDuration => useInstanceValues ? chaseMemoryDuration : 
        (ghostData != null ? ghostData.chaseMemoryDuration : 3f);
    
    // Cache references
    private GhostMovement ghostMovement;
    [SerializeField] private new Animator animator;
    private GhostAnimator ghostAnimator;
    private GhostData ghostData;
    private float lastClimbTime = 0f;
    private float lastJumpTime = 0f;
    private float damageStateEnteredTime = 0;
    private float surfaceY = 0f;
    private bool isGrounded = false;
    private bool isClimbing = false;
    private bool isJumping = false;
    
    // State flags used for decisions
    private bool wallAhead = false;
    private bool ledgeClear = false;
    private float playerHeightDifference = 0f;

    public new void Initialize(EnemyData enemyData)
    {
        if (groundLayerMask.value == 0)
        {
            // Assign default ground layer if not set
            groundLayerMask = LayerMask.GetMask("Ground");
            Debug.Log($"{gameObject.name}: Auto-assigned Ground layer mask");
        }
        
        base.Initialize(enemyData);
        
        // Get ghost components
        ghostMovement = GetComponent<GhostMovement>();
        animator = GetComponent<Animator>();

        // Get the GhostAnimator instead of regular EnemyAnimator
        ghostAnimator = GetComponent<GhostAnimator>();
        if (ghostAnimator == null)
        {
            Debug.LogWarning($"{gameObject.name}: GhostAnimator component not found!");
        }
        
        // Cast to GhostData if possible
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Not provided with GhostData! Ghost-specific features disabled.");
        }
        
        // Set up ledge check if not already assigned
        if (ledgeCheckPoint == null)
        {
            GameObject ledgeCheck = new GameObject("LedgeCheck");
            ledgeCheck.transform.parent = transform;
            ledgeCheck.transform.localPosition = new Vector3(0.5f, ghostData?.maxJumpHeight ?? 2f, 0);
            ledgeCheckPoint = ledgeCheck.transform;
            Debug.Log($"{gameObject.name}: Created ledge check at {ledgeCheck.transform.localPosition}");
        }
        
        // Check if Animator has appropriate triggers
        if (animator != null)
        {
            Debug.Log("Verifying animator parameters for Ghost:");
            string[] requiredTriggers = { "MoveTrigger", "JumpTrigger", "ClimbTrigger", "AttackTrigger", "StunedTrigger", "DeathTrigger", "SpecialATrigger" };
            
            foreach (string trigger in requiredTriggers)
            {
                bool hasParameter = false;
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == trigger)
                    {
                        hasParameter = true;
                        break;
                    }
                }
                Debug.Log($"- {trigger}: {(hasParameter ? "Found" : "MISSING")}");
            }
        }
    }

    protected override void Update()
    {
        // Skip base update to use our custom implementation
        if (currentState == State.Dying)
            return;
            
        // Check if we're currently in a special ghost state
        if (isClimbing || isJumping)
        {
            HandleSpecialStates();
            return;
        }
        
        // Check ground beneath for proper hovering
        CheckGroundBeneath();
            
        // Essential helper values for decisions
        CheckEnvironmentConditions();
            
        // Main state machine updates
        bool canSeePlayer = CanSeePlayer();
        bool inAttackRange = IsInAttackRange();
        
        
        // Handle special trigger - damage
        if (currentState == State.TakingDamage){
        // Add emergency timeout check here
        CheckDamageStateTimeout();
        return;
        }

        // Attack opportunity check
        if (currentState != State.Attacking && 
            player != null && canSeePlayer && inAttackRange && 
            attack != null && attack.CanAttack)
        {
            ChangeState(State.Attacking);
            FireAnimationTrigger("AttackTrigger");
            return;
        }

        // NEW: Check for platform jump opportunity when in chase state
        if (currentState == State.Chasing && 
            player != null && !canSeePlayer && 
            Time.time - lastJumpTime > 3f && 
            Time.time - lastClimbTime > 3f)
        {
            // Try platform jump if player is detected on higher platform
            if (playerOnHigherPlatform)
            {
                AttemptPlatformJump();
                return;
            }
        }
        
        // Vertical navigation check (while not attacking)
        if ((currentState == State.Chasing || currentState == State.Patrolling || currentState == State.Idle) &&
            player != null && Time.time - lastClimbTime > 1f && Time.time - lastJumpTime > 1f)
        {
            // If player is above and within jump height and no wall
            bool canJump = playerHeightDifference > 0.5f && 
                           playerHeightDifference <= (ghostData?.maxJumpHeight ?? 3f) && 
                           !wallAhead && isGrounded;
                           
            // If player is too high to jump and there's a wall
            bool mustClimb = playerHeightDifference > (ghostData?.maxJumpHeight ?? 3f) && 
                            wallAhead;
            
            if (canJump)
            {
                Debug.Log($"{gameObject.name}: Jumping to reach player at height +{playerHeightDifference}");
                StartJumping();
                return;
            }
            else if (mustClimb)
            {
                Debug.Log($"{gameObject.name}: Climbing to reach player at height +{playerHeightDifference}");
                StartClimbing();
                return;
            }
        }
        
        // Player detection and chase memory
        if (player != null && canSeePlayer && currentState != State.Attacking && 
            currentState != State.Jumping && currentState != State.Climbing)
        {
            if (currentState != State.Chasing)
            {
                ChangeState(State.Chasing);
                FireAnimationTrigger("MoveTrigger");
            }
        }
        else if (currentState == State.Chasing && 
                 Time.time - lastPlayerDetectedTime > (ghostData?.chaseMemoryDuration ?? 3f))
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                ChangeState(State.Patrolling);
                FireAnimationTrigger("MoveTrigger");
            }
            else
            {
                ChangeState(State.Idle);
                if (animator != null)
                    animator.ResetTrigger("MoveTrigger");
            }
        }
        
        // Normal state handling
        HandleCurrentState();
    }

    // Move this outside the Update method to be a class-level method
    private void CheckDamageStateTimeout()
    {
        if (currentState == State.TakingDamage)
        {
            // First time entering damage state
            if (damageStateEnteredTime == 0)
                damageStateEnteredTime = Time.time;
            
            // If stuck in damage state for more than 2 seconds, force reset
            if (Time.time - damageStateEnteredTime > 2f)
            {
                Debug.LogWarning($"{gameObject.name}: Stuck in damage state for too long! Forcing reset.");
                damageStateEnteredTime = 0;
                
                // Force change to an active state
                if (player != null && CanSeePlayer())
                {
                    ChangeState(State.Chasing);
                    FireAnimationTrigger("MoveTrigger");
                }
                else if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    ChangeState(State.Patrolling);
                    FireAnimationTrigger("MoveTrigger");
                }
                else
                {
                    ChangeState(State.Idle);
                }
            }
        }
        else
        {
            damageStateEnteredTime = 0; // Reset timer when not in damage state
        }
    }

    private void TrackPlayerPosition()
    {
        if (player == null)
            return;

        // Record player positions at set intervals
        if (Time.time > lastPositionRecordTime + playerTrackingInterval)
        {
            lastPositionRecordTime = Time.time;
            
            // Add current position
            playerPositionHistory.Add(player.position);
            
            // Limit history size
            if (playerPositionHistory.Count > positionHistorySize)
                playerPositionHistory.RemoveAt(0);
                
            // Calculate if player is consistently on a higher platform
            DetectPlayerOnPlatform();
        }
    }

    private void DetectPlayerOnPlatform()
    {
        if (player == null || playerPositionHistory.Count < 3)
            return;

        // Check horizontal distance - don't attempt platform jumps if player is too far
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        if (horizontalDistance > platformDetectionDistance)
        {
            playerOnPlatformTimer = 0f;
            playerOnHigherPlatform = false;
            return;
        }
        
        // Calculate average height of last few positions
        float avgHeight = 0f;
        int recentPositions = Mathf.Min(3, playerPositionHistory.Count);
        for (int i = playerPositionHistory.Count - recentPositions; i < playerPositionHistory.Count; i++)
        {
            avgHeight += playerPositionHistory[i].y;
        }
        avgHeight /= recentPositions;
        
        // Get height difference between ghost and player
        float heightDiff = avgHeight - transform.position.y;
        
        // Check if player is above ghost and height is stable
        if (heightDiff > verticalJumpThreshold)
        {
            bool isHeightConsistent = true;
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            
            // Check if recent positions are within a small vertical range
            for (int i = playerPositionHistory.Count - recentPositions; i < playerPositionHistory.Count; i++)
            {
                minHeight = Mathf.Min(minHeight, playerPositionHistory[i].y);
                maxHeight = Mathf.Max(maxHeight, playerPositionHistory[i].y);
            }
            
            // If height variation is small, consider player on platform
            isHeightConsistent = (maxHeight - minHeight) < 0.3f;
            
            // Check if player is actually grounded (not just jumping)
            bool playerLikelyGrounded = IsPlayerLikelyGrounded();
            
            if (isHeightConsistent && playerLikelyGrounded)
            {
                // Player is consistently on a higher platform
                playerOnPlatformTimer += playerTrackingInterval;
                
                // After timer exceeds threshold, confirm player on platform
                if (playerOnPlatformTimer >= platformDwellTime && !playerOnHigherPlatform)
                {
                    playerOnHigherPlatform = true;
                    detectedPlatformY = avgHeight;
                    platformJumpTarget = CalculateJumpTarget();
                    
                    Debug.Log($"{gameObject.name}: Detected player on higher platform! Height: {heightDiff:F1}, " +
                            $"Player Y: {avgHeight:F1}, Target: {platformJumpTarget}");
                }
            }
            else
            {
                // Reset timer if height is inconsistent
                playerOnPlatformTimer = 0f;
            }
        }
        else
        {
            // Player not above ghost or not high enough
            playerOnPlatformTimer = 0f;
            playerOnHigherPlatform = false;
        }
    }

    private bool IsPlayerLikelyGrounded()
    {
        if (playerPositionHistory.Count < 2)
            return false;
            
        Vector3 currentPos = playerPositionHistory[playerPositionHistory.Count - 1];
        Vector3 prevPos = playerPositionHistory[playerPositionHistory.Count - 2];
        
        // If vertical movement is minimal and horizontal movement exists, likely grounded
        float verticalChange = Mathf.Abs(currentPos.y - prevPos.y);
        float horizontalChange = Mathf.Abs(currentPos.x - prevPos.x);
        
        // Logic: If player is moving horizontally but not much vertically
        // they're probably on a platform, not in middle of a jump
        bool likelyGrounded = verticalChange < 0.1f && horizontalChange > 0.05f;
        
        // Also check if player has been at consistent height for a while
        if (playerPositionHistory.Count >= 3)
        {
            Vector3 olderPos = playerPositionHistory[playerPositionHistory.Count - 3];
            float totalVertChange = Mathf.Abs(currentPos.y - olderPos.y);
            if (totalVertChange < 0.15f)
                likelyGrounded = true;
        }
        
        // Optional: Cast ray down from player to check for ground
        if (Physics2D.Raycast(player.position, Vector2.down, 0.3f, groundLayerMask))
            likelyGrounded = true;

        return likelyGrounded;
    }

    private Vector2 CalculateJumpTarget()
    {
        if (player == null)
            return transform.position;
            
        // Start with player position
        Vector2 target = player.position;
        
        // Adjust X position based on platform and approach direction
        float approachDir = Mathf.Sign(player.position.x - transform.position.x);
        
        // Cast rays to find edge of platform
        Vector2 rayStart = new Vector2(player.position.x - (approachDir * 2), player.position.y - 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 3f, groundLayerMask);
        
        if (hit.collider != null)
        {
            // Found platform edge, aim for slightly past it
            target = new Vector2(hit.point.x + (approachDir * 0.5f), detectedPlatformY);
        }
        else
        {
            // Fallback if edge not found
            target = new Vector2(player.position.x - (approachDir * 1.0f), detectedPlatformY);
        }
        
        return target;
    }    

    private bool IsJumpPathClear(Vector2 target)
{
    // Define the jump arc (simplified parabola with multiple line segments)
    Vector2 start = transform.position;
    Vector2 peak = new Vector2(
        start.x + (target.x - start.x) * 0.5f,
        Mathf.Max(start.y, target.y) + MaxJumpHeight * 0.5f
    );
    
    // Check first half (up)
    if (!IsPathSegmentClear(start, peak))
        return false;
        
    // Check second half (down)
    if (!IsPathSegmentClear(peak, target))
        return false;
        
    return true;
}

    private bool IsPathSegmentClear(Vector2 from, Vector2 to)
    {
        // Cast a ray along this path segment
        RaycastHit2D hit = Physics2D.Linecast(from, to, groundLayerMask);
        
        // Visualize the ray
        if (showJumpPrediction)
        {
            Debug.DrawLine(from, to, hit.collider != null ? Color.red : Color.green, 0.5f);
        }
        
        // Return true if no obstacles
        return hit.collider == null;
    }

    private void AttemptPlatformJump()
    {
        if (!playerOnHigherPlatform || Time.time - lastJumpTime < 3f)
            return;
            
        // Reset the detection flag to avoid spam jumping
        playerOnHigherPlatform = false;
        
        // Check if path to jump target is clear
        if (IsJumpPathClear(platformJumpTarget))
        {
            Debug.Log($"{gameObject.name}: Jump path is clear! Jumping to platform at y={detectedPlatformY:F1}");
            
            // Calculate height difference for jump force
            float heightDiff = detectedPlatformY - transform.position.y;
            
            // Execute the jump!
            StartIntelligentJump(heightDiff, platformJumpTarget);
        }
        else if (wallAhead) 
        {
            // Jump path blocked, try climbing instead
            Debug.Log($"{gameObject.name}: Jump path blocked, attempting to climb up");
            StartClimbing();
        }
        else
        {
            Debug.Log($"{gameObject.name}: Cannot reach platform - path blocked and no wall to climb");
        }
    }

    private void StartIntelligentJump(float heightNeeded, Vector2 target)
    {
        if (ghostMovement == null)
            return;

        // Log the jump attempt
        Debug.Log($"{gameObject.name}: Executing intelligent jump to height +{heightNeeded:F1}");
        
        // Set state and animation
        isJumping = true;
        currentGhostState = GhostState.Jumping;
        lastJumpTime = Time.time;
        
        // Trigger animation
        FireAnimationTrigger("JumpTrigger");
        
        // Face direction of target
        bool jumpRight = target.x > transform.position.x;
        ghostMovement.Flip(jumpRight);
        
        // Use movement component to perform jump physics
        ghostMovement.JumpToTarget(heightNeeded, target);
        
        // Change state to jumping
        ChangeState(State.Jumping);
    }


    
    private void FireAnimationTrigger(string triggerName)
    {
        if (ghostAnimator != null)
        {
            // Reset all triggers first to avoid conflicts
            ghostAnimator.ResetAllTriggers();
            
            // Then fire the appropriate trigger
            switch (triggerName)
            {
                case "MoveTrigger":
                    ghostAnimator.SetWalking(true);
                    break;
                case "JumpTrigger":
                    ghostAnimator.PlayJumpAnimation();
                    break;
                case "ClimbTrigger":
                    ghostAnimator.PlayClimbAnimation();
                    break;
                case "AttackTrigger":
                    ghostAnimator.PlayAttackAnimation();
                    break;
                case "StunedTrigger":
                    ghostAnimator.PlayHitAnimation();
                    break;
                case "DeathTrigger":
                    ghostAnimator.PlayDeathAnimation();
                    break;
                case "TalkTrigger":
                    ghostAnimator.PlayTalkAnimation();
                    break;
                case "SpecialATrigger":
                    ghostAnimator.PlaySpecialAttackAnimation();
                    break;
            }
            
            Debug.Log($"{gameObject.name}: Fired animation trigger '{triggerName}'");
        }
        else if (animator != null)
        {
            // Fallback to using animator directly
            animator.ResetTrigger("MoveTrigger");
            animator.ResetTrigger("JumpTrigger");
            animator.ResetTrigger("ClimbTrigger");
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("StunedTrigger");
            animator.ResetTrigger("DeathTrigger");
            animator.ResetTrigger("TalkTrigger");
            animator.ResetTrigger("SpecialATrigger");
            
            animator.SetTrigger(triggerName);
        }
    }
    
    private void CheckGroundBeneath()
    {
        float rayDistance = ghostData?.groundRayDistance ?? 10f;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            rayDistance, 
            groundLayerMask
        );
        
        if (hit.collider != null)
        {
            surfaceY = hit.point.y;
            float targetY = surfaceY + (ghostData?.hoverHeight ?? 1.2f);
            float currentY = transform.position.y;
            
            // Check if we're at the right hover height
            isGrounded = Mathf.Abs(currentY - targetY) < 0.05f;
            
            // Only adjust hover if not in special states
            if (!isClimbing && !isJumping && ghostMovement != null)
            {
                ghostMovement.AdjustHoverHeight(targetY);
            }
        }
        else
        {
            // If no ground below, assume we're in the air
            isGrounded = false;
        }
    }
    
    private void CheckEnvironmentConditions()
    {
        if (player == null)
            return;
            
        // Calculate vertical difference between ghost and player
        playerHeightDifference = player.position.y - transform.position.y;
        
        // Check for wall in front
        Vector2 wallCheckDirection = ghostMovement != null && ghostMovement.IsFacingRight ? 
                                    Vector2.right : Vector2.left;
                                    
        RaycastHit2D wallHit = Physics2D.Raycast(
            transform.position,
            wallCheckDirection,
            ghostData?.wallCheckDistance ?? 0.7f,
            groundLayerMask
        );
        
        wallAhead = wallHit.collider != null;
        
        // If there's a wall, check for a ledge
        if (wallAhead && ledgeCheckPoint != null)
        {
            RaycastHit2D ledgeHit = Physics2D.Raycast(
                ledgeCheckPoint.position,
                wallCheckDirection,
                ghostData?.ledgeCheckDistance ?? 1.5f,
                groundLayerMask
            );
            
            ledgeClear = ledgeHit.collider == null;
        }
        else
        {
            ledgeClear = false;
        }
        
        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, wallCheckDirection * (ghostData?.wallCheckDistance ?? 0.7f), 
                wallAhead ? Color.red : Color.green);
                
            if (ledgeCheckPoint != null)
            {
                Debug.DrawRay(ledgeCheckPoint.position, wallCheckDirection * (ghostData?.ledgeCheckDistance ?? 1.5f), 
                    ledgeClear ? Color.green : Color.red);
            }
            
            Debug.DrawRay(transform.position, Vector2.down * (ghostData?.groundRayDistance ?? 10f), Color.blue);
        }
    }
    private void LogDebugInfo()
    {
        if (Time.frameCount % 60 == 0) // Only log every 60 frames to avoid spam
        {
            if (player != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, player.position);
                Debug.Log($"Ghost-Player Info: Distance={distToPlayer:F1}, " +
                        $"Detection Range={DetectionRange:F1}, " +
                        $"Attack Range={AttackRange:F1}, " +
                        $"Can See Player={CanSeePlayer()}, " +
                        $"In Attack Range={IsInAttackRange()}, " +
                        $"Height Diff={playerHeightDifference:F1}");
            }
        }
    }
    
        // Make use of the currentGhostState field
        private void HandleSpecialStates()
        {
            if (isClimbing)
            {
                currentGhostState = GhostState.Climbing;
                
                // Check if we've reached the top of the wall
                if (ledgeClear || CheckTopReached())
                {
                    FinishClimbing();
                }
                else
                {
                    // Continue climbing
                    if (ghostMovement != null)
                    {
                        ghostMovement.Climb();
                    }
                }
            }
            else if (isJumping)
            {
                currentGhostState = GhostState.Jumping;
                
                // Check if we've landed after a jump
                if (isGrounded)
                {
                    FinishJumping();
                }
            }
            else
            {
                currentGhostState = GhostState.None;
            }
        }
    
    private bool CheckTopReached()
    {
        // Cast a ray downward from slightly ahead of the ghost to see if there's ground
        Vector2 checkDirection = ghostMovement != null && ghostMovement.IsFacingRight ? 
                                Vector2.right : Vector2.left;
                                
        Vector2 checkPosition = (Vector2)transform.position + checkDirection * 0.5f;
        
        RaycastHit2D hit = Physics2D.Raycast(
            checkPosition,
            Vector2.down,
            0.5f,
            groundLayerMask
        );
        
        return hit.collider != null;
    }
    
    private void StartJumping()
    {
        isJumping = true;
        currentGhostState = GhostState.Jumping;
        lastJumpTime = Time.time;
        
        // Fire jump animation trigger
        FireAnimationTrigger("JumpTrigger");
        
        // Execute the jump - fixed: only pass one parameter
        if (ghostMovement != null)
        {
            ghostMovement.Jump(playerHeightDifference);
        }
    }
    
    private void FinishJumping()
    {
        isJumping = false;
        currentGhostState = GhostState.None;
        
        // Return to chasing
        ChangeState(State.Chasing);
        FireAnimationTrigger("MoveTrigger");
    }
    
    private void StartClimbing()
    {
        isClimbing = true;
        currentGhostState = GhostState.Climbing;
        lastClimbTime = Time.time;
        
        // Fire climb animation trigger
        FireAnimationTrigger("ClimbTrigger");
        
        // Setup for climbing
        if (ghostMovement != null)
        {
            ghostMovement.StartClimbing();
        }
    }
    
    private void FinishClimbing()
    {
        isClimbing = false;
        currentGhostState = GhostState.None;
        
        if (ghostMovement != null)
        {
            ghostMovement.FinishClimbing();
        }
        
        // Small delay before returning to chase
        StartCoroutine(ResumeAfterClimb());
    }
    
    private IEnumerator ResumeAfterClimb()
    {
        yield return new WaitForSeconds(ghostData?.climbToIdleDelay ?? 0.3f);
        
        // Return to chasing
        ChangeState(State.Chasing);
        FireAnimationTrigger("MoveTrigger");
    }
    
    // Override handling for death
    public new void HandleDeath()
    {
        base.ChangeState(State.Dying);
        FireAnimationTrigger("DeathTrigger");
        
        // If we have a GhostEnemy component, play death effect
        GhostEnemy ghostEnemy = GetComponent<GhostEnemy>();
        if (ghostEnemy != null)
        {
            ghostEnemy.PlayVanishEffect();
        }
    }
    
    // Special attack functionality
    public void TriggerSpecialAttack()
    {
        if (currentState == State.Attacking && attack != null)
        {
            FireAnimationTrigger("SpecialATrigger");
            // You would implement special attack logic here, similar to normal attack
            StartCoroutine(PerformSpecialAttackSequence());
        }
    }

    // Override to customize damage handling for ghosts
protected new void HandleDamage(float amount)
{
    // Cancel any special state behaviors
    if (isClimbing || isJumping) 
    {
        isClimbing = false;
        isJumping = false;
        currentGhostState = GhostState.None;
    }
    
    // Store our current state before entering damage state
    State previousState = currentState;
    
    // Let base class handle the damage state change
    base.HandleDamage(amount);
    
    // Start our own recovery coroutine that will track and fix stuck state
    StartCoroutine(GhostRecoveryMonitor(previousState));
    
    Debug.Log($"Ghost: Taking damage while in {previousState} state, will monitor recovery");
}

// Custom ghost recovery monitor that runs parallel to base class recovery
private IEnumerator GhostRecoveryMonitor(State returnState)
{
    float recoveryStartTime = Time.time;
    float maxStuckTime = 1.5f; // Max time we allow the ghost to be in damage state
    
    // Wait for the regular damage recovery period
    yield return new WaitForSeconds(0.6f); // Slightly longer than parent's 0.5s
    
    // Check if we're still in TakingDamage state (potentially stuck)
    if (currentState == State.TakingDamage)
    {
        Debug.LogWarning($"Ghost: Still in damage state after recovery period! Forcing state change.");
        
        // Force the ghost back to an appropriate state
        if (player != null)
        {
            if (CanSeePlayer())
            {
                if (IsInAttackRange() && attack != null && attack.CanAttack)
                {
                    Debug.Log("Ghost: Recovery - forcing attack state");
                    ChangeState(State.Attacking);
                    FireAnimationTrigger("AttackTrigger");
                }
                else
                {
                    Debug.Log("Ghost: Recovery - forcing chase state");
                    ChangeState(State.Chasing);
                    FireAnimationTrigger("MoveTrigger");
                }
            }
            else if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Debug.Log("Ghost: Recovery - forcing patrol state");
                ChangeState(State.Patrolling);
                FireAnimationTrigger("MoveTrigger");
            }
            else
            {
                Debug.Log("Ghost: Recovery - forcing idle state");
                ChangeState(State.Idle);
            }
        }
        else
        {
            // No player, go to patrol or idle
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                ChangeState(State.Patrolling);
                FireAnimationTrigger("MoveTrigger");
            }
            else
            {
                ChangeState(State.Idle);
            }
        }
    }
    else
    {
        Debug.Log($"Ghost: Successfully recovered to {currentState} state");
    }
}

    // Override ChangeState to add debugging
    public override void ChangeState(State newState)
    {
        Debug.Log($"Ghost: STATE CHANGE from {currentState} to {newState}");
        base.ChangeState(newState);
    }
    
    private IEnumerator PerformSpecialAttackSequence()
    {
        // Special attack logic
        yield return new WaitForSeconds(0.5f);
        
        // Return to normal state
        ChangeState(State.Chasing);
        FireAnimationTrigger("MoveTrigger");
    }
    
    // Fix the CanSeePlayer method to properly use the eye transform
    private new bool CanSeePlayer()
    {
        if (player == null)
        {
            // Find player logic...
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                return false;
        }
        
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > DetectionRange)
            return Time.time - lastPlayerDetectedTime < ChaseMemoryDuration;
        
        // If ghost can see through walls, skip obstacle check
        if (ghostData != null && ghostData.sightThroughWalls)
        {
            lastPlayerDetectedTime = Time.time;
            return true;
        }
        
        // Use the eyes transform if available, otherwise use default position
        Vector2 eyePosition;
        if (eyesTransform != null)
        {
            eyePosition = eyesTransform.position;
        }
        else
        {
            eyePosition = (Vector2)transform.position + eyeOffset;
        }
        
        Vector2 directionToPlayer = ((Vector2)player.position - eyePosition).normalized;
        
        // Create layermask that excludes the ghost itself
        LayerMask sightMask = ~LayerMask.GetMask("Enemy");
        
        // Cast ray to player
        RaycastHit2D hit = Physics2D.Raycast(
            eyePosition,
            directionToPlayer, 
            distance, 
            sightMask
        );
        
        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawLine(eyePosition, player.position, 
                (hit.collider != null && hit.collider.gameObject == player.gameObject) ? Color.green : Color.yellow);
        }
        
        if (hit.collider != null && hit.collider.gameObject == player.gameObject)
        {
            lastPlayerDetectedTime = Time.time;
            return true;
        }
        
        return false;
    }
    
    // Enhanced visualization for ghost-specific features
// Enhanced visualization for better understanding attack and detection ranges
        private void OnDrawGizmos()
        {
            // Always draw these even in edit mode for better clarity
            Gizmos.color = new Color(1, 1, 0, 0.1f); // Yellow semi-transparent
            Gizmos.DrawWireSphere(transform.position, ghostDetectionRange);
            
            Gizmos.color = new Color(1, 0, 0, 0.2f); // Red semi-transparent
            Gizmos.DrawWireSphere(transform.position, ghostAttackRange);

            // Inside your OnDrawGizmos method
            if (playerOnHigherPlatform && showJumpPrediction)
            {
            // Draw the detected platform and jump target
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(platformJumpTarget, 0.3f);
            
            // Draw platform level
            Vector3 platformStart = new Vector3(transform.position.x - 5, detectedPlatformY);
            Vector3 platformEnd = new Vector3(transform.position.x + 5, detectedPlatformY);
            Gizmos.DrawLine(platformStart, platformEnd);
            
            // Draw text labels
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(platformJumpTarget + Vector2.up * 0.5f, "Jump Target");
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"Platform Y: {detectedPlatformY:F1}, Player on platform: {playerOnHigherPlatform}");
            #endif
            }
            
            // Draw text labels
            #if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                    $"Detection: {ghostDetectionRange:F1}\nAttack: {ghostAttackRange:F1}");
            }
            #endif
            
            // If in play mode, draw line to player
            if (Application.isPlaying && player != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, player.position);
                Gizmos.color = distToPlayer <= ghostAttackRange ? Color.red : 
                            (distToPlayer <= ghostDetectionRange ? Color.yellow : Color.gray);
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
}