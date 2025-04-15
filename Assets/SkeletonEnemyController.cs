using System.Collections;
using UnityEngine;

public class SkeletonEnemyController : MonoBehaviour
{
    // Debug settings
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private float debugInterval = 1f;
    private float lastDebugTime = 0f;

    // Detection settings
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float chaseMemoryDuration = 3f;

    // Movement settings
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolStopDuration = 1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    // Attack settings
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;

    // Health settings
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float currentHealth = 20f;
    [SerializeField] private GameObject hitEffectPrefab;

    // References
    [Header("References")]
    [SerializeField] private LayerMask playerLayer;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D boxCollider;

    // States
    public enum State { Idle, Patrolling, Chasing, Attacking, TakingDamage, Dying }
    [Header("Current State (Read-Only)")]
    [SerializeField] private State currentState = State.Idle;

    // State tracking
    private int currentPatrolPoint = 0;
    private bool isWaitingAtPatrolPoint = false;
    private float lastPlayerDetectedTime = 0f;
    private float lastAttackTime = 0f;
    private bool isFacingRight = true;
    private Vector2 moveDirection;
    private Vector2 originalScale;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Store original scale
        originalScale = transform.localScale;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Create ground check if needed
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = groundCheckObj.transform;
            DebugLog("Created groundCheck automatically");
        }

        // Create attack point if needed
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.8f, 0, 0);
            attackPoint = attackPointObj.transform;
            DebugLog("Created attackPoint automatically");
        }

        // Initialize health
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Set initial state based on patrol points
        if (patrolPoints.Length > 0)
            ChangeState(State.Patrolling);
        else
            ChangeState(State.Idle);
            
        // Check for a player layer
        if (playerLayer.value == 0)
        {
            Debug.LogError("Player Layer not set on Skeleton Enemy! You need to create and assign a Player layer.");
        }
    }

    void Update()
    {
        if (currentState == State.Dying)
            return;

        // Check if grounded
        CheckGrounded();
        
        // Update debug periodically
        if (showDebugLogs && Time.time > lastDebugTime + debugInterval)
        {
            DebugLog("State: " + currentState + 
                    ", Facing: " + (isFacingRight ? "Right" : "Left") + 
                    ", Health: " + currentHealth);
            lastDebugTime = Time.time;
        }

        // Can we see the player?
        bool canSeePlayer = CanSeePlayer();

        // Handle current state
        switch (currentState)
        {
            case State.Idle:
                HandleIdleState(canSeePlayer);
                break;

            case State.Patrolling:
                HandlePatrollingState(canSeePlayer);
                break;

            case State.Chasing:
                HandleChasingState(canSeePlayer);
                break;
                
            case State.Attacking:
                // Handled by coroutine
                break;
                
            case State.TakingDamage:
                // Handled by coroutine
                break;
        }
    }

private void LateUpdate()
{
    // Debug the current animation state
    if (showDebugLogs && Time.frameCount % 60 == 0) // Limit to once per second
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        DebugLog("Current animation: " + stateInfo.fullPathHash + 
                ", normalizedTime: " + stateInfo.normalizedTime +
                ", speed: " + animator.speed);
    }
}

private void FixedUpdate()
{
    // Only move if in a moving state and grounded
    if (isGrounded && (currentState == State.Patrolling || currentState == State.Chasing))
    {
        float speed = (currentState == State.Patrolling) ? patrolSpeed : moveSpeed;
        rb.linearVelocity = new Vector2(moveDirection.x * speed, rb.linearVelocity.y);
    }
    else if (currentState == State.Attacking || currentState == State.TakingDamage)
    {
        // Ensure no movement during attacking or taking damage
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Keep vertical velocity
    }
    else if (currentState != State.Dying)
    {
        // If not in a moving state, stop horizontal movement only
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    
    // If the skeleton is supposed to be grounded but isn't, force it back to the ground
    if (!isGrounded && currentState != State.Dying)
    {
        Vector2 rayStart = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 10f, groundLayer);
        if (hit.collider != null)
        {
            float correctY = hit.point.y + 0.9f; // 0.9f should be adjusted based on your character height
            transform.position = new Vector3(transform.position.x, correctY, transform.position.z);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Stop vertical velocity when forced to ground
            DebugLog("Forced skeleton to ground at y=" + correctY);
        }
    }
}

    // State Handlers
    private void HandleIdleState(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(State.Chasing);
        }
        else if (patrolPoints.Length > 0)
        {
            ChangeState(State.Patrolling);
        }
        
        // In idle, we ensure no horizontal movement
        moveDirection = Vector2.zero;
    }

    private void HandlePatrollingState(bool canSeePlayer)
    {
        // If player spotted, chase them
        if (canSeePlayer)
        {
            ChangeState(State.Chasing);
            return;
        }

        // If no patrol points are assigned, go to idle
        if (patrolPoints.Length == 0)
        {
            ChangeState(State.Idle);
            return;
        }

        // If waiting at a point, do nothing
        if (isWaitingAtPatrolPoint)
            return;

        // Get the current patrol point target
        Transform targetPoint = patrolPoints[currentPatrolPoint];
        float distanceToTarget = targetPoint.position.x - transform.position.x;
        
        // Set direction toward patrol point
        moveDirection = new Vector2(Mathf.Sign(distanceToTarget), 0);
        
        // Make skeleton face the right way
        Flip(distanceToTarget > 0);
        
        // Check if we've reached the patrol point
        if (Mathf.Abs(distanceToTarget) < 0.1f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
        
        // Update animator
        animator.SetBool("isWalking", Mathf.Abs(moveDirection.x) > 0.1f);
    }

    private void HandleChasingState(bool canSeePlayer)
    {
        // If player is lost, go back to patrolling after memory duration
        if (!canSeePlayer)
        {
            if (Time.time - lastPlayerDetectedTime > chaseMemoryDuration)
            {
                DebugLog("Lost player, returning to patrol");
                if (patrolPoints.Length > 0)
                    ChangeState(State.Patrolling);
                else
                    ChangeState(State.Idle);
                return;
            }
        }
        
        if (player == null)
        {
            ChangeState(State.Idle);
            return;
        }
        
        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // If close enough to attack
        if (distanceToPlayer <= attackRange)
        {
            // Check if cooldown has passed
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                ChangeState(State.Attacking);
                return;
            }
            else
            {
                // Stop moving if in attack range but on cooldown
                moveDirection = Vector2.zero;
                return;
            }
        }
        
        // Chase the player
        float directionToPlayer = player.position.x - transform.position.x;
        moveDirection = new Vector2(Mathf.Sign(directionToPlayer), 0);
        
        // Face the player
        Flip(directionToPlayer > 0);
        
        // Update animator
        animator.SetBool("isWalking", Mathf.Abs(moveDirection.x) > 0.1f);
    }

    // Helper Methods
    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (!isGrounded)
        {
            DebugLog("WARNING: Skeleton not grounded!");
            // Force the skeleton to stay at its current height
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
    }

        private bool HasAnimatorParameter(string paramName){
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        DebugLog("WARNING: Parameter '" + paramName + "' not found in animator!");
        return false;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;
            
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= detectionRange)
        {
            // Optional line-of-sight check with raycast
            Vector2 direction = player.position - transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, detectionRange, groundLayer);
            
            // If something is blocking the view to player
            if (hit.collider != null && hit.collider.gameObject != player.gameObject)
            {
                return false;
            }
            
            lastPlayerDetectedTime = Time.time;
            return true;
        }
        
        return (Time.time - lastPlayerDetectedTime < chaseMemoryDuration);
    }

    private void Flip(bool faceRight)
    {
        // Only flip if the facing direction changed
        if (isFacingRight != faceRight)
        {
            isFacingRight = faceRight;
            
            // Only flip X scale, maintain Y and Z
            Vector3 theScale = transform.localScale;
            theScale.x = originalScale.x * (faceRight ? 1 : -1);
            transform.localScale = theScale;
            
            DebugLog("Flipped to face: " + (faceRight ? "Right" : "Left"));
        }
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState)
            return;
            
        DebugLog("Changing state from " + currentState + " to " + newState);
        
        // Exit current state
        switch (currentState)
        {
            case State.Patrolling:
                animator.SetBool("isWalking", false);
                break;
                
            case State.Chasing:
                animator.SetBool("isWalking", false);
                break;
        }
        
        // Enter new state
        currentState = newState;
        
        switch (newState)
        {
            case State.Idle:
                moveDirection = Vector2.zero;
                animator.SetBool("isWalking", false);
                break;
                
            case State.Patrolling:
                if (patrolPoints.Length > 0)
                {
                    // Find nearest patrol point to start with
                    if (!isWaitingAtPatrolPoint)
                    {
                        FindNearestPatrolPoint();
                    }
                }
                break;
                
            case State.Chasing:
                // Start walking animation
                animator.SetBool("isWalking", true);
                break;
                
            case State.Attacking:
                // Stop movement
                moveDirection = Vector2.zero;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                
                // Start attack
                StartCoroutine(PerformAttack());
                break;
        }
    }

    private void FindNearestPatrolPoint()
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
        DebugLog("Selected nearest patrol point: " + currentPatrolPoint);
    }
    

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaitingAtPatrolPoint = true;
        moveDirection = Vector2.zero;
        animator.SetBool("isWalking", false);
        
        DebugLog("Waiting at patrol point " + currentPatrolPoint);
        yield return new WaitForSeconds(patrolStopDuration);
        
        // Move to next patrol point
        currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
        isWaitingAtPatrolPoint = false;
        DebugLog("Moving to next patrol point: " + currentPatrolPoint);
    }

    private IEnumerator PerformAttack()
    {
        // Face the player before attacking
        if (player != null)
        {
            Flip(player.position.x > transform.position.x);
        }
        
        DebugLog("Performing attack");
        
        // Make sure movement is completely stopped
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        
        // Trigger attack animation
        animator.SetTrigger("Attack");
        
        // Wait for the wind-up
        yield return new WaitForSeconds(0.3f);
        
        // Apply damage if player is in range
        if (player != null)
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
            if (hitPlayer != null)
            {
                DebugLog("Attack hit player!");
                DamageInfo info = new DamageInfo(damage, transform.position);
                hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                DebugLog("Attack missed - no player in range");
            }
        }
        
        // Wait for attack animation to finish completely
        yield return new WaitForSeconds(0.8f);
        
        // Reset attack cooldown
        lastAttackTime = Time.time;
        
        // Return to appropriate state
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            // If player is still in attack range, attack again
            ChangeState(State.Attacking);
        }
        else if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            ChangeState(State.Chasing);
        }
        else if (patrolPoints.Length > 0)
        {
            ChangeState(State.Patrolling);
        }
        else
        {
            ChangeState(State.Idle);
        }
    }

    // Animation event method - called by the attack animation
    public void OnAttackHit()
    {
        // This gets called by the animation event at the exact frame of attack
        if (player != null && currentState == State.Attacking)
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
            if (hitPlayer != null)
            {
                DebugLog("Attack hit player from animation event!");
                DamageInfo info = new DamageInfo(damage, transform.position);
                hitPlayer.SendMessage("ApplyDamage", info, SendMessageOptions.DontRequireReceiver);
            }
        }
    }



    // Public methods
    public void TakeDamage(float damageAmount)
    {
        if (currentState == State.Dying)
            return;
            
        DebugLog("Taking " + damageAmount + " damage!");
        currentHealth -= damageAmount;
        
        // Optional hit effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Check for death
        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
        else
        {
            StartCoroutine(TakeDamageAnimation());
        }
    }
    

    private IEnumerator TakeDamageAnimation()
    {
        // Save previous state
        State previousState = currentState;
        currentState = State.TakingDamage;
        
        // Stop movement
        moveDirection = Vector2.zero;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Trigger hit animation
        animator.SetTrigger("Hit");
        
        // Wait for hit animation
        yield return new WaitForSeconds(0.5f);
        
        // Return to previous state if still alive
        if (currentState == State.TakingDamage)
        {
            if (previousState == State.Attacking)
            {
                // Don't go back to attacking, go to chasing
                ChangeState(State.Chasing);
            }
            else
            {
                ChangeState(previousState);
            }
        }
    }

    private IEnumerator Die()
    {
        DebugLog("Skeleton died!");
        currentState = State.Dying;
        
        // Stop movement
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        
        // Disable collisions and gravity
        if (boxCollider != null)
            boxCollider.enabled = false;
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Check if animator has a Die trigger
        if (HasAnimatorParameter("Die"))
        {
            animator.SetTrigger("Die");
            DebugLog("Die animation triggered");
        }
        else 
        {
            DebugLog("WARNING: Animator missing Die trigger!");
        }
        
        // Wait for animation
        yield return new WaitForSeconds(1.5f);
        
        // Destroy the skeleton
        Destroy(gameObject);
    }

    // Debug helper
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[Skeleton " + gameObject.name + "] " + message);
        }
    }

    // Draw gizmos for visualization
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Attack point
        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}