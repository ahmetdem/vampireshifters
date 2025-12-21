using Unity.Netcode;
using UnityEngine;

public class TestCoinDropper : NetworkBehaviour
{
    [SerializeField] private GameObject coinPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnCoin(new Vector3(2, 0, 0), 1);   // Small coin
            SpawnCoin(new Vector3(-2, 0, 0), 50); // Big sack
        }
    }

    private void SpawnCoin(Vector3 pos, int value)
    {
        GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity);
        coin.GetComponent<NetworkObject>().Spawn();
        coin.GetComponent<CoinPickup>().coinValue.Value = value; // Set value AFTER spawn
    }
}
