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
        // Only try to respawn if it's a player
        if (TryGetComponent(out PlayerNetworkState playerState))
        {
            // Tell the Manager to handle the respawn logic
            ConnectionHandler.Instance.HandlePlayerDeath(OwnerClientId);

            // Destroy this object immediately
            // Since the Coroutine runs in ConnectionHandler, it won't be cancelled!
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            // Just destroy enemies
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
