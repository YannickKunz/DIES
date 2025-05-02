// BaseEnemyAI.cs
using System.Collections;
using UnityEngine;

public class BaseEnemyAI : MonoBehaviour
{
    // Make these protected so derived classes can access them
    [HideInInspector] public enum State { Idle, Patrolling, Chasing, Attacking, TakingDamage, Dying }
    [SerializeField] protected State currentState = State.Idle;
    
    protected Transform player;
    protected float lastPlayerDetectedTime = 0f;
    protected EnemyMovement movement;
    protected EnemyAnimator animator;
    protected EnemyAttack attack;
    protected EnemyData data;
    
    [SerializeField] protected Transform[] patrolPoints;
    protected int currentPatrolPoint = 0;
    protected bool isWaitingAtPatrolPoint = false;
    
    [SerializeField] protected LayerMask whatIsGround; // Add this to fix m_WhatIsGround error
    
    // Provide access to important properties
    public float DetectionRange => data != null ? data.detectionRange : 7f;
    public float AttackRange => data != null ? data.attackRange : 1.5f;
    
    // Virtual methods that can be overridden
    public virtual void Initialize(EnemyData enemyData) {
        data = enemyData;
        movement = GetComponent<EnemyMovement>();
        animator = GetComponent<EnemyAnimator>();
        attack = GetComponent<EnemyAttack>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    protected virtual void Update() {
        // Base update logic
    }
    
    // Methods for state management
    protected virtual void ChangeState(State newState) {
        if (currentState == newState)
            return;
        
        currentState = newState;
    }
    
    protected virtual void HandleCurrentState() {
        // Base state handling
    }
    
    protected virtual bool IsInAttackRange() {
        if (player == null)
            return false;
            
        float distance = Vector2.Distance(transform.position, player.position);
        return distance <= AttackRange;
    }
    
    protected virtual bool CanSeePlayer() {
        // Base implementation
        return false;
    }
}