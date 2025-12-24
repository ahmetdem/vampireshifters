using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Object pooling system for enemies. Reuses enemy objects instead of
/// Instantiate/Destroy to reduce garbage collection and improve performance.
/// </summary>
public class EnemyPool : NetworkBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Maximum pool size per enemy type")]
    [SerializeField] private int maxPoolSize = 50;

    // Pool storage: prefab hashcode -> list of inactive enemies
    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    
    // Track all active enemies for count limiting
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    // Track enemies per player for per-player spawn caps
    private Dictionary<ulong, List<GameObject>> enemiesPerPlayer = new Dictionary<ulong, List<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Get an enemy from the pool (or create new if pool empty).
    /// Legacy overload - uses global tracking only.
    /// </summary>
    public GameObject GetEnemy(GameObject prefab, Vector3 position, float difficulty, float healthMult = 1f, float damageMult = 1f)
    {
        return GetEnemy(prefab, position, difficulty, healthMult, damageMult, null);
    }

    /// <summary>
    /// Get an enemy from the pool, tracked to a specific player for per-player caps.
    /// </summary>
    public GameObject GetEnemy(GameObject prefab, Vector3 position, float difficulty, float healthMult, float damageMult, ulong? targetClientId)
    {
        if (!IsServer) return null;

        int prefabId = prefab.GetHashCode();
        GameObject enemy = null;

        // Try to get from pool
        if (pools.ContainsKey(prefabId) && pools[prefabId].Count > 0)
        {
            enemy = pools[prefabId].Dequeue();
            enemy.transform.position = position;
            enemy.SetActive(true);
            
            // Re-spawn on network
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }
        else
        {
            // Create new enemy
            enemy = Instantiate(prefab, position, Quaternion.identity);
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }

        // Initialize difficulty with wave modifiers
        if (enemy.TryGetComponent(out SwarmController swarmScript))
        {
            swarmScript.InitializeDifficulty(difficulty, healthMult, damageMult);
        }

        // Reset health
        if (enemy.TryGetComponent(out Health health))
        {
            health.ResetHealth();
        }

        activeEnemies.Add(enemy);
        
        // Track per-player if client ID provided
        if (targetClientId.HasValue)
        {
            if (!enemiesPerPlayer.ContainsKey(targetClientId.Value))
            {
                enemiesPerPlayer[targetClientId.Value] = new List<GameObject>();
            }
            enemiesPerPlayer[targetClientId.Value].Add(enemy);
        }
        
        return enemy;
    }

    /// <summary>
    /// Return an enemy to the pool (instead of destroying).
    /// </summary>
    public void ReturnEnemy(GameObject enemy, GameObject prefab)
    {
        if (!IsServer) return;

        int prefabId = prefab.GetHashCode();

        // Despawn from network
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(false); // Don't destroy, just despawn
        }

        enemy.SetActive(false);

        // Return to pool (if not over max size)
        if (!pools.ContainsKey(prefabId))
        {
            pools[prefabId] = new Queue<GameObject>();
        }

        if (pools[prefabId].Count < maxPoolSize)
        {
            pools[prefabId].Enqueue(enemy);
        }
        else
        {
            // Pool full, destroy excess
            Destroy(enemy);
        }

        activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// Get current count of active enemies (global).
    /// </summary>
    public int GetActiveEnemyCount()
    {
        // Clean up null references
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }

    /// <summary>
    /// Get count of enemies spawned for a specific player.
    /// </summary>
    public int GetEnemyCountForPlayer(ulong clientId)
    {
        if (!enemiesPerPlayer.ContainsKey(clientId))
        {
            return 0;
        }
        
        // Clean up null/destroyed references
        enemiesPerPlayer[clientId].RemoveAll(e => e == null || !e.activeInHierarchy);
        return enemiesPerPlayer[clientId].Count;
    }

    /// <summary>
    /// Pre-warm pools with initial enemies.
    /// </summary>
    public void PrewarmPool(GameObject prefab, int count)
    {
        if (!IsServer) return;

        int prefabId = prefab.GetHashCode();
        if (!pools.ContainsKey(prefabId))
        {
            pools[prefabId] = new Queue<GameObject>();
        }

        for (int i = 0; i < count && pools[prefabId].Count < maxPoolSize; i++)
        {
            GameObject enemy = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            enemy.SetActive(false);
            pools[prefabId].Enqueue(enemy);
        }

        Debug.Log($"[EnemyPool] Pre-warmed {count} enemies of type {prefab.name}");
    }
}
