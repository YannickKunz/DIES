using System.Collections;
using UnityEngine;
using Pathfinding;

// Completely rewritten Ghost AI with clean state machine and proper A* integration
public class GhostAI : MonoBehaviour
{
    // Core state machine
    public enum GhostState { Idle, Patrolling, Chasing, Attacking, TakingDamage, Dying }
    [SerializeField] private GhostState currentState = GhostState.Idle;
    
    [Header("Per-Ghost Configuration (Override ScriptableObject)")]
    [Tooltip("When enabled, uses the values below instead of the GhostData asset values")]
    [SerializeField] private bool useInstanceValues = true;
    [Tooltip("How far the ghost can detect the player")]
    [SerializeField] private float detectionRange = 15f;
    [Tooltip("How close the player must be for the ghost to attack")]
    [SerializeField] private float attackRange = 7f;
    [Tooltip("How long the ghost remembers where it last saw the player")]
    [SerializeField] private float chaseMemoryDuration = 3f;
    [Tooltip("Maximum time to stay idle before returning to patrol")]
    [SerializeField] private float maxIdleTime = 4f;
    [Tooltip("Points the ghost will patrol between (leave empty for no patrolling)")]
    [SerializeField] private Transform[] patrolPoints;
    
    [Header("A* Pathfinding")]
    [SerializeField] private float repathRate = 1.0f;
    [SerializeField] private float pathfindingUpdateRate = 0.2f;
    
    [Header("References")]
    [SerializeField] private Transform eyesTransform;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool ignoreGroundForLineOfSight = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showPathGizmos = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    // Internal references
    private Seeker seeker;
    private GhostMovement movement;
    private GhostAttack ghostAttack;
    private GhostAnimator ghostAnimator;
    private Transform player;
    private GhostData ghostData;
    
    // State tracking
    private float lastPlayerSeenTime = -1000f;
    private float lastPathRequestTime = -1000f;
    private float idleStartTime = 0f;
    private float damageStateEnteredTime = 0f;
    private float attackStateEnteredTime = 0f; // Track when attack state was entered
    private int currentPatrolIndex = 0;
    private bool playerCurrentlyVisible = false;
    private float currentPlayerDistance = float.MaxValue;
    
    // Properties for clean access
    public float DetectionRange => useInstanceValues ? detectionRange : 
        ((ghostData != null) ? ghostData.detectionRange : 15f);
    
    public float AttackRange => useInstanceValues ? attackRange : 
        ((ghostData != null) ? ghostData.attackRange : 7f);
    
    public float ChaseMemoryDuration => useInstanceValues ? chaseMemoryDuration : 
        ((ghostData != null) ? ghostData.chaseMemoryDuration : 3f);
    
    private void Awake()
    {
        // Get required components
        seeker = GetComponent<Seeker>();
        movement = GetComponent<GhostMovement>();
        ghostAttack = GetComponent<GhostAttack>();
        ghostAnimator = GetComponent<GhostAnimator>();
        
        // Validate components
        if (seeker == null) Debug.LogError($"{gameObject.name}: Missing Seeker component!");
        if (movement == null) Debug.LogError($"{gameObject.name}: Missing GhostMovement component!");
        if (ghostAttack == null) Debug.LogError($"{gameObject.name}: Missing GhostAttack component!");
        
        // Set up layer masks if not already set
        if (groundLayerMask.value == 0)
        {
            groundLayerMask = LayerMask.GetMask("Ground");
        }
        
        if (obstacleMask.value == 0)
        {
            obstacleMask = ~LayerMask.GetMask("Player"); // Everything except player
        }
        
        // Find player
        FindPlayer();
        
        // Set up health events
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDamage += HandleDamage;
            health.OnDeath += HandleDeath;
        }
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            DebugLog("Player found and assigned");
        }
        else
        {
            DebugLog("Player not found!");
        }
    }
    
    public void Initialize(EnemyData enemyData)
    {
        if (enemyData is GhostData)
        {
            ghostData = (GhostData)enemyData;
            DebugLog("Initialized with GhostData (full ghost features available)");
        }
        else if (enemyData != null)
        {
            DebugLog("Initialized with basic EnemyData (using fallback values for ghost features)");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No EnemyData provided! Ghost will use fallback values.");
        }
        
        // Initialize the GhostAttack component with whatever data we have
        if (ghostAttack != null)
        {
            ghostAttack.Initialize(enemyData);
            DebugLog("GhostAttack component initialized");
        }
        
        // Initialize the GhostMovement component with whatever data we have
        if (movement != null)
        {
            movement.Initialize(enemyData);
            DebugLog("GhostMovement component initialized");
        }
        
        // Initialize the GhostAnimator component
        if (ghostAnimator != null)
        {
            ghostAnimator.Initialize(enemyData);
            DebugLog("GhostAnimator component initialized");
        }
        
        // Validate patrol points
        ValidatePatrolPoints();
        
        // Set initial state
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            FindNearestPatrolPoint();
            ChangeState(GhostState.Patrolling);
        }
        else
        {
            ChangeState(GhostState.Idle);
        }
    }
    
    private void Update()
    {
        if (currentState == GhostState.Dying)
            return;
        
        // Ensure we have a player reference
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }
        
        // Update player detection and distance (single source of truth)
        UpdatePlayerDetection();
        
        // Main state machine update
        UpdateStateMachine();
        
        // Handle current state behavior
        HandleCurrentState();
    }
    
    private void UpdatePlayerDetection()
    {
        if (player == null) 
        {
            playerCurrentlyVisible = false;
            currentPlayerDistance = float.MaxValue;
            return;
        }
        
        // Calculate distance
        currentPlayerDistance = Vector2.Distance(transform.position, player.position);
            
        // Check if player is within detection range
        if (currentPlayerDistance <= DetectionRange)
        {
            // Check line of sight
            playerCurrentlyVisible = HasLineOfSightToPlayer();
            
            if (playerCurrentlyVisible)
            {
                lastPlayerSeenTime = Time.time;
                DebugLog($"Player visible at distance {currentPlayerDistance:F2}");
            }
        }
        else
        {
            playerCurrentlyVisible = false;
        }
    }
    
    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        
        // Get eye position
        Vector2 eyePosition = eyesTransform != null ? 
            eyesTransform.position : 
            (Vector2)transform.position + new Vector2(0f, 0.7f);
        
        // Target player's center
        Vector2 targetPosition = (Vector2)player.position + new Vector2(0, 0.5f);
        Vector2 direction = (targetPosition - eyePosition).normalized;
        
        // Create vision mask
        LayerMask visionMask = obstacleMask;
        if (ignoreGroundForLineOfSight)
        {
            visionMask &= ~groundLayerMask;
        }
        
        // Cast ray
        RaycastHit2D hit = Physics2D.Raycast(
            eyePosition,
            direction,
            currentPlayerDistance,
            visionMask
        );
        
        // Draw debug ray
        if (showDebugRays)
        {
            Color rayColor = (hit.collider == null || hit.collider.CompareTag("Player")) ? Color.green : Color.red;
            Debug.DrawRay(eyePosition, direction * currentPlayerDistance, rayColor, 0.1f);
        }
        
        // Check if we can see player
        return hit.collider == null || hit.collider.CompareTag("Player");
    }
    
    private void UpdateStateMachine()
    {
        // Check for transitions based on current state and conditions
        switch (currentState)
        {
            case GhostState.Idle:
                if (ShouldStartChasing())
                    ChangeState(GhostState.Chasing);
                else if (ShouldStartPatrolling())
                    ChangeState(GhostState.Patrolling);
                break;
                
            case GhostState.Patrolling:
                if (ShouldStartChasing())
                    ChangeState(GhostState.Chasing);
                break;
                
            case GhostState.Chasing:
                if (ShouldAttack())
                    ChangeState(GhostState.Attacking);
                else if (ShouldStopChasing())
                {
                    if (patrolPoints != null && patrolPoints.Length > 0)
                        ChangeState(GhostState.Patrolling);
                    else
                        ChangeState(GhostState.Idle);
                }
                break;
                
            case GhostState.Attacking:
                if (!ghostAttack.IsAttacking)
                {
                    if (ShouldAttack())
                    {
                        // Continue attacking
                    StartCoroutine(PerformAttackSequence());
                }
                    else if (ShouldStartChasing())
                {
                        ChangeState(GhostState.Chasing);
                    }
                    else if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        ChangeState(GhostState.Patrolling);
                    }
                    else
                    {
                        ChangeState(GhostState.Idle);
                    }
                }
                else
                {
                    // Safety check: if we've been attacking for too long, force state change
                    float timeInAttackState = Time.time - attackStateEnteredTime;
                    if (timeInAttackState > 5f) // 5 seconds is way too long for an attack
                    {
                        Debug.LogWarning($"{gameObject.name}: Been in attacking state for {timeInAttackState:F1}s - forcing recovery!");
                        
                        // Force stop any attack that might be stuck
                        StopAllCoroutines();
                        
                        // Reset attack state
                        if (ghostAttack != null)
                        {
                            // Force the attack to complete
                            typeof(GhostAttack).GetField("isAttacking", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.SetValue(ghostAttack, false);
                        }
                        
                        // Transition to appropriate state
                        if (ShouldStartChasing())
                            ChangeState(GhostState.Chasing);
                        else if (patrolPoints != null && patrolPoints.Length > 0)
                            ChangeState(GhostState.Patrolling);
                        else
                            ChangeState(GhostState.Idle);
                    }
                }
                break;
                
            case GhostState.TakingDamage:
                if (Time.time - damageStateEnteredTime > 1.0f)
                {
                    if (ShouldAttack())
                        ChangeState(GhostState.Attacking);
                    else if (ShouldStartChasing())
                        ChangeState(GhostState.Chasing);
                    else if (patrolPoints != null && patrolPoints.Length > 0)
                        ChangeState(GhostState.Patrolling);
                    else
                        ChangeState(GhostState.Idle);
                }
                break;
        }
    }
    
    // Clear decision methods
    private bool ShouldStartChasing()
    {
        return playerCurrentlyVisible && currentPlayerDistance <= DetectionRange;
    }
    
    private bool ShouldAttack()
        {
        return playerCurrentlyVisible && currentPlayerDistance <= AttackRange && 
               ghostAttack != null && ghostAttack.CanAttack;
    }
    
    private bool ShouldStopChasing()
    {
        // Stop chasing if we haven't seen the player for too long
        return !playerCurrentlyVisible && 
               (Time.time - lastPlayerSeenTime) > ChaseMemoryDuration;
    }
    
    private bool ShouldStartPatrolling()
            {
        return patrolPoints != null && patrolPoints.Length > 0;
            }
    
    private bool RemembersPlayer()
    {
        return (Time.time - lastPlayerSeenTime) <= ChaseMemoryDuration;
    }
    
    private void HandleCurrentState()
    {
        switch (currentState)
        {
            case GhostState.Idle:
                HandleIdleState();
                break;
                
            case GhostState.Patrolling:
                HandlePatrolState();
                break;
                
            case GhostState.Chasing:
                HandleChaseState();
                break;
                
            case GhostState.Attacking:
                // Handled by coroutine
                break;
                
            case GhostState.TakingDamage:
                // Handled by state timer
                break;
        }
    }
    
    private void HandleIdleState()
    {
        // Start idle timer if not already started
        if (idleStartTime == 0f)
        {
            idleStartTime = Time.time;
        }
        
        // Check for idle timeout
        if (Time.time - idleStartTime > maxIdleTime && ShouldStartPatrolling())
        {
            ChangeState(GhostState.Patrolling);
            return;
        }
        
        // Ensure movement is stopped
        if (movement != null && movement.IsFollowingAStarPath)
        {
            movement.StopAPath();
        }
    }
    
    private void HandlePatrolState()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            ChangeState(GhostState.Idle);
            return;
        }
        
        // Validate current patrol index
        if (currentPatrolIndex >= patrolPoints.Length)
        {
            currentPatrolIndex = 0;
        }
        
            Transform target = patrolPoints[currentPatrolIndex];
        if (target == null)
            {
            DebugLog("Current patrol target is null, moving to next");
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            return;
        }
        
                float distanceToTarget = Vector2.Distance(transform.position, target.position);
                
        // Check if we've reached the current patrol point (larger distance for reliability)
        if (distanceToTarget < 3.0f) // Increased from 2.0f for better reliability
                {
            DebugLog($"Reached patrol point {currentPatrolIndex}, moving to next");
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    
            // Stop current path and wait a moment before requesting new path
            if (movement != null && movement.IsFollowingAStarPath)
            {
                movement.StopAPath();
                }
            
            // Don't immediately request new path - wait for next update cycle
            return;
        }
        
        // Only request new path if we're not currently following one and enough time has passed
        if (Time.time > lastPathRequestTime + repathRate && 
            seeker != null && seeker.IsDone() && 
            (!movement.IsFollowingAStarPath || distanceToTarget > 5.0f))
        {
            RequestPathTo(target.position);
        }
    }
    
    private void HandleChaseState()
    {
        if (player == null)
        {
                ChangeState(GhostState.Idle);
            return;
        }
        
        // Request new path to player periodically
        if (Time.time > lastPathRequestTime + pathfindingUpdateRate && seeker != null && seeker.IsDone())
        {
            // Only request path if we can still see player or remember seeing them recently
            if (playerCurrentlyVisible || RemembersPlayer())
            {
                RequestPathTo(player.position);
            }
        }
    }
    
    private void RequestPathTo(Vector3 targetPosition)
    {
        if (seeker == null || !seeker.IsDone()) 
        {
            DebugLog("Cannot request path: Seeker busy or null");
            return;
        }
        
        float distance = Vector2.Distance(transform.position, targetPosition);
        lastPathRequestTime = Time.time;
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        DebugLog($"Requested path to {targetPosition} (distance: {distance:F2}) in state {currentState}");
    }
    
    private void OnPathComplete(Path p)
    {
        if (p.error)
        {
            Debug.LogWarning($"{gameObject.name}: Path error: {p.errorLog}");
            return;
        }
        
        DebugLog($"Path completed with {p.vectorPath.Count} waypoints");
        
        // Send the path to movement controller
        if (movement != null)
        {
            movement.FollowAStarPath(p);
        }
    }
    
    public void ChangeState(GhostState newState)
    {
        if (currentState == newState)
            return;
        
        DebugLog($"State changed from {currentState} to {newState}");
        
        // Exit current state
        ExitState(currentState);
        
        // Set new state
        currentState = newState;
        
        // Enter new state
        EnterState(newState);
    }
    
    private void ExitState(GhostState state)
    {
        switch (state)
        {
            case GhostState.Idle:
                idleStartTime = 0f;
                break;
                
            case GhostState.Attacking:
                StopAllCoroutines();
                break;
        }
        }
        
    private void EnterState(GhostState state)
    {
        switch (state)
        {
            case GhostState.Idle:
                idleStartTime = Time.time;
                if (movement != null)
                    movement.StopAPath();
                break;
                
            case GhostState.Patrolling:
                FindNearestPatrolPoint();
                break;
                
            case GhostState.Chasing:
                // Immediately request path to player
                if (player != null && seeker != null && seeker.IsDone())
                {
                    RequestPathTo(player.position);
                }
                break;
                
            case GhostState.Attacking:
                attackStateEnteredTime = Time.time; // Track when we enter attacking state
                if (movement != null)
                    movement.StopAPath();
                StartCoroutine(PerformAttackSequence());
                break;
                
            case GhostState.TakingDamage:
                damageStateEnteredTime = Time.time;
                if (movement != null)
                    movement.StopAPath();
                if (ghostAnimator != null)
                    ghostAnimator.PlayHitAnimation();
                break;
                
            case GhostState.Dying:
                if (movement != null)
                    movement.StopAPath();
                if (ghostAnimator != null)
                    ghostAnimator.PlayDeathAnimation();
                break;
        }
    }
    
    private IEnumerator PerformAttackSequence()
    {
        if (ghostAttack == null || player == null)
        {
            DebugLog("Attack sequence aborted - missing ghostAttack or player");
            yield break;
        }
        
        DebugLog("Starting attack sequence");
        
        // Face the player and stop movement
        if (movement != null && player != null)
        {
            movement.Flip(player.position.x > transform.position.x);
            movement.StopMoving();
        }
        
        // GhostAttack now handles animation internally
        ghostAttack.PerformAttack();
        
        DebugLog($"Waiting for attack to complete... (Duration: {ghostAttack.AttackDuration}s)");
        
        // Wait for attack to complete
        yield return new WaitForSeconds(ghostAttack.AttackDuration);
        
        DebugLog("Attack sequence completed - checking next state");
        }
    
    private void FindNearestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;
        
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < patrolPoints.Length; i++)
    {
            if (patrolPoints[i] == null) continue;
            
            float distance = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        currentPatrolIndex = closestIndex;
        DebugLog($"Nearest patrol point is index {closestIndex}");
        }
        
    private void ValidatePatrolPoints()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No patrol points assigned!");
            return;
        }
        
        bool hasValidPoints = false;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
        {
                Debug.LogWarning($"{gameObject.name}: Patrol point at index {i} is null!");
            }
            else
        {
                hasValidPoints = true;
        }
        }
        
        if (!hasValidPoints)
        {
            Debug.LogError($"{gameObject.name}: All patrol points are null!");
        }
    }
    
    private void HandleDamage(float amount)
    {
        if (currentState != GhostState.Dying)
        {
            ChangeState(GhostState.TakingDamage);
        }
    }
    
    private void HandleDeath()
    {
        ChangeState(GhostState.Dying);
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] {message}");
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw detection and attack ranges
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, DetectionRange);
        
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, AttackRange);
        
        // Draw patrol points
        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    bool isCurrentTarget = (currentState == GhostState.Patrolling && i == currentPatrolIndex);
                    Gizmos.color = isCurrentTarget ? Color.yellow : Color.blue;
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    
                    // Draw lines between patrol points
                    if (i < patrolPoints.Length - 1 && patrolPoints[i+1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i+1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
        
        #if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                $"State: {currentState}\nVisible: {playerCurrentlyVisible}\nDistance: {currentPlayerDistance:F1}\nMemory: {(RemembersPlayer() ? "Yes" : "No")}");
        }
        #endif
    }
}