// EnemyAI.cs
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{

[Header("Instance Override Settings")]
[SerializeField] protected bool useInstanceValues = false;
[Tooltip("Override the detection range from EnemyData")]
[SerializeField] protected float instanceDetectionRange = 7f;
[Tooltip("Override the attack range from EnemyData")]
[SerializeField] protected float instanceAttackRange = 1.5f;

private float idleStartTime = 0f;
[SerializeField] private float maxIdleTime = 4f; // Maximum time to stay in idle before resetting

public virtual float DetectionRange => useInstanceValues ? instanceDetectionRange : (data != null ? data.detectionRange : 7f);
public virtual float AttackRange => useInstanceValues ? instanceAttackRange : (data != null ? data.attackRange : 1.5f);
    protected EnemyData data;
    protected Transform player;
    protected float lastPlayerDetectedTime = 0f;
    protected EnemyMovement movement;
    protected EnemyAnimator animator;
    protected EnemyAttack attack;
    
    public enum State { Idle, Patrolling, Chasing, Attacking, TakingDamage, Dying, Jumping, Climbing }
    [SerializeField] protected State currentState = State.Idle;
        
    [SerializeField] protected Transform[] patrolPoints;
    protected int currentPatrolPoint = 0;
    protected bool isWaitingAtPatrolPoint = false;
        
    protected Coroutine patrolWaitRoutine;
    protected Coroutine attackRoutine;
    
    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        
        // Get components
        movement = GetComponent<EnemyMovement>();
        animator = GetComponent<EnemyAnimator>();
        attack = GetComponent<EnemyAttack>();
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        // Subscribe to health events
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDamage += HandleDamage;
            health.OnDeath += HandleDeath;
        }

            ValidatePatrolPoints();
        
        // Set initial state
        if (patrolPoints != null && patrolPoints.Length > 0)
            ChangeState(State.Patrolling);
        else
            ChangeState(State.Idle);
    }
    
protected virtual void Update()
{
    // Essential null checks
    if (data == null)
    {
        Debug.LogError(gameObject.name + ": EnemyData is null! Make sure it's assigned.");
        return;
    }
    
    // Don't update if dead
    if (currentState == State.Dying)
        return;
    
    // Check player visibility once to avoid multiple expensive calculations
    bool canSeePlayer = CanSeePlayer();
    bool inAttackRange = IsInAttackRange();
    
    // SECTION 1: Emergency checks for stuck states
    // ------------------------------------------------------------------------
    
    // Check for stuck in attack state without a routine
    if (currentState == State.Attacking && attackRoutine == null && 
        player != null && attack != null && attack.CanAttack)
    {
        Debug.Log($"{gameObject.name}: Detected stuck in attack state - restarting attack sequence");
        attackRoutine = StartCoroutine(PerformAttackSequence());
    }
    
    // SECTION 2: Periodic sanity checks (runs every 3 seconds)
    // ------------------------------------------------------------------------
    if (Time.frameCount % 180 == 0)
    {
        // Check for inconsistent attacking state
        if (currentState == State.Attacking && attackRoutine == null)
        {
            Debug.LogWarning($"{gameObject.name}: Inconsistent state detected! In Attacking state but no attack routine!");
            
            // Force state reset
            if (patrolPoints != null && patrolPoints.Length > 0)
                ChangeState(State.Patrolling);
            else
                ChangeState(State.Idle);
        }
        
        // Check if we can see the player but are in idle
        if (currentState == State.Idle && canSeePlayer && patrolPoints != null && patrolPoints.Length > 0)
        {
            Debug.Log($"{gameObject.name}: Can see player but in Idle state - correcting to chase");
            ChangeState(State.Chasing);
        }
        
        // Debug current state and conditions (only every 3 seconds to reduce spam)
        #if UNITY_EDITOR
        Debug.Log(gameObject.name + ": Current state: " + currentState +
            ", Can see player: " + canSeePlayer +
            ", In attack range: " + (player != null ? inAttackRange.ToString() : "No player") +
            ", Can attack: " + (attack != null ? attack.CanAttack.ToString() : "No attack component"));
        #endif
    }
    
    // SECTION 3: Core state management
    // ------------------------------------------------------------------------
    
    // Update state based on conditions (main state machine)
    UpdateState(canSeePlayer);
    
    // Handle behavior for the current state
    HandleCurrentState();
    
    // SECTION 4: Add any global emergency keys for debugging/testing
    // ------------------------------------------------------------------------
    #if UNITY_EDITOR
    // Global reset key for testing (uncomment if needed)
    
    if (Input.GetKeyDown(KeyCode.F12))
    {
        Debug.Log("Emergency reset of ALL enemies requested");
        foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.patrolPoints != null && enemy.patrolPoints.Length > 0)
                enemy.ChangeState(State.Patrolling);
            else
                enemy.ChangeState(State.Idle);
        }
    }
    
    #endif
}

    protected void LateUpdate()
{
    // Check for emergency reset via right-click
    if (Input.GetMouseButtonDown(1)) // Right-click
    {
        // Check if we're clicking on this enemy using raycasts
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            Debug.Log($"{gameObject.name}: Emergency reset from state {currentState}");
            
            // Cancel all coroutines
            StopAllCoroutines();
            
            // Reset to default state
            if (patrolPoints != null && patrolPoints.Length > 0)
                ChangeState(State.Patrolling);
            else
                ChangeState(State.Idle);
        }
    }
}
    
    protected void HandleCurrentState()
    {
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
                
            case State.Patrolling:
                HandlePatrolState();
                break;
                
            case State.Chasing:
                HandleChaseState();
                break;
                
            case State.Attacking:
                // Handled by coroutine
                break;
                
            case State.TakingDamage:
                // Handled by event
                break;
        }
    }
    
protected void UpdateState(bool canSeePlayer)
{
    // Debug current state
    if (Time.frameCount % 60 == 0)
    {
        Debug.Log(gameObject.name + ": Current state: " + currentState +
                ", Can see player: " + canSeePlayer +
                ", In attack range: " + (player != null ? IsInAttackRange().ToString() : "No player") +
                ", Can attack: " + (attack != null ? attack.CanAttack.ToString() : "No attack component"));
    }

    /*
    // Check for attack opportunity in any state except Attacking or Dying
    if (currentState != State.Attacking && currentState != State.Dying && 
        player != null && IsInAttackRange() && attack != null && attack.CanAttack)
    {
        // Force attack if player is in range regardless of visibility
        Debug.Log(gameObject.name + ": Player in range! ATTACKING!");
        ChangeState(State.Attacking);
        return;
    }
    */

    // Check for attack opportunity only when the enemy can see player
    if (currentState != State.Attacking && currentState != State.Dying && 
        player != null && canSeePlayer && IsInAttackRange() && attack != null && attack.CanAttack)
    {
        // Attack only if player is in range AND visible
        Debug.Log(gameObject.name + ": Player in sight and range! ATTACKING!");
        ChangeState(State.Attacking);
        return;
    }

    switch (currentState)
    {
        case State.Idle:
            if (canSeePlayer)
                ChangeState(State.Chasing);
            else if (patrolPoints.Length > 0)
                ChangeState(State.Patrolling);
            break;
            
        case State.Patrolling:
            if (canSeePlayer)
                ChangeState(State.Chasing);
            break;
            
        case State.Chasing:
            if (!canSeePlayer && Time.time - lastPlayerDetectedTime > data.chaseMemoryDuration)
            {
                if (patrolPoints.Length > 0)
                    ChangeState(State.Patrolling);
                else
                    ChangeState(State.Idle);
            }
            break;
    }
}
    
protected void HandleIdleState()
{
    // If this is the first frame we're handling idle
    if (idleStartTime == 0f)
    {
        idleStartTime = Time.time;
        Debug.Log($"{gameObject.name}: Started idle timer");
    }
    
    // Check if we've been idle for too long
    if (Time.time - idleStartTime > maxIdleTime)
    {
        Debug.Log($"{gameObject.name}: Idle timeout reached ({maxIdleTime}s), forcing state reset");
        
        // Force reset to patrolling if possible
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Debug.Log($"{gameObject.name}: Forcing return to patrol");
            ChangeState(State.Patrolling);
        }
        else
        {
            // If no patrol points, we at least reset the idle timer
            idleStartTime = Time.time;
            
            // Try to look for the player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && player == null)
            {
                player = playerObj.transform;
                Debug.Log($"{gameObject.name}: Re-acquired player reference during idle reset");
            }
        }
    }

    // Existing code
    if (movement != null)
    {
        movement.StopMoving();
    }
}
    
    protected void HandlePatrolState()
    {
        if (patrolPoints.Length == 0 || isWaitingAtPatrolPoint)
            return;
            
        if (movement == null)
            return;
            
        Transform targetPoint = patrolPoints[currentPatrolPoint];
        float distanceToTarget = targetPoint.position.x - transform.position.x;
        
        // Set direction toward patrol point
        Vector2 direction = new Vector2(Mathf.Sign(distanceToTarget), 0);
        movement.SetMoveDirection(direction, true);
        
        // Check if we've reached the patrol point
        if (Mathf.Abs(distanceToTarget) < 0.1f)
        {
            if (patrolWaitRoutine != null)
                StopCoroutine(patrolWaitRoutine);
                
            patrolWaitRoutine = StartCoroutine(WaitAtPatrolPoint());
        }
    }
    
    protected void HandleChaseState()
    {
        if (player == null || movement == null)
            return;
            
        // If in attack range but attack is on cooldown, stop and wait
        if (IsInAttackRange() && attack != null && !attack.CanAttack)
        {
            movement.StopMoving();
            return;
        }
        
        // Move toward player
        float directionToPlayer = player.position.x - transform.position.x;
        Vector2 direction = new Vector2(Mathf.Sign(directionToPlayer), 0);
        movement.SetMoveDirection(direction, false);
    }
    
    protected IEnumerator WaitAtPatrolPoint()
    {
        isWaitingAtPatrolPoint = true;
        movement.StopMoving();
        
        yield return new WaitForSeconds(data.patrolStopDuration);
        
        currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
        isWaitingAtPatrolPoint = false;
    }
    
    public virtual void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        // Reset idle timer when leaving idle state
        if (currentState == State.Idle)
        {
            idleStartTime = 0f;
        }    
        // Exit previous state
        switch (currentState)
        {
            case State.Patrolling:
                if (patrolWaitRoutine != null)
                {
                    StopCoroutine(patrolWaitRoutine);
                    isWaitingAtPatrolPoint = false;
                }
                break;
                
            case State.Attacking:
                if (attackRoutine != null)
                {
                    StopCoroutine(attackRoutine);
                }
                break;
        }
        
        // Set new state
        currentState = newState;
        
        // Enter new state
        switch (newState)
        {
            case State.Idle:
            
            // Reset idle timer when leaving idle state
            idleStartTime = Time.time;

                if (movement != null)
                    movement.StopMoving();
                break;
                
            case State.Patrolling:
                FindNearestPatrolPoint();
                break;
                
            case State.Attacking:
                if (attack != null)
                {
                    attackRoutine = StartCoroutine(PerformAttackSequence());
                }
                break;
        }
    }
    
// Improved PerformAttackSequence() for reliable attacks
protected IEnumerator PerformAttackSequence()
{
    // Always face the player when attacking
    if (player != null && movement != null)
    {
        movement.Flip(player.position.x > transform.position.x);
        movement.StopMoving();
    }
    
    // Log attack attempt
    Debug.Log(gameObject.name + ": Executing attack sequence!");
    
    // Perform attack
    attack.PerformAttack();
    
    // Wait for first half of attack animation (when the hit should register)
    yield return new WaitForSeconds(0.3f);
    
    // Force damage check if animation event might be unreliable
    attack.OnAttackHit();
    
    // Wait for the rest of the attack animation
    yield return new WaitForSeconds(attack.AttackDuration - 0.3f);
    
    // Decide what to do next - add extensive logging to track decisions
    Debug.Log($"{gameObject.name}: Attack sequence completed, deciding next action...");
    
    // First check if the player is null or the game object is inactive
    if (player == null || !player.gameObject.activeInHierarchy)
    {
        Debug.Log($"{gameObject.name}: Player reference lost or inactive, returning to patrol");
        // Force return to patrolling or idle
        if (patrolPoints != null && patrolPoints.Length > 0)
            ChangeState(State.Patrolling);
        else
            ChangeState(State.Idle);
        yield break;
    }
    
    // Log the decision factors
    bool inAttackRange = IsInAttackRange();
    bool canAttackAgain = attack.CanAttack;
    bool canSeePlayer = CanSeePlayer();
    bool hasPatrolPoints = (patrolPoints != null && patrolPoints.Length > 0);
    
    Debug.Log($"{gameObject.name}: Decision factors - In Attack Range: {inAttackRange}, " + 
              $"Can Attack Again: {canAttackAgain}, Can See Player: {canSeePlayer}, " +
              $"Has Patrol Points: {hasPatrolPoints}");
    
    if (inAttackRange && canAttackAgain){
    Debug.Log($"{gameObject.name}: Player still in range, attacking again");
    // Don't just change state - directly start new attack if already in attack state
    if (currentState == State.Attacking){
        // Create a new attack routine directly
        attackRoutine = StartCoroutine(PerformAttackSequence());
    }
    else 
    {
        ChangeState(State.Attacking);
    }
}
    /*// Check if player is still in attack range and can attack again
    if (inAttackRange && canAttackAgain)
    {
        Debug.Log($"{gameObject.name}: Player still in range, attacking again");
        ChangeState(State.Attacking);
    }*/
    else if (canSeePlayer)
    {
        Debug.Log($"{gameObject.name}: Can see player, chasing");
        ChangeState(State.Chasing);
    }
    else if (hasPatrolPoints)
    {
        Debug.Log($"{gameObject.name}: Cannot see player, returning to patrol");
        ChangeState(State.Patrolling);
    }
    else
    {
        Debug.Log($"{gameObject.name}: No patrol points, going to idle");
        ChangeState(State.Idle);
    }
}

[Header("Line of Sight Settings")]
[SerializeField] protected Vector2 eyeOffset = new Vector2(0, 0.7f); // Adjust to where the skeleton's "eyes" would be
[SerializeField] protected bool ignoreGroundForLineOfSight = true;
[SerializeField] protected Transform eyesTransform; // Assign this in the Inspector to your "eyes" child

protected bool CanSeePlayer()
{
    if (player == null)
    {
        // Try to find player again if lost
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log(gameObject.name + ": Found player!");
        }
        else
        {
            return false;
        }
    }
    
    // Check if player is within detection range
    float distance = Vector2.Distance(transform.position, player.position);
    if (distance > DetectionRange)
        return Time.time - lastPlayerDetectedTime < data.chaseMemoryDuration;


    
    // Use the actual eyes transform position instead of calculating it
    Vector2 eyePosition = eyesTransform != null ? 
                          eyesTransform.position : 
                          ((Vector2)transform.position + new Vector2(eyeOffset.x * (movement.IsFacingRight ? 1 : -1), eyeOffset.y));
    
    // Calculate target position - aim at player's center, not feet
    Vector2 targetPosition = (Vector2)player.position + new Vector2(0, 0.5f);
    
    // Debug eye position
    Debug.DrawRay(eyePosition, Vector2.up * 0.2f, Color.blue, 0.1f);
    
    // Direction to player from eye position
    Vector2 direction = (targetPosition - eyePosition).normalized;
    
    // Create layermask that excludes ground if needed
    LayerMask sightMask = ignoreGroundForLineOfSight ? 
        ~(LayerMask.GetMask("Ground") | LayerMask.GetMask("Enemy")) : 
        ~LayerMask.GetMask("Enemy");
    
    // Cast ray from eye height to player chest
    RaycastHit2D hit = Physics2D.Raycast(
        eyePosition,
        direction, 
        distance, 
        sightMask
    );
    
    // Debug raycasts
    if (hit.collider != null)
    {
        Debug.DrawLine(eyePosition, hit.point, 
            (hit.collider.gameObject == player.gameObject) ? Color.green : Color.yellow, 0.1f);
        
        // Hit player
        if (hit.collider.gameObject == player.gameObject)
        {
            lastPlayerDetectedTime = Time.time;
            return true;
        }
        
        // Hit something else - log for debugging
        Debug.Log($"{gameObject.name}: Line of sight blocked by {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        return false;
    }
    else
    {
        // Ray didn't hit anything - means clear line to player
        Debug.DrawLine(eyePosition, targetPosition, Color.green, 0.1f);
        lastPlayerDetectedTime = Time.time;
        return true;
    }
}
    
protected bool IsInAttackRange()
{
    if (player == null)
        return false;
        
    float distance = Vector2.Distance(transform.position, player.position);
    bool inRange = distance <= AttackRange;  // Use property instead of data.attackRange
    
    // Debug visualization
    if (inRange)
    {
        Debug.DrawLine(transform.position, player.position, Color.red, 0.1f);
    }
    
    return inRange;
}
    protected void FindNearestPatrolPoint()
    {
        if (patrolPoints.Length == 0)
            return;
            
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;
                
            float distance = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        currentPatrolPoint = closestIndex;
    }
    
protected void HandleDamage(float amount)
{
    // Store previous state before changing to TakingDamage
    State previousState = currentState;
    
    // Cancel all existing coroutines that might interfere with state transitions
    if (attackRoutine != null)
    {
        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }
    
    if (patrolWaitRoutine != null)
    {
        StopCoroutine(patrolWaitRoutine);
        patrolWaitRoutine = null;
    }
    
    // Use proper state change method rather than direct assignment
    ChangeState(State.TakingDamage);
    
    // Store the previous state to resume later and start the resume coroutine
    StopAllCoroutines(); // Stop any other possible coroutines
    StartCoroutine(ResumeAfterDamage(previousState));
    
    // Debug to see what's happening
    Debug.Log($"{gameObject.name}: Taking damage, will resume {previousState} state in 0.5s");
}

protected IEnumerator ResumeAfterDamage(State previousState)
{
    yield return new WaitForSeconds(0.5f);
    
    // Only change state if we're still in TakingDamage (not dead or changed by another system)
    if (currentState == State.TakingDamage)
    {
        Debug.Log($"{gameObject.name}: Resuming {previousState} state after damage");
        
        // If the previous state was Attacking but we're no longer in attack range, change to Chasing instead
        if (previousState == State.Attacking && player != null)
        {
            if (IsInAttackRange() && attack != null && attack.CanAttack)
            {
                ChangeState(State.Attacking);
            }
            else if (CanSeePlayer())
            {
                ChangeState(State.Chasing);
            }
            else if (patrolPoints != null && patrolPoints.Length > 0)
            {
                ChangeState(State.Patrolling);
            }
            else
            {
                ChangeState(State.Idle);
            }
        }
        else
        {
            ChangeState(previousState);
        }
    }
    else
    {
        Debug.Log($"{gameObject.name}: Not resuming after damage, state changed to {currentState}");
    }
}
protected void ValidatePatrolPoints()
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
        Debug.LogError($"{gameObject.name}: All patrol points are null! This will cause behavior issues.");
    }
}
    
    protected void HandleDeath()
    {
        ChangeState(State.Dying);
    }
    protected void OnDrawGizmosSelected()
{
    // Draw detection range (green)
    Gizmos.color = new Color(0, 1, 0, 0.2f);  // Transparent green
    float detectionRadius = useInstanceValues ? instanceDetectionRange : 
        (data != null ? data.detectionRange : 7f);
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
    
    // Draw attack range (red)
    Gizmos.color = new Color(1, 0, 0, 0.2f);  // Transparent red
    float attackRadius = useInstanceValues ? instanceAttackRange : 
        (data != null ? data.attackRange : 1.5f);
    Gizmos.DrawWireSphere(transform.position, attackRadius);
}

}