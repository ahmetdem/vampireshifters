using UnityEngine;

/// <summary>
/// Defines an attack pattern that a boss can use.
/// Create different attack assets and assign them to BossData.
/// </summary>
[CreateAssetMenu(fileName = "NewBossAttack", menuName = "Game/Boss/Attack Data")]
public class BossAttackData : ScriptableObject
{
    [Header("Identity")]
    public string attackName = "New Attack";
    
    [Header("Attack Type")]
    public BossAttackType attackType = BossAttackType.Projectile;
    
    [Header("Base Stats")]
    [Tooltip("Damage dealt by this attack")]
    public int damage = 20;
    
    [Tooltip("Seconds between uses of this attack")]
    public float cooldown = 3f;
    
    [Tooltip("Range at which this attack can be used")]
    public float range = 10f;
    
    [Tooltip("Weight for random attack selection (higher = more likely)")]
    [Range(0.1f, 10f)]
    public float selectionWeight = 1f;
    
    [Header("Projectile Attack Settings")]
    [Tooltip("Prefab to spawn for projectile attacks")]
    public GameObject projectilePrefab;
    
    [Tooltip("Speed of projectile")]
    public float projectileSpeed = 8f;
    
    [Tooltip("Number of projectiles to fire")]
    public int projectileCount = 1;
    
    [Tooltip("Spread angle for multiple projectiles (degrees)")]
    public float spreadAngle = 30f;
    
    [Header("Charge Attack Settings")]
    [Tooltip("Speed of the charge dash")]
    public float chargeSpeed = 15f;
    
    [Tooltip("Windup time before charge starts")]
    public float chargeWindupTime = 0.5f;
    
    [Tooltip("Duration of the charge")]
    public float chargeDuration = 1f;
    
    [Header("Slam Attack Settings")]
    [Tooltip("Radius of the slam area")]
    public float slamRadius = 4f;
    
    [Tooltip("Delay before slam hits after windup")]
    public float slamDelay = 0.8f;
    
    [Header("Summon Attack Settings")]
    [Tooltip("Enemy prefab to summon")]
    public GameObject summonPrefab;
    
    [Tooltip("Number of enemies to summon")]
    public int summonCount = 3;
    
    [Tooltip("Spawn radius around boss")]
    public float summonRadius = 3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Warning indicator shown before attack (e.g., red circle for slam)")]
    public GameObject warningIndicatorPrefab;
    
    [Tooltip("Effect spawned when attack executes")]
    public GameObject attackEffectPrefab;
}

/// <summary>
/// Types of boss attacks
/// </summary>
public enum BossAttackType
{
    Projectile,  // Fires projectiles at players
    Charge,      // Dashes at a player
    Slam,        // AoE ground slam
    Summon       // Spawns minions
}
