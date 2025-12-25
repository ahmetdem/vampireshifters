using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Health component for bosses.
/// Extends base Health with phase transition support.
/// </summary>
public class BossHealth : Health
{
    private BossController bossController;
    private int maxHealthValue;
    
    private void Awake()
    {
        bossController = GetComponent<BossController>();
    }
    
    /// <summary>
    /// Set the max health from BossData configuration.
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (!IsServer) return;
        
        maxHealthValue = newMaxHealth;
        currentHealth.Value = newMaxHealth;
    }
    
    /// <summary>
    /// Override TakeDamage to notify controller about phase transitions.
    /// </summary>
    public new void TakeDamage(int damage)
    {
        if (!IsServer) return;
        
        currentHealth.Value -= damage;
        
        Debug.Log($"[BossHealth] Boss took {damage} damage! HP: {currentHealth.Value}/{maxHealthValue}");
        
        // Notify controller for phase checks
        if (bossController != null && maxHealthValue > 0)
        {
            float healthPercent = (float)currentHealth.Value / maxHealthValue;
            bossController.OnHealthChanged(healthPercent);
        }
        
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }
    
    // Override the Die method (make sure Die is 'protected virtual' in Health.cs)
    protected override void Die()
    {
        if (IsServer)
        {
            // Spawn death effect
            if (bossController != null && bossController.Data != null && bossController.Data.deathEffectPrefab != null)
            {
                var effect = Instantiate(bossController.Data.deathEffectPrefab, transform.position, Quaternion.identity);
                if (effect.TryGetComponent(out NetworkObject netObj))
                {
                    netObj.Spawn();
                }
            }
            
            if (BossEventDirector.Instance != null)
            {
                BossEventDirector.Instance.OnBossDefeated();
            }
        }

        base.Die(); // Despawns the boss
    }
}

