using Unity.Netcode;
using UnityEngine;

public class CoinPickup : NetworkBehaviour
{
    public NetworkVariable<int> coinValue = new NetworkVariable<int>(10);

    [Header("Auto-Despawn")]
    [SerializeField] private float lifetime = 30f; // Time before auto-despawn (editable in Inspector)

    // Safety flag to prevent double-collection
    private bool isCollected = false;

    public void SetValue(int newValue)
    {
        if (IsServer)
        {
            coinValue.Value = newValue;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && lifetime > 0)
        {
            // Schedule auto-despawn
            Invoke(nameof(AutoDespawn), lifetime);
        }
    }

    private void AutoDespawn()
    {
        if (!isCollected && IsSpawned)
        {
            Debug.Log($"[CoinPickup] Auto-despawning coin after {lifetime}s");
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Standard Server Check
        if (!IsServer) return;

        // 2. CRITICAL FIX: If already collected or already despawned, stop here.
        if (isCollected || !IsSpawned) return;

        if (other.TryGetComponent(out PlayerEconomy economy))
        {
            // Mark as collected immediately so the next collider ignores this code
            isCollected = true;

            // Cancel auto-despawn since we're collecting it
            CancelInvoke(nameof(AutoDespawn));

            economy.CollectCoin(coinValue.Value);

            // Safe to despawn now
            NetworkObject.Despawn();
        }
    }
}
