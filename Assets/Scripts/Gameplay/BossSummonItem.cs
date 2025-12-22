using Unity.Netcode;
using UnityEngine;

public class BossSummonItem : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Check if a player picked it up
        if (other.CompareTag("Player"))
        {
            // Trigger the Event!
            if (BossEventDirector.Instance != null)
            {
                BossEventDirector.Instance.ForceStartEvent();
            }

            // Destroy the item
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
