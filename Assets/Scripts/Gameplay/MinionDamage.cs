using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to each visual minion prefab to handle individual collision detection.
/// Delegates damage application to the parent SwarmController via ServerRpc.
/// Requires: CircleCollider2D (or any Collider2D) set as trigger on the prefab.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MinionDamage : MonoBehaviour
{
    private SwarmController parentSwarm;
    private float nextDamageTime = 0f;
    private const float DAMAGE_INTERVAL = 1.0f;
    
    public void Initialize(SwarmController swarm)
    {
        parentSwarm = swarm;
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // Skip if not initialized
        if (parentSwarm == null) return;
        
        // Check if we hit a player
        if (other.TryGetComponent(out Health health) && other.TryGetComponent(out NetworkObject netObj))
        {
            // Local cooldown check to avoid spamming ServerRpcs
            if (Time.time < nextDamageTime) return;
            
            nextDamageTime = Time.time + DAMAGE_INTERVAL;
            
            // Request damage from the server
            parentSwarm.RequestDamageServerRpc(netObj.OwnerClientId);
        }
    }
}
