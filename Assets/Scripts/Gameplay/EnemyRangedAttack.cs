using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enables an enemy to shoot projectiles at players from range.
/// Attach to SwarmEnemy variants for ranged enemy types.
/// </summary>
public class EnemyRangedAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Projectile prefab to shoot")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("Range at which enemy starts attacking")]
    [SerializeField] private float attackRange = 8f;
    
    [Tooltip("Seconds between attacks")]
    [SerializeField] private float fireInterval = 2f;
    
    [Tooltip("Projectile travel speed")]
    [SerializeField] private float projectileSpeed = 5f;
    
    [Tooltip("Damage per projectile hit")]
    [SerializeField] private int projectileDamage = 10;

    [Header("Behavior")]
    [Tooltip("Stop moving while attacking")]
    [SerializeField] private bool stopWhileAttacking = true;
    
    [Tooltip("Minimum range - won't attack if player is closer (switch to melee)")]
    [SerializeField] private float minAttackRange = 2f;

    private SwarmController swarmController;
    private float fireTimer;
    private bool isAttacking;

    private void Awake()
    {
        swarmController = GetComponent<SwarmController>();
    }

    private void Update()
    {
        if (!IsServer) return;

        fireTimer -= Time.deltaTime;

        Transform target = GetClosestPlayer();
        if (target == null)
        {
            SetAttacking(false);
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        // Check if in attack range (but not too close)
        if (distance <= attackRange && distance >= minAttackRange)
        {
            SetAttacking(true);

            if (fireTimer <= 0f)
            {
                FireProjectile(target);
                fireTimer = fireInterval;
            }
        }
        else
        {
            SetAttacking(false);
        }
    }

    private void SetAttacking(bool attacking)
    {
        if (isAttacking == attacking) return;
        isAttacking = attacking;

        // Tell SwarmController to stop/resume movement
        if (swarmController != null && stopWhileAttacking)
        {
            swarmController.SetMovementPaused(attacking);
        }
    }

    private void FireProjectile(Transform target)
    {
        if (projectilePrefab == null) return;

        // Calculate direction to player
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        
        // Spawn projectile
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Spawn on network FIRST (so IsServer is properly set)
        if (proj.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
        }

        // THEN initialize projectile (after network spawn)
        if (proj.TryGetComponent(out EnemyProjectile enemyProj))
        {
            enemyProj.Initialize(direction, projectileSpeed, projectileDamage);
        }
    }

    private Transform GetClosestPlayer()
    {
        if (NetworkManager.Singleton == null) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float dist = Vector2.Distance(transform.position, client.PlayerObject.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = client.PlayerObject.transform;
                }
            }
        }
        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw min range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minAttackRange);
    }
}
