using Unity.Netcode;
using UnityEngine;

public class CoinPickup : NetworkBehaviour
{
    [Header("Settings")]
    public NetworkVariable<int> coinValue = new NetworkVariable<int>(1);

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite smallCoinSprite;
    [SerializeField] private Sprite bigSackSprite;

    public override void OnNetworkSpawn()
    {
        UpdateVisuals(coinValue.Value);
        coinValue.OnValueChanged += (oldVal, newVal) => UpdateVisuals(newVal);
    }

    private void UpdateVisuals(int value)
    {
        // Logic: If value > 10, show a "Sack", otherwise show a "Coin"
        if (smallCoinSprite != null && bigSackSprite != null)
        {
            spriteRenderer.sprite = value >= 10 ? bigSackSprite : smallCoinSprite;
        }

        // Optimization 2: Visual scaling based on value
        // Scale range: 1.0 (1 coin) to 2.0 (100 coins)
        float scale = 1f + Mathf.Clamp01(value / 100f);
        transform.localScale = Vector3.one * scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerEconomy economy))
        {
            economy.CollectCoin(coinValue.Value);

            // Despawn object across the network
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
