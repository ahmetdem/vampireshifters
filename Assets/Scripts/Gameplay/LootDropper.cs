using Unity.Netcode;
using UnityEngine;

public class LootDropper : NetworkBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private float dropChance = 1.0f;

    [Header("Rare Drops")]
    [SerializeField] private GameObject rareItemPrefab;
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
        if (Random.value <= rareDropChance && rareItemPrefab != null)
        {
            SpawnItem(rareItemPrefab);
            return;
        }

        if (Random.value > dropChance) return;

        GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
        NetworkObject netObj = coin.GetComponent<NetworkObject>();
        netObj.Spawn();

        if (coin.TryGetComponent(out CoinPickup coinScript))
        {
            // 3. Apply the multiplier to the coin value
            // If base is 10 and difficulty is 3, this drops 30 gold.
            int finalValue = Mathf.RoundToInt(coinValue * lootMultiplier);
            coinScript.SetValue(finalValue);
        }
    }

    private void SpawnItem(GameObject prefab)
    {
        GameObject item = Instantiate(prefab, transform.position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn();
    }
}
