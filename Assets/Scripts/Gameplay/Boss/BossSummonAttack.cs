using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Summon attack for bosses.
/// Spawns minion enemies around the boss.
/// </summary>
public class BossSummonAttack : BaseBossAttack
{
    public override void Execute(Transform target)
    {
        if (!bossController.IsServer) return;
        
        Debug.Log($"[BossSummonAttack] Summoning {data.summonCount} minions");
        
        if (data.summonPrefab == null)
        {
            Debug.LogWarning("[BossSummonAttack] No summon prefab assigned!");
            return;
        }
        
        // Spawn attack effect at boss position
        SpawnEffect(data.attackEffectPrefab, transform.position);
        
        // Spawn minions in a circle around the boss
        for (int i = 0; i < data.summonCount; i++)
        {
            SpawnMinion(i);
        }
        
        StartCooldown();
    }
    
    private void SpawnMinion(int index)
    {
        // Calculate spawn position in a circle
        float angle = (360f / data.summonCount) * index * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * data.summonRadius;
        Vector3 spawnPos = transform.position + (Vector3)offset;
        
        // Spawn minion
        GameObject minion = Instantiate(data.summonPrefab, spawnPos, Quaternion.identity);
        
        // Network spawn
        if (minion.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
        }
        
        Debug.Log($"[BossSummonAttack] Spawned minion at {spawnPos}");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.cyan;
            
            // Draw spawn positions
            for (int i = 0; i < data.summonCount; i++)
            {
                float angle = (360f / data.summonCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * data.summonRadius;
                Gizmos.DrawWireSphere(transform.position + offset, 0.3f);
            }
        }
    }
}
