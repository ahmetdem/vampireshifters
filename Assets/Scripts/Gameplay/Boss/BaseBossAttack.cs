using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Abstract base class for boss attack behaviors.
/// Each attack type (Projectile, Charge, Slam, Summon) inherits from this.
/// </summary>
public abstract class BaseBossAttack : MonoBehaviour
{
    protected BossAttackData data;
    protected BossController bossController;
    protected float cooldownTimer;
    
    /// <summary>
    /// Initialize the attack with its data and parent controller.
    /// </summary>
    public virtual void Initialize(BossAttackData attackData, BossController controller)
    {
        data = attackData;
        bossController = controller;
        cooldownTimer = 0f;
    }
    
    /// <summary>
    /// Returns true if this attack is ready to use.
    /// </summary>
    public virtual bool IsReady => cooldownTimer <= 0f;
    
    /// <summary>
    /// Get the selection weight for random attack choosing.
    /// </summary>
    public float SelectionWeight => data != null ? data.selectionWeight : 1f;
    
    /// <summary>
    /// Get the range of this attack.
    /// </summary>
    public float Range => data != null ? data.range : 10f;
    
    /// <summary>
    /// Update cooldown timer. Called every frame.
    /// </summary>
    public virtual void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Execute the attack toward a target.
    /// </summary>
    public abstract void Execute(Transform target);
    
    /// <summary>
    /// Start the cooldown for this attack.
    /// </summary>
    protected void StartCooldown()
    {
        float multiplier = 1f;
        if (bossController != null && bossController.CurrentPhase != null)
        {
            multiplier = bossController.CurrentPhase.cooldownMultiplier;
        }
        cooldownTimer = data.cooldown * multiplier;
    }
    
    /// <summary>
    /// Get the damage for this attack, applying phase multipliers.
    /// </summary>
    protected int GetDamage()
    {
        float multiplier = 1f;
        if (bossController != null && bossController.CurrentPhase != null)
        {
            multiplier = bossController.CurrentPhase.damageMultiplier;
        }
        return Mathf.RoundToInt(data.damage * multiplier);
    }
    
    /// <summary>
    /// Spawn a visual effect at a position.
    /// </summary>
    protected void SpawnEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null) return;
        
        GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
        
        // If it's a network object, spawn it on the network
        if (effect.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
        }
        
        // Auto-destroy after 5 seconds if no auto-destroy component
        if (effect.GetComponent<ParticleSystem>() == null)
        {
            Destroy(effect, 5f);
        }
    }
}
