using UnityEngine;

[CreateAssetMenu(fileName = "GhostData", menuName = "Game/Ghost Data")]
public class GhostData : EnemyData
{
    [Header("Ghost Movement")]
    public float hoverHeight = 1.2f;
    public float hoverVariation = 0.2f;
    public float hoverSpeed = 2f;
    public float hoverSmoothing = 0.2f;
    public float hoverBobAmplitude = 0.1f;
    public float hoverBobSpeed = 1.5f;
    public float maxJumpHeight = 3f;
    public float jumpForce = 10f;
    public float climbSpeed = 3f;

    [Header("Ghost Detection")]
    public float wallCheckDistance = 0.7f;
    public float ledgeCheckDistance = 1.5f;
    public float groundRayDistance = 10f;
    public bool sightThroughWalls = false;

    [Header("Ghost Attack")]
    public float specialAttackCooldown = 5f;
    public float specialAttackDamage = 4f;

    [Header("Ghost Transition Times")]
    public float climbToIdleDelay = 0.3f;
    public float jumpToChaseDelay = 0.3f;
}