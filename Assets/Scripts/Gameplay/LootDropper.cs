using Unity.Netcode;
using UnityEngine;

public class LootDropper : NetworkBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private float dropChance = 0.5f;

    [Header("Rare Drops")]
    [SerializeField] private GameObject[] rareDropTable;
    [SerializeField] private float rareDropChance = 0.01f;

    // 1. New variable to store the multiplier
    private float lootMultiplier = 1.0f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
    }

    // 2. New helper method for SwarmController to call
    public void SetLootMultiplier(float multiplier)
    {
        lootMultiplier = multiplier;
    }

    public void DropLoot()
    {
        // 1. Roll for RARE drop first
        if (rareDropTable.Length > 0 && Random.value <= rareDropChance)
        {
            // Pick a random rare item from the list
            GameObject itemToDrop = rareDropTable[Random.Range(0, rareDropTable.Length)];
            SpawnItem(itemToDrop);
            return; // Don't drop a coin if we dropped a rare item
        }

        // 2. Roll for COMMON drop (Coin)
        if (Random.value <= dropChance)
        {
            SpawnCoin();
        }
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null) return;

        GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
        NetworkObject netObj = coin.GetComponent<NetworkObject>();
        netObj.Spawn();

        if (coin.TryGetComponent(out CoinPickup coinScript))
        {
            // Use the coinValue variable here to clear the warning
            int finalValue = Mathf.RoundToInt(coinValue * lootMultiplier);
            coinScript.SetValue(finalValue);
        }
    }

    private void SpawnItem(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject item = Instantiate(prefab, transform.position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn();
    }
}
