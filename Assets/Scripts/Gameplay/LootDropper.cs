using Unity.Netcode;
using UnityEngine;

public class LootDropper : NetworkBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private float dropChance = 1.0f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
    }

    public void DropLoot()
    {
        // Simple RNG check
        if (Random.value > dropChance) return;

        // Spawn coin
        GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
        NetworkObject netObj = coin.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Set Value
        if (coin.TryGetComponent(out CoinPickup coinScript))
        {
            coinScript.SetValue(coinValue);
        }
    }
}
