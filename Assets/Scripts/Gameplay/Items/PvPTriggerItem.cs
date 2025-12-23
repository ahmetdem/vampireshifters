using Unity.Netcode;
using UnityEngine;

public class PvPTriggerItem : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // Trigger the "Sudden Death" PvP Mode
            if (PvPDirector.Instance != null)
            {
                Debug.Log($"[Item] Player {other.GetComponent<NetworkObject>().OwnerClientId} found the Blood Moon!");
                PvPDirector.Instance.StartPvPEvent();
            }

            // Despawn item
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
