using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Charge attack for bosses.
/// Boss winds up, then dashes rapidly toward the target position.
/// </summary>
public class BossChargeAttack : BaseBossAttack
{
    private bool isCharging;
    private Vector2 chargeDirection;
    private float chargeTimer;
    
    public bool IsCharging => isCharging;
    
    public override void Execute(Transform target)
    {
        if (!bossController.IsServer) return;
        if (target == null) return;
        if (isCharging) return;
        
        Debug.Log($"[BossChargeAttack] Starting charge toward {target.name}");
        
        // Start charge coroutine
        StartCoroutine(ChargeRoutine(target.position));
        
        StartCooldown();
    }
    
    private IEnumerator ChargeRoutine(Vector3 targetPosition)
    {
        isCharging = true;
        
        // 1. WINDUP PHASE
        // Show warning indicator
        GameObject warning = null;
        if (data.warningIndicatorPrefab != null)
        {
            warning = Instantiate(data.warningIndicatorPrefab, transform.position, Quaternion.identity);
            // Parent to boss so it follows during windup
            warning.transform.SetParent(transform);
        }
        
        // Notify clients about windup
        bossController.OnAttackWindupClientRpc(transform.position, targetPosition, data.chargeWindupTime);
        
        // Calculate charge direction once (don't track during windup)
        chargeDirection = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        
        // Wait for windup
        yield return new WaitForSeconds(data.chargeWindupTime);
        
        // Destroy warning
        if (warning != null) Destroy(warning);
        
        // 2. CHARGE PHASE
        // Spawn effect at start of charge
        SpawnEffect(data.attackEffectPrefab, transform.position);
        
        // Notify clients about charge start
        bossController.OnChargeStartClientRpc(chargeDirection);
        
        chargeTimer = data.chargeDuration;
        
        while (chargeTimer > 0f)
        {
            // Move boss in charge direction
            transform.position += (Vector3)(chargeDirection * data.chargeSpeed * Time.deltaTime);
            chargeTimer -= Time.deltaTime;
            yield return null;
        }
        
        // 3. END CHARGE
        isCharging = false;
        bossController.OnChargeEndClientRpc();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCharging) return;
        if (!bossController.IsServer) return;
        
        // Deal damage to players hit during charge
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out Health playerHealth))
            {
                playerHealth.TakeDamage(GetDamage());
                Debug.Log($"[BossChargeAttack] Hit player for {GetDamage()} damage!");
            }
        }
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
