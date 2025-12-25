using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Slam attack for bosses.
/// Area-of-effect attack with a warning indicator before dealing damage.
/// </summary>
public class BossSlamAttack : BaseBossAttack
{
    private bool isSlamming;
    
    public bool IsSlamming => isSlamming;
    
    public override void Execute(Transform target)
    {
        if (!bossController.IsServer) return;
        if (target == null) return;
        if (isSlamming) return;
        
        Debug.Log($"[BossSlamAttack] Starting slam at current position");
        
        // Slam at boss's current position (or could target player position)
        StartCoroutine(SlamRoutine(transform.position));
        
        StartCooldown();
    }
    
    private IEnumerator SlamRoutine(Vector3 slamPosition)
    {
        isSlamming = true;
        
        // 1. WARNING PHASE
        // Notify clients to show warning circle
        bossController.OnSlamWarningClientRpc(slamPosition, data.slamRadius, data.slamDelay);
        
        // Show warning indicator on server too
        GameObject warning = null;
        if (data.warningIndicatorPrefab != null)
        {
            warning = Instantiate(data.warningIndicatorPrefab, slamPosition, Quaternion.identity);
            // Scale warning to match slam radius
            warning.transform.localScale = Vector3.one * data.slamRadius * 2f;
        }
        
        // Wait for delay
        yield return new WaitForSeconds(data.slamDelay);
        
        // Destroy warning
        if (warning != null) Destroy(warning);
        
        // 2. SLAM IMPACT
        // Spawn impact effect
        SpawnEffect(data.attackEffectPrefab, slamPosition);
        
        // Notify clients about impact
        bossController.OnSlamImpactClientRpc(slamPosition, data.slamRadius);
        
        // Deal damage to all players in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(slamPosition, data.slamRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent(out Health playerHealth))
                {
                    playerHealth.TakeDamage(GetDamage());
                    Debug.Log($"[BossSlamAttack] Hit player for {GetDamage()} damage!");
                }
            }
        }
        
        isSlamming = false;
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.slamRadius);
        }
    }
}
