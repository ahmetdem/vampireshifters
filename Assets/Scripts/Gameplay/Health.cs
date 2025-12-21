using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (TryGetComponent(out PlayerNetworkState playerState))
        {
            // Player Death Logic (Respawn)...
            ConnectionHandler.Instance.HandlePlayerDeath(OwnerClientId);
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            // Enemy Death Logic
            if (TryGetComponent(out LootDropper loot))
            {
                loot.DropLoot();
            }

            Debug.Log($"[Health] Enemy {NetworkObjectId} Died.");
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
