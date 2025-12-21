using Unity.Netcode;
using UnityEngine;

public class VisualEffectKiller : NetworkBehaviour
{
    [SerializeField] private float duration = 0.5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Auto-despawn after the flash is done
            Invoke(nameof(DespawnMe), duration);
        }
    }

    private void DespawnMe()
    {
        if (IsSpawned) NetworkObject.Despawn();
    }
}
