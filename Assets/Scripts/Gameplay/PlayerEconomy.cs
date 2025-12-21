using Unity.Netcode;
using UnityEngine;

public class PlayerEconomy : NetworkBehaviour
{
    // Syncs coin count to all clients (needed for Scoreboard later)
    public NetworkVariable<int> totalCoins = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void CollectCoin(int value)
    {
        if (IsServer)
        {
            totalCoins.Value += value;
        }
    }
}
