using UnityEngine;

[CreateAssetMenu(fileName = "DemonData", menuName = "Enemy Data/Demon Data", order = 2)]
public class DemonData : EnemyData
{
    [Header("Demon Movement")]
    [Tooltip("How quickly the demon reaches its target hover height")]
    public float hoverSmoothing = 0.2f;
    [Tooltip("How much the demon bobs up and down while hovering")]
    public float hoverBobAmplitude = 0.1f;
    [Tooltip("How fast the demon bobs up and down")]
    public float hoverBobSpeed = 1.5f;
    [Tooltip("How powerful the demon's jumps are")]
    public float jumpForce = 10f;
    
    [Header("Demon Combat")]
    [Tooltip("Damage dealt by the demon's special attack")]
    public float specialAttackDamage = 4f;
    [Tooltip("Cooldown time between special attacks")]
    public float specialAttackCooldown = 5f;
    [Tooltip("Duration of the demon's attack animation")]
    public float attackDuration = 1.2f;
} 