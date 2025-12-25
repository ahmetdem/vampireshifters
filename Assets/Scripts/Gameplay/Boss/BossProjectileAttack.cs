using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Projectile attack for bosses.
/// Fires one or more projectiles at the target with optional spread.
/// </summary>
public class BossProjectileAttack : BaseBossAttack
{
    public override void Execute(Transform target)
    {
        if (!bossController.IsServer) return;
        if (target == null) return;
        
        Debug.Log($"[BossProjectileAttack] Firing {data.projectileCount} projectiles at {target.name}");
        
        Vector2 baseDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        
        // Calculate spread angles
        float totalSpread = data.spreadAngle;
        float angleStep = data.projectileCount > 1 ? totalSpread / (data.projectileCount - 1) : 0f;
        float startAngle = baseAngle - totalSpread / 2f;
        
        for (int i = 0; i < data.projectileCount; i++)
        {
            float angle = data.projectileCount > 1 ? startAngle + (angleStep * i) : baseAngle;
            SpawnProjectile(angle);
        }
        
        // Spawn attack effect
        SpawnEffect(data.attackEffectPrefab, transform.position);
        
        StartCooldown();
    }
    
    private void SpawnProjectile(float angleDegrees)
    {
        if (data.projectilePrefab == null)
        {
            Debug.LogWarning("[BossProjectileAttack] No projectile prefab assigned!");
            return;
        }
        
        // Calculate direction from angle
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        
        // Spawn at boss position with slight offset
        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);
        
        // Create projectile
        GameObject proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.Euler(0, 0, angleDegrees));
        
        // Network spawn
        if (proj.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
        }
        
        // Initialize projectile movement
        if (proj.TryGetComponent(out EnemyProjectile enemyProj))
        {
            enemyProj.Initialize(direction, data.projectileSpeed, GetDamage());
        }
        // Also check for BossProjectile if we create one
        else if (proj.TryGetComponent(out BossProjectile bossProj))
        {
            bossProj.Initialize(direction, data.projectileSpeed, GetDamage());
        }
    }
}
