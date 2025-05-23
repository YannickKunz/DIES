using UnityEngine;
using System.Collections.Generic;
using System;
using Pathfinding;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class GhostPathMotor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waypointReachedDistance = 0.35f;

    private readonly List<Vector3> _currentPath = new List<Vector3>();
    private int _wpIndex;
    private Rigidbody2D _rb;

    public bool IsIdle => _currentPath.Count == 0 || _wpIndex >= _currentPath.Count;

    public event Action OnReachedEndOfPath;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            Debug.LogError("GhostPathMotor: Rigidbody2D missing!");
        }
    }

    public void Follow(IList<Vector3> newPath, float speedOverride = -1f)
    {
        _currentPath.Clear();
        if (newPath != null)
        {
            _currentPath.AddRange(newPath);
        }
        _wpIndex = 0;
        if (speedOverride > 0f) moveSpeed = speedOverride;
    }

    public void Stop()
    {
        _currentPath.Clear();
        _rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (IsIdle) return;

        Vector2 pos   = _rb.position;
        Vector2 target= _currentPath[_wpIndex];
        Vector2 dir   = (target - pos).normalized;

        // Move --------------------------------------------------------------
        _rb.linearVelocity = dir * moveSpeed;

        // Optional: face direction by flipping localScale.x -----------------
        if (dir.x != 0)
        {
            Vector3 scl = transform.localScale;
            scl.x = Mathf.Abs(scl.x) * (dir.x > 0 ? 1 : -1);
            transform.localScale = scl;
        }

        // Arrive check ------------------------------------------------------
        if (Vector2.Distance(pos, target) < waypointReachedDistance)
        {
            _wpIndex++;
            if (IsIdle)
            {
                _rb.linearVelocity = Vector2.zero;
                OnReachedEndOfPath?.Invoke();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < _currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
        }
        Gizmos.color = Color.magenta;
        if (!IsIdle && _wpIndex < _currentPath.Count)
        {
            Gizmos.DrawSphere(_currentPath[_wpIndex], 0.2f);
        }
    }
#endif
} 