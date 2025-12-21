using Unity.Netcode;
using UnityEngine;

public class AreaWeapon : BaseWeapon
{
    protected override bool TryAttack()
    {
        if (!NetworkManager.Singleton.IsServer) return false;

        // 1. Find a target
        Transform target = FindNearestEnemy();
        if (target == null) return false;

        // 2. Deal Damage Immediately
        if (target.TryGetComponent(out Health health))
        {
            health.TakeDamage(data.baseDamage);
        }

        // 3. Spawn Visual Effect (The "Zap")
        SpawnLightningEffect(target.position);

        return true;
    }

    private void SpawnLightningEffect(Vector3 position)
    {
        // Spawn a visual-only object that deletes itself quickly
        // We use the Prefab from WeaponData as the "Explosion/Lightning" effect
        GameObject vfx = Instantiate(data.projectilePrefab, position, Quaternion.identity);

        NetworkObject netObj = vfx.GetComponent<NetworkObject>();
        netObj.Spawn();
    }
}
