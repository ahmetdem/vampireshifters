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
        // Use GetComponentInParent to find Health on parent (for individual minions)
        Health health = target.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDamage(data.baseDamage);
        }

        // 3. Spawn Visual Effect (The "Zap")
        SpawnLightningEffect(target.position);

        return true;
    }

    private void SpawnLightningEffect(Vector3 targetPosition)
    {
        // Spawn a visual-only object that deletes itself quickly
        GameObject vfx = Instantiate(data.projectilePrefab, Vector3.zero, Quaternion.identity);

        // Set up the bolt to stretch from player to target
        LightningVisual lightningVisual = vfx.GetComponent<LightningVisual>();
        if (lightningVisual != null)
        {
            lightningVisual.SetupBolt(transform.position, targetPosition);
        }

        NetworkObject netObj = vfx.GetComponent<NetworkObject>();
        netObj.Spawn();
    }
}
