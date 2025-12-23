using Unity.Netcode;
using UnityEngine;

public class ProjectileWeapon : BaseWeapon
{
    protected override bool TryAttack()
    {
        // Only Server spawns networked projectiles
        if (!NetworkManager.Singleton.IsServer) return false;

        Transform target = FindNearestEnemy();

        // If no enemy is in range, don't fire (reset timer slightly?)
        if (target == null) return false;

        SpawnProjectile(target);
        return true;
    }

    private void SpawnProjectile(Transform target)
    {
        if (data.projectilePrefab == null) return;

        // Instantiate
        GameObject projObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);

        // Network Spawn
        NetworkObject netObj = projObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Initialize Projectile Logic (Direction, Damage)
        // We assume the prefab has a 'ProjectileMover' script
        if (projObj.TryGetComponent(out ProjectileMover mover))
        {
            Vector2 dir = (target.position - transform.position).normalized;

            // FIX: Use GetCurrentDamage() instead of data.baseDamage
            mover.Initialize(dir, data.projectileSpeed, GetCurrentDamage(), ownerId);
        }
    }
}
