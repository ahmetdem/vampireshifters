using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Weapon that fires piercing projectiles (like Vampire Fang).
/// Uses PiercingProjectileMover instead of regular ProjectileMover.
/// </summary>
public class PiercingProjectileWeapon : BaseWeapon
{
    [Header("Pierce Settings")]
    private int pierceCount = 3; // How many enemies it can pass through

    public override void Initialize(WeaponData weaponData, ulong id)
    {
        base.Initialize(weaponData, id);
        // Use duration field as pierce count (configurable in WeaponData)
        pierceCount = Mathf.Max(1, Mathf.RoundToInt(data.duration));
    }

    protected override bool TryAttack()
    {
        if (!NetworkManager.Singleton.IsServer) return false;

        Transform target = FindNearestEnemy();
        if (target == null) return false;

        SpawnPiercingProjectile(target);
        return true;
    }

    private void SpawnPiercingProjectile(Transform target)
    {
        if (data.projectilePrefab == null) return;

        GameObject projObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);

        NetworkObject netObj = projObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        // Initialize with pierce logic
        if (projObj.TryGetComponent(out PiercingProjectileMover mover))
        {
            Vector2 dir = (target.position - transform.position).normalized;
            mover.Initialize(dir, data.projectileSpeed, GetCurrentDamage(), ownerId, pierceCount);
        }
    }
}
