using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SwarmController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float trackingRefreshRate = 0.5f;
    private float currentSpeed;

    [Header("Damage Settings")]
    [SerializeField] private int baseDamageAmount = 10;
    [SerializeField] private float damageInterval = 1.0f;
    private int damageAmount; // Actual damage after scaling

    public NetworkVariable<float> difficultyMultiplier = new NetworkVariable<float>(1.0f);

    [Header("Despawn Settings")]
    [Tooltip("Despawn if further than this from ALL players")]
    [SerializeField] private float despawnDistance = 50f;
    
    [Tooltip("How often to check distance (seconds)")]
    [SerializeField] private float despawnCheckInterval = 2f;
    private float despawnCheckTimer;

    private Transform targetPlayer;
    private float trackingTimer;
    
    // Server-side damage cooldown tracking per player
    private Dictionary<ulong, float> nextDamageTime = new Dictionary<ulong, float>();

    private void Awake()
    {
        // Set default damage
        damageAmount = baseDamageAmount;
        
        // Link SwarmVisuals to this controller before Start() runs
        if (TryGetComponent(out SwarmVisuals visuals))
        {
            visuals.SetSwarmController(this);
        }
    }

    public void InitializeDifficulty(float difficulty)
    {
        InitializeDifficulty(difficulty, 1f, 1f);
    }

    /// <summary>
    /// Initialize with wave-specific multipliers.
    /// </summary>
    public void InitializeDifficulty(float difficulty, float waveHealthMult, float waveDamageMult)
    {
        // Update the Network Variable so clients know
        difficultyMultiplier.Value = difficulty;

        // SCALING LOGIC
        // 1. Slow Down: Speed decreases as difficulty goes up
        currentSpeed = baseSpeed / (1.0f + (difficulty * 0.1f));

        // 2. Tank Up: Health increases (e.g. 10 HP per difficulty level) + wave multiplier
        if (TryGetComponent(out Health healthScript))
        {
            int extraHealth = Mathf.RoundToInt(difficulty * 10 * waveHealthMult);
            healthScript.IncreaseMaxHealth(extraHealth);
        }

        // 3. Get Rich: Loot value increases
        if (TryGetComponent(out LootDropper loot))
        {
            loot.SetLootMultiplier(difficulty);
        }

        // 4. Update Visuals
        if (TryGetComponent(out SwarmVisuals visuals))
        {
            visuals.SetSwarmDensity(difficulty);
        }

        // 5. Hit Harder: Damage scales with difficulty (20% per level) + wave multiplier
        damageAmount = Mathf.RoundToInt(baseDamageAmount * (1.0f + (difficulty * 0.2f)) * waveDamageMult);
    }

    public override void OnNetworkSpawn()
    {
        if (currentSpeed == 0) currentSpeed = baseSpeed;

        if (TryGetComponent(out SwarmVisuals visuals))
        {
            visuals.SetSwarmDensity(difficultyMultiplier.Value);
        }
    }

    private bool isMovementPaused = false;

    /// <summary>
    /// Pause/resume movement. Used by EnemyRangedAttack when attacking.
    /// </summary>
    public void SetMovementPaused(bool paused)
    {
        isMovementPaused = paused;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Check if too far from all players
        despawnCheckTimer -= Time.fixedDeltaTime;
        if (despawnCheckTimer <= 0)
        {
            despawnCheckTimer = despawnCheckInterval;
            if (IsTooFarFromAllPlayers())
            {
                DespawnSelf();
                return;
            }
        }

        if (isMovementPaused) return; // Don't move while attacking

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

    /// <summary>
    /// Check if this enemy is too far from all players.
    /// </summary>
    private bool IsTooFarFromAllPlayers()
    {
        if (NetworkManager.Singleton == null) return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float dist = Vector2.Distance(transform.position, client.PlayerObject.transform.position);
                if (dist <= despawnDistance)
                {
                    return false; // At least one player is close enough
                }
            }
        }
        return true; // All players are too far
    }

    /// <summary>
    /// Despawn this enemy to free up the cap for new spawns.
    /// </summary>
    private void DespawnSelf()
    {
        Debug.Log($"[SwarmController] Despawning enemy too far from all players.");
        
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// Called by MinionDamage components when a minion collides with a player.
    /// Server validates cooldown and applies damage.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDamageServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        // Server-side cooldown validation to prevent spam/cheating
        if (nextDamageTime.TryGetValue(targetClientId, out float nextTime) && Time.time < nextTime)
        {
            return; // Still on cooldown
        }
        
        // Find the player and apply damage
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client))
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out Health health))
            {
                Debug.Log($"[SwarmController] Minion dealing {damageAmount} damage to Client {targetClientId}.");
                health.TakeDamage(damageAmount);
                
                // Set server-side cooldown
                nextDamageTime[targetClientId] = Time.time + damageInterval;
            }
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
        // Draw gizmos showing approximate swarm area
        if (TryGetComponent(out SwarmVisuals visuals))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, visuals.GetSwarmSpread());
        }
    }
}

