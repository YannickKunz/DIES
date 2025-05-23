using UnityEngine;

[CreateAssetMenu(fileName = "GhostData", menuName = "Game/Ghost Data")]
public class GhostData : EnemyData
{
    [Header("Ghost Movement")]
    public float hoverSmoothing = 0.2f;
    public float hoverBobAmplitude = 0.1f;
    public float hoverBobSpeed = 1.5f;
    public float jumpForce = 10f;

    [Header("Ghost Attack")]
    public float specialAttackCooldown = 5f;
    public float specialAttackDamage = 4f;
    public float attackDuration = 1.1f; // Duration of regular ghost attack animation
}