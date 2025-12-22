using Unity.Netcode;
using UnityEngine;

public class LootDropper : NetworkBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private float dropChance = 1.0f;

    [Header("Rare Drops")]
    [SerializeField] private GameObject rareItemPrefab; // Drag Summon Scroll here
    [SerializeField] private float rareDropChance = 0.01f; // 1% chance

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
    }

    public void DropLoot()
    {
        if (Random.value <= rareDropChance && rareItemPrefab != null)
        {
            SpawnItem(rareItemPrefab);
            return;
        }

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

    private void SpawnItem(GameObject prefab)
    {
        GameObject item = Instantiate(prefab, transform.position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn();
    }
}
