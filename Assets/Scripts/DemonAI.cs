using System.Collections;
using UnityEngine;
using Pathfinding;

// Demon AI with clean state machine and A* integration, plus special attacks
public class DemonAI : MonoBehaviour
{
    // Core state machine
    public enum DemonState { Idle, Patrolling, Chasing, Attacking, SpecialAttacking, TakingDamage, Dying }
    [SerializeField] private DemonState currentState = DemonState.Idle;
    
    [Header("Per-Demon Configuration (Override ScriptableObject)")]
    [Tooltip("When enabled, uses the values below instead of the DemonData asset values")]
    [SerializeField] private bool useInstanceValues = true;
    [Tooltip("How far the demon can detect the player")]
    [SerializeField] private float detectionRange = 15f;
    [Tooltip("How close the player must be for the demon to attack")]
    [SerializeField] private float attackRange = 7f;
    [Tooltip("How long the demon remembers where it last saw the player")]
    [SerializeField] private float chaseMemoryDuration = 3f;
    [Tooltip("Maximum time to stay idle before returning to patrol")]
    [SerializeField] private float maxIdleTime = 4f;
    [Tooltip("Points the demon will patrol between (leave empty for no patrolling)")]
    [SerializeField] private Transform[] patrolPoints;
    [Tooltip("Chance (0-1) that demon will use special attack when in range")]
    [SerializeField] private float specialAttackChance = 0.3f;
    
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
    private DemonMovement movement;
    private DemonAttack demonAttack;
    private DemonAnimator demonAnimator;
    private Transform player;
    private DemonData demonData;
    
    // State tracking
    private float lastPlayerSeenTime = -1000f;
    private float lastPathRequestTime = -1000f;
    private float idleStartTime = 0f;
    private float damageStateEnteredTime = 0f;
    private float attackStateEnteredTime = 0f;
    private int currentPatrolIndex = 0;
    private bool playerCurrentlyVisible = false;
    private float currentPlayerDistance = float.MaxValue;
    
    // Properties for clean access
    public float DetectionRange => useInstanceValues ? detectionRange : 
        ((demonData != null) ? demonData.detectionRange : 15f);
    
    public float AttackRange => useInstanceValues ? attackRange : 
        ((demonData != null) ? demonData.attackRange : 7f);
    
    public float ChaseMemoryDuration => useInstanceValues ? chaseMemoryDuration : 
        ((demonData != null) ? demonData.chaseMemoryDuration : 3f);
    
    private void Awake()
    {
        // Get required components
        seeker = GetComponent<Seeker>();
        movement = GetComponent<DemonMovement>();
        demonAttack = GetComponent<DemonAttack>();
        demonAnimator = GetComponent<DemonAnimator>();
        
        // Validate components
        if (seeker == null) Debug.LogError($"{gameObject.name}: Missing Seeker component!");
        if (movement == null) Debug.LogError($"{gameObject.name}: Missing DemonMovement component!");
        if (demonAttack == null) Debug.LogError($"{gameObject.name}: Missing DemonAttack component!");
        
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
        if (enemyData is DemonData)
        {
            demonData = (DemonData)enemyData;
            DebugLog("Initialized with DemonData (full demon features available)");
        }
        else if (enemyData != null)
        {
            DebugLog("Initialized with basic EnemyData (using fallback values for demon features)");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No EnemyData provided! Demon will use fallback values.");
        }
        
        // Initialize the DemonAttack component with whatever data we have
        if (demonAttack != null)
        {
            demonAttack.Initialize(enemyData);
            DebugLog("DemonAttack component initialized");
        }
        
        // Initialize the DemonMovement component with whatever data we have
        if (movement != null)
        {
            movement.Initialize(enemyData);
            DebugLog("DemonMovement component initialized");
        }
        
        // Initialize the DemonAnimator component
        if (demonAnimator != null)
        {
            demonAnimator.Initialize(enemyData);
            DebugLog("DemonAnimator component initialized");
        }
        
        // Validate patrol points
        ValidatePatrolPoints();
        
        // Set initial state
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            FindNearestPatrolPoint();
            ChangeState(DemonState.Patrolling);
        }
        else
        {
            ChangeState(DemonState.Idle);
        }
    }
    
    private void Update()
    {
        if (currentState == DemonState.Dying)
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
            case DemonState.Idle:
                if (ShouldStartChasing())
                    ChangeState(DemonState.Chasing);
                else if (ShouldStartPatrolling())
                    ChangeState(DemonState.Patrolling);
                break;
                
            case DemonState.Patrolling:
                if (ShouldStartChasing())
                    ChangeState(DemonState.Chasing);
                break;
                
            case DemonState.Chasing:
                if (ShouldSpecialAttack())
                    ChangeState(DemonState.SpecialAttacking);
                else if (ShouldAttack())
                    ChangeState(DemonState.Attacking);
                else if (ShouldStopChasing())
                {
                    if (patrolPoints != null && patrolPoints.Length > 0)
                        ChangeState(DemonState.Patrolling);
                    else
                        ChangeState(DemonState.Idle);
                }
                break;
                
            case DemonState.Attacking:
                if (!demonAttack.IsAttacking)
                {
                    if (ShouldSpecialAttack())
                    {
                        ChangeState(DemonState.SpecialAttacking);
                    }
                    else if (ShouldAttack())
                    {
                        StartCoroutine(PerformAttackSequence());
                    }
                    else if (ShouldStartChasing())
                    {
                        ChangeState(DemonState.Chasing);
                    }
                    else if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        ChangeState(DemonState.Patrolling);
                    }
                    else
                    {
                        ChangeState(DemonState.Idle);
                    }
                }
                else
                {
                    // Safety check: if we've been attacking for too long, force state change
                    float timeInAttackState = Time.time - attackStateEnteredTime;
                    if (timeInAttackState > 5f)
                    {
                        Debug.LogWarning($"{gameObject.name}: Been in attacking state for {timeInAttackState:F1}s - forcing recovery!");
                        
                        StopAllCoroutines();
                        
                        if (demonAttack != null)
                        {
                            typeof(DemonAttack).GetField("isAttacking", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.SetValue(demonAttack, false);
                        }
                        
                        if (ShouldStartChasing())
                            ChangeState(DemonState.Chasing);
                        else if (patrolPoints != null && patrolPoints.Length > 0)
                            ChangeState(DemonState.Patrolling);
                        else
                            ChangeState(DemonState.Idle);
                    }
                }
                break;
                
            case DemonState.SpecialAttacking:
                if (!demonAttack.IsAttacking)
                {
                    if (ShouldAttack())
                        ChangeState(DemonState.Attacking);
                    else if (ShouldStartChasing())
                        ChangeState(DemonState.Chasing);
                    else if (patrolPoints != null && patrolPoints.Length > 0)
                        ChangeState(DemonState.Patrolling);
                    else
                        ChangeState(DemonState.Idle);
                }
                break;
                
            case DemonState.TakingDamage:
                if (Time.time - damageStateEnteredTime > 1.0f)
                {
                    if (ShouldSpecialAttack())
                        ChangeState(DemonState.SpecialAttacking);
                    else if (ShouldAttack())
                        ChangeState(DemonState.Attacking);
                    else if (ShouldStartChasing())
                        ChangeState(DemonState.Chasing);
                    else if (patrolPoints != null && patrolPoints.Length > 0)
                        ChangeState(DemonState.Patrolling);
                    else
                        ChangeState(DemonState.Idle);
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
               demonAttack != null && demonAttack.CanAttack;
    }
    
    private bool ShouldSpecialAttack()
    {
        return playerCurrentlyVisible && currentPlayerDistance <= AttackRange && 
               demonAttack != null && demonAttack.CanSpecialAttack && 
               Random.value < specialAttackChance;
    }
    
    private bool ShouldStopChasing()
    {
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
            case DemonState.Idle:
                HandleIdleState();
                break;
                
            case DemonState.Patrolling:
                HandlePatrolState();
                break;
                
            case DemonState.Chasing:
                HandleChaseState();
                break;
                
            case DemonState.Attacking:
            case DemonState.SpecialAttacking:
                // Handled by coroutines
                break;
                
            case DemonState.TakingDamage:
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
            ChangeState(DemonState.Patrolling);
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
            ChangeState(DemonState.Idle);
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
                
        // Check if we've reached the current patrol point
        if (distanceToTarget < 3.0f)
        {
            DebugLog($"Reached patrol point {currentPatrolIndex}, moving to next");
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            
            if (movement != null && movement.IsFollowingAStarPath)
            {
                movement.StopAPath();
            }
            
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
            ChangeState(DemonState.Idle);
            return;
        }
        
        // Request new path to player periodically
        if (Time.time > lastPathRequestTime + pathfindingUpdateRate && seeker != null && seeker.IsDone())
        {
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
    
    public void ChangeState(DemonState newState)
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
    
    private void ExitState(DemonState state)
    {
        switch (state)
        {
            case DemonState.Idle:
                idleStartTime = 0f;
                break;
                
            case DemonState.Attacking:
            case DemonState.SpecialAttacking:
                StopAllCoroutines();
                break;
        }
    }
        
    private void EnterState(DemonState state)
    {
        switch (state)
        {
            case DemonState.Idle:
                idleStartTime = Time.time;
                if (movement != null)
                    movement.StopAPath();
                break;
                
            case DemonState.Patrolling:
                FindNearestPatrolPoint();
                break;
                
            case DemonState.Chasing:
                // Immediately request path to player
                if (player != null && seeker != null && seeker.IsDone())
                {
                    RequestPathTo(player.position);
                }
                break;
                
            case DemonState.Attacking:
                attackStateEnteredTime = Time.time;
                if (movement != null)
                    movement.StopAPath();
                StartCoroutine(PerformAttackSequence());
                break;
                
            case DemonState.SpecialAttacking:
                attackStateEnteredTime = Time.time;
                if (movement != null)
                    movement.StopAPath();
                StartCoroutine(PerformSpecialAttackSequence());
                break;
                
            case DemonState.TakingDamage:
                Debug.Log($"📍 {gameObject.name}: Entering TakingDamage state");
                damageStateEnteredTime = Time.time;
                if (movement != null)
                {
                    Debug.Log($"🛑 {gameObject.name}: Stopping A* path movement");
                    movement.StopAPath();
                }
                if (demonAnimator != null)
                {
                    Debug.Log($"🎬 {gameObject.name}: DemonAI calling PlayHitAnimation on DemonAnimator");
                    demonAnimator.PlayHitAnimation();
                }
                else
                {
                    Debug.LogError($"❌ {gameObject.name}: No DemonAnimator found - cannot play hit animation!");
                }
                break;
                
            case DemonState.Dying:
                if (movement != null)
                    movement.StopAPath();
                if (demonAnimator != null)
                    demonAnimator.PlayDeathAnimation();
                break;
        }
    }
    
    private IEnumerator PerformAttackSequence()
    {
        if (demonAttack == null || player == null)
        {
            DebugLog("Attack sequence aborted - missing demonAttack or player");
            yield break;
        }
        
        DebugLog("Starting attack sequence");
        
        // Face the player and stop movement
        if (movement != null && player != null)
        {
            movement.Flip(player.position.x > transform.position.x);
            movement.StopMoving();
        }
        
        // DemonAttack handles animation internally
        demonAttack.PerformAttack();
        
        DebugLog($"Waiting for attack to complete... (Duration: {demonAttack.AttackDuration}s)");
        
        // Wait for attack to complete
        yield return new WaitForSeconds(demonAttack.AttackDuration);
        
        DebugLog("Attack sequence completed - checking next state");
    }
    
    private IEnumerator PerformSpecialAttackSequence()
    {
        if (demonAttack == null || player == null)
        {
            DebugLog("Special attack sequence aborted - missing demonAttack or player");
            yield break;
        }
        
        DebugLog("Starting special attack sequence");
        
        // Face the player and stop movement
        if (movement != null && player != null)
        {
            movement.Flip(player.position.x > transform.position.x);
            movement.StopMoving();
        }
        
        // DemonAttack handles special attack animation internally
        demonAttack.PerformSpecialAttack();
        
        DebugLog($"Waiting for special attack to complete... (Duration: {demonAttack.AttackDuration}s)");
        
        // Wait for special attack to complete
        yield return new WaitForSeconds(demonAttack.AttackDuration);
        
        DebugLog("Special attack sequence completed - checking next state");
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
        Debug.Log($"👹 {gameObject.name}: DemonAI HandleDamage called - Amount: {amount}, Current State: {currentState}");
        
        if (currentState != DemonState.Dying)
        {
            Debug.Log($"🔄 {gameObject.name}: Changing state from {currentState} to TakingDamage");
            ChangeState(DemonState.TakingDamage);
        }
        else
        {
            Debug.Log($"💀 {gameObject.name}: Already dying, ignoring damage state change");
        }
    }
    
    private void HandleDeath()
    {
        Debug.Log($"☠️ {gameObject.name}: DemonAI HandleDeath called - changing to Dying state");
        ChangeState(DemonState.Dying);
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
                    bool isCurrentTarget = (currentState == DemonState.Patrolling && i == currentPatrolIndex);
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