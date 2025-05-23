using UnityEngine;
using System;

[DisallowMultipleComponent]
public class GhostSense : MonoBehaviour
{
    [Header("Sense Settings")]
    [SerializeField] private LayerMask obstacleMask = ~0;          // Everything unless filtered out
    [SerializeField] private bool ignoreGroundForLineOfSight = true;
    [SerializeField] private Vector2 eyeOffset = new Vector2(0, 0.7f);
    [SerializeField] private Transform eyesTransform;              // Optional – overrides eyeOffset when assigned
    [SerializeField] private float detectionRange = 12f;

    // Public read-only data -----------------------------------------------------
    public bool PlayerVisible { get; private set; }
    public float PlayerDistance { get; private set; }
    public Transform Player => _player;

    //--------------------------------------------------------------------------
    private Transform _player;

    public event Action<bool> OnVisibilityChanged; // (newVisible)

    //--------------------------------------------------------------------------
    private void Awake()
    {
        // Cache player reference once at start – if your game allows respawn you
        // may need to refresh this pointer.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            PlayerVisible = false;
            PlayerDistance = float.PositiveInfinity;
            return;
        }

        // ------------------------------------------------------------------
        // 1. Range test (cheap)
        // ------------------------------------------------------------------
        PlayerDistance = Vector2.Distance(transform.position, _player.position);
        if (PlayerDistance > detectionRange)
        {
            SetVisible(false);
            return;
        }

        // ------------------------------------------------------------------
        // 2. Line-of-sight test (raycast)
        // ------------------------------------------------------------------
        Vector2 eyePos = eyesTransform != null ? (Vector2)eyesTransform.position
                                               : (Vector2)transform.position + new Vector2(eyeOffset.x * (transform.localScale.x >= 0 ? 1 : -1), eyeOffset.y);

        Vector2 targetPos = (Vector2)_player.position + new Vector2(0, 0.5f);
        Vector2 dir = (targetPos - eyePos).normalized;

        LayerMask mask = obstacleMask;
        if (ignoreGroundForLineOfSight)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) mask &= ~(1 << groundLayer);
        }

        RaycastHit2D hit = Physics2D.Raycast(eyePos, dir, PlayerDistance, mask);
        bool visible = hit.collider != null && hit.collider.transform.CompareTag("Player");

        // Debug draw ---------------------------------------------------------
#if UNITY_EDITOR
        Color c = visible ? Color.green : Color.red;
        Debug.DrawLine(eyePos, visible ? (Vector3)_player.position : (Vector3)(eyePos + dir * PlayerDistance), c);
#endif
        SetVisible(visible);
    }

    private void SetVisible(bool value)
    {
        if (PlayerVisible == value) return;
        PlayerVisible = value;
        OnVisibilityChanged?.Invoke(PlayerVisible);
    }

    public void ForceUpdate()
    {
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
        }
        
        if (_player != null)
        {
            Update(); // Run the regular update logic immediately
        }
    }
} 