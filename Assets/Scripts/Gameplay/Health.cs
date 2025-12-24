using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // FORCE the network variable to match the Inspector setting
            currentHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        Debug.Log($"[Health] Took {damage} damage! Current HP: {currentHealth.Value - damage}"); // <--- ADD THIS

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            // Trace the stack to see what called Die()
            Debug.LogError($"[Health] DIED! Death triggered by: {System.Environment.StackTrace}"); // <--- ADD THIS
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

    /// <summary>
    /// Reset health to max. Used by object pooling when recycling enemies.
    /// </summary>
    public void ResetHealth()
    {
        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }
}
