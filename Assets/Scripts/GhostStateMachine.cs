using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(GhostSense))]
[RequireComponent(typeof(GhostPathMotor))]
[RequireComponent(typeof(Seeker))]
[DisallowMultipleComponent]
public class GhostStateMachine : MonoBehaviour
{
    private enum GhostState { Idle, Patrol, Chase, Attack, Dead }

    [Header("General Settings")]
    [SerializeField] private GhostData config;            // ScriptableObject holding stats
    [SerializeField] private Transform[] patrolPoints;

    [Header("Behaviour Timings")]
    [SerializeField] private float repathRate = 0.25f;
    [SerializeField] private float chaseMemory = 3f;
    [SerializeField] private float attackCooldown = 1.5f;

    // ----------------------------------------------------------------------
    private GhostSense _sense;
    private GhostPathMotor _motor;
    private Seeker _seeker;
    private EnemyAttack _attack;          // optional, only if present
    private GhostAnimator _anim;          // optional

    private GhostState _state = GhostState.Idle;
    private int _patrolIdx;
    private float _nextRepath;
    private float _lastSeen;
    private float _nextAttack;

    private void Awake()
    {
        _sense  = GetComponent<GhostSense>();
        _motor  = GetComponent<GhostPathMotor>();
        _seeker = GetComponent<Seeker>();
        _attack = GetComponent<EnemyAttack>();
        _anim   = GetComponent<GhostAnimator>();

        if (config != null)
        {
            chaseMemory     = config.chaseMemoryDuration;
        }

        _sense.OnVisibilityChanged += visible => { if (visible) _lastSeen = Time.time; };
    }

    private void Start()
    {
        ChangeState(patrolPoints != null && patrolPoints.Length > 0 ? GhostState.Patrol : GhostState.Idle);
    }

    private void Update()
    {
        switch (_state)
        {
            case GhostState.Idle:   TickIdle();   break;
            case GhostState.Patrol: TickPatrol(); break;
            case GhostState.Chase:  TickChase();  break;
        }
    }

    // ------------------------------------------------------------------
    #region  State Ticks
    private void TickIdle()
    {
        TrySeePlayerFromIdleOrPatrol();
    }

    private void TickPatrol()
    {
        if (_motor.IsIdle)
        {
            if (patrolPoints == null || patrolPoints.Length == 0) { ChangeState(GhostState.Idle); return; }
            _patrolIdx = (_patrolIdx + 1) % patrolPoints.Length;
            RequestPath(patrolPoints[_patrolIdx].position);
        }
        TrySeePlayerFromIdleOrPatrol();
    }

    private void TrySeePlayerFromIdleOrPatrol()
    {
        if (_sense.PlayerVisible)
        {
            ChangeState(GhostState.Chase);
        }
    }

    private void TickChase()
    {
        // Dynamic repath ------------------------------------------------
        if (Time.time >= _nextRepath && _sense.PlayerVisible)
        {
            _nextRepath = Time.time + repathRate;
            RequestPath(_sense.Player.position);
        }

        // Attack trigger ----------------------------------------------
        if (_sense.PlayerVisible && _sense.PlayerDistance <= (_attack ? _attack.AttackRadius : 3f))
        {
            if (Time.time >= _nextAttack)
            {
                StartCoroutine(AttackCoroutine());
            }
        }

        // Lost sight ---------------------------------------------------
        if (!_sense.PlayerVisible && Time.time - _lastSeen > chaseMemory)
        {
            ChangeState(GhostState.Patrol);
        }
    }
    #endregion

    // ------------------------------------------------------------------
    private IEnumerator AttackCoroutine()
    {
        ChangeState(GhostState.Attack);
        _motor.Stop();
        _nextAttack = Time.time + attackCooldown;

        _anim?.PlayAttackAnimation();
        _attack?.PerformAttack();
        yield return new WaitForSeconds(_attack ? _attack.AttackDuration : 0.5f);

        ChangeState(GhostState.Chase);
    }

    // ------------------------------------------------------------------
    private void ChangeState(GhostState newState)
    {
        if (_state == newState) return;

        // Exit logic ----------------------------------------------------
        switch (_state)
        {
            case GhostState.Chase:
            case GhostState.Patrol:
                _motor.Stop();
                break;
        }

        _state = newState;

        // Entry logic ---------------------------------------------------
        switch (newState)
        {
            case GhostState.Idle:
                _motor.Stop();
                break;

            case GhostState.Patrol:
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    _patrolIdx = FindNearestPatrolIndex();
                    RequestPath(patrolPoints[_patrolIdx].position);
                }
                break;

            case GhostState.Chase:
                _nextRepath = Time.time; // force immediate path
                _lastSeen = Time.time;
                break;

            case GhostState.Attack:
                _motor.Stop();
                break;
        }
    }

    private int FindNearestPatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return 0;
        float best = float.MaxValue; int bestIdx = 0;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float dist = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (dist < best) { best = dist; bestIdx = i; }
        }
        return bestIdx;
    }

    // ------------------------------------------------------------------
    private void RequestPath(Vector3 destination)
    {
        if (_seeker.IsDone())
        {
            _seeker.StartPath(transform.position, destination, OnPathComplete);
        }
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            _motor.Follow(p.vectorPath);
        }
        else
        {
            Debug.LogWarning($"GhostStateMachine: Path error → {p.errorLog}");
        }
    }

    // ------------------------------------------------------------------
    public void HandleDeath()
    {
        ChangeState(GhostState.Dead);
        _motor.Stop();
        _anim?.PlayDeathAnimation();
        enabled = false;
    }
} 