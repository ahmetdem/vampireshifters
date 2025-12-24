using UnityEngine;

/// <summary>
/// Relays damage from individual minions to the parent SwarmController's Health.
/// This allows weapons to damage the swarm by hitting individual minions.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DamageRelay : MonoBehaviour
{
    private Health parentHealth;

    private void Start()
    {
        // Find the Health component on the parent swarm
        parentHealth = GetComponentInParent<Health>();
        
        if (parentHealth == null)
        {
            Debug.LogWarning($"[DamageRelay] No Health component found in parent hierarchy of {gameObject.name}!");
        }
    }

    /// <summary>
    /// Called by weapons when they need to access Health from this minion.
    /// Returns the parent swarm's Health component.
    /// </summary>
    public Health GetHealth()
    {
        return parentHealth;
    }
}
