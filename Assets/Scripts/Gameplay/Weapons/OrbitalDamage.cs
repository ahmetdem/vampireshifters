using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to orbital prefabs to handle damage on collision.
/// Tracks recently hit targets to prevent damage spam.
/// </summary>
public class OrbitalDamage : NetworkBehaviour
{
    private int damage;
    private ulong ownerId;
    private float hitCooldown = 0.5f; // Prevent damage spam on same target
    private Dictionary<ulong, float> recentHits = new Dictionary<ulong, float>();

    public void Initialize(int dmg, ulong owner)
    {
        damage = dmg;
        ownerId = owner;
    }

    private void Update()
    {
        // Clean up old hit records
        List<ulong> toRemove = new List<ulong>();
        foreach (var kvp in recentHits)
        {
            if (Time.time > kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove)
        {
            recentHits.Remove(id);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // 1. Hit Enemy
        if (other.CompareTag("Enemy"))
        {
            // Use instance ID for enemies since they don't have network IDs we can easily track
            int instanceId = other.gameObject.GetInstanceID();
            ulong enemyId = (ulong)(instanceId & 0x7FFFFFFF); // Convert to positive ulong

            if (!CanHit(enemyId)) return;

            // Use GetComponentInParent to find Health on parent (for individual minions)
            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                RecordHit(enemyId);
            }
        }
        // 2. Hit Player (PvP)
        else if (other.CompareTag("Player"))
        {
            if (PvPDirector.Instance != null && PvPDirector.Instance.IsPvPActive.Value)
            {
                NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.NetworkObjectId != ownerId)
                {
                    if (!CanHit(netObj.NetworkObjectId)) return;

                    Health health = netObj.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                        RecordHit(netObj.NetworkObjectId);
                        Debug.Log($"[PvP] Orbital hit Player {netObj.OwnerClientId}! Dealing {damage} dmg.");
                    }
                }
            }
        }
    }

    private bool CanHit(ulong targetId)
    {
        return !recentHits.ContainsKey(targetId);
    }

    private void RecordHit(ulong targetId)
    {
        recentHits[targetId] = Time.time + hitCooldown;
    }
}
