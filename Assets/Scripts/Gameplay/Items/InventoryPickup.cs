using Unity.Netcode;
using UnityEngine;

public class InventoryPickup : NetworkBehaviour
{
    [SerializeField] private InventoryItemData itemData;

    [Header("Auto-Despawn")]
    [SerializeField] private float lifetime = 60f; // Time before auto-despawn (editable in Inspector)

    private bool isCollected = false;

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
            Debug.Log($"[InventoryPickup] Auto-despawning item after {lifetime}s");
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Only Server handles pickups
        if (isCollected || !IsSpawned) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerInventory inventory))
            {
                // Try to add it
                bool added = inventory.AddItem(itemData.itemId);

                if (added)
                {
                    isCollected = true;
                    CancelInvoke(nameof(AutoDespawn));

                    // Despawn ONLY if successfully added (bag not full)
                    GetComponent<NetworkObject>().Despawn();
                }
            }
        }
    }
}
