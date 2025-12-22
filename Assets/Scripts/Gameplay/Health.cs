using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
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

    protected virtual void Die()
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

    // Add this anywhere inside the class
    public void Heal(int amount)
    {
        if (!IsServer) return;

        // Use the variable name defined above
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (!IsServer) return;

        maxHealth += amount;
        currentHealth.Value += amount;
    }
}
