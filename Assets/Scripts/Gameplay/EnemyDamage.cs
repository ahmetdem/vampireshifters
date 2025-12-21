using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageInterval = 1.0f;

    // Server-side only: tracks Player -> Next time they can be hit
    private Dictionary<ulong, float> nextDamageTime = new Dictionary<ulong, float>();

    private void OnTriggerStay2D(Collider2D other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (other.TryGetComponent(out Health health))
        {
            if (other.TryGetComponent(out NetworkObject netObj))
            {
                ulong clientId = netObj.OwnerClientId;

                if (!nextDamageTime.ContainsKey(clientId) || Time.time >= nextDamageTime[clientId])
                {
                    Debug.Log($"[EnemyDamage] Trigger Stay: Dealing {damageAmount} damage to Client {clientId}.");

                    health.TakeDamage(damageAmount);

                    // Set the next timestamp
                    nextDamageTime[clientId] = Time.time + damageInterval;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (other.TryGetComponent(out NetworkObject netObj))
        {
            if (nextDamageTime.ContainsKey(netObj.OwnerClientId))
            {
                Debug.Log($"[EnemyDamage] Client {netObj.OwnerClientId} exited damage zone.");
                nextDamageTime.Remove(netObj.OwnerClientId);
            }
        }
    }
}
