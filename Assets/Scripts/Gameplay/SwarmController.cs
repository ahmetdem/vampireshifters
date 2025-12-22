using Unity.Netcode;
using UnityEngine;

public class SwarmController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float trackingRefreshRate = 0.5f;
    private float currentSpeed;

    [Header("Collision Tuning")]
    [SerializeField] private CircleCollider2D swarmCollider;
    [SerializeField] private float padding = 1.2f;

    public NetworkVariable<float> difficultyMultiplier = new NetworkVariable<float>(1.0f);

    private Transform targetPlayer;
    private float trackingTimer;

    public void InitializeDifficulty(float difficulty)
    {
        // Update the Network Variable so clients know
        difficultyMultiplier.Value = difficulty;

        // SCALING LOGIC
        // 1. Slow Down: Speed decreases as difficulty goes up
        currentSpeed = baseSpeed / (1.0f + (difficulty * 0.1f));

        // 2. Tank Up: Health increases (e.g. 10 HP per difficulty level)
        if (TryGetComponent(out Health healthScript))
        {
            healthScript.IncreaseMaxHealth(Mathf.RoundToInt(difficulty * 10));
        }

        // 3. Get Rich: Loot value increases
        if (TryGetComponent(out LootDropper loot))
        {
            loot.SetLootMultiplier(difficulty);
        }

        // --- THE FIX ---
        // 4. Update Visuals Immediately (Host side)
        if (TryGetComponent(out SwarmVisuals visuals))
        {
            // This ensures the Host sees the change instantly
            visuals.SetSwarmDensity(difficulty);

            // Update the physical collider to match the new visual size
            if (IsServer && swarmCollider != null)
            {
                swarmCollider.radius = visuals.GetSwarmSpread() * padding;
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        if (currentSpeed == 0) currentSpeed = baseSpeed;

        if (TryGetComponent(out SwarmVisuals visuals))
        {
            visuals.SetSwarmDensity(difficultyMultiplier.Value);

            if (IsServer && swarmCollider != null)
            {
                swarmCollider.radius = visuals.GetSwarmSpread() * padding;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        trackingTimer -= Time.fixedDeltaTime;
        if (trackingTimer <= 0)
        {
            trackingTimer = trackingRefreshRate;
            targetPlayer = GetClosestPlayer();
        }

        if (targetPlayer != null)
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPos = targetPlayer.position;

            Vector2 direction = (targetPos - currentPos).normalized;

            transform.position += (Vector3)direction * currentSpeed * Time.fixedDeltaTime;
        }
    }

    private Transform GetClosestPlayer()
    {
        Transform closest = null;
        float minDst = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float dst = Vector2.Distance(transform.position, client.PlayerObject.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = client.PlayerObject.transform;
                }
            }
        }
        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        if (swarmCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, swarmCollider.radius);
        }
    }
}
