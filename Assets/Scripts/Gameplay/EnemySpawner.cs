using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Wave Configuration")]
    [Tooltip("Configure enemy waves here. Enemies spawn based on time.")]
    [SerializeField] private WaveData[] waves;
    
    [Tooltip("Fallback prefab if no waves are configured")]
    [SerializeField] private GameObject fallbackPrefab;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int baseBurstCount = 1;
    [SerializeField] private float difficultyMultiplier = 1.0f;

    [Header("Enemy Cap (Performance)")]
    [Tooltip("Hard limit on total enemies. After this, difficulty scales via damage/health only.")]
    [SerializeField] private int absoluteMaxEnemies = 50;
    
    [Header("Scaling")]
    [SerializeField] private int baseMaxEnemies = 20;
    [SerializeField] private float extraCapPerMinute = 10f;

    [Header("Spawn Distance (Camera Simulation)")]
    [Tooltip("Orthographic Size of the camera (Half Height). Default is 10.")]
    [SerializeField] private float referenceOrthographicSize = 10f;
    [Tooltip("Aspect Ratio of the camera (Width / Height). Default 16:9 = ~1.77.")]
    [SerializeField] private float referenceAspectRatio = 1.777f; 
    [Tooltip("Extra buffer distance outside the camera view.")]
    [SerializeField] private float spawnBuffer = 2f;

    [Header("Object Pooling")]
    [Tooltip("Enable object pooling for better performance (requires EnemyPool in scene)")]
    [SerializeField] private bool useObjectPooling = true;

    [Header("Map Bounds")]
    [Tooltip("Enable to prevent spawning outside map boundaries")]
    [SerializeField] private bool clampToMapBounds = true;
    [Tooltip("Half-width of the map (e.g., 1000 for a 2000x2000 map)")]
    [SerializeField] private float mapHalfWidth = 1000f;
    [Tooltip("Half-height of the map (e.g., 1000 for a 2000x2000 map)")]
    [SerializeField] private float mapHalfHeight = 1000f;
    [Tooltip("Padding inside map edge to prevent spawning right at the boundary")]
    [SerializeField] private float mapBoundsPadding = 5f;

    private float timer;
    private float timeElapsed;
    private bool isSpawningStopped = false;

    private void Start()
    {
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("CRITICAL ERROR: EnemySpawner is missing a 'NetworkObject' component!");
        }

        if ((waves == null || waves.Length == 0) && fallbackPrefab == null)
        {
            Debug.LogError("CRITICAL ERROR: EnemySpawner has no waves or fallback prefab assigned!");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            enabled = true;
            isSpawningStopped = false;
            timer = 2f;
            Debug.Log("Spawner Initialized on Server.");
        }
        else
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (isSpawningStopped) return;

        timeElapsed += Time.deltaTime;
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = spawnInterval;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        float minutes = timeElapsed / 60f;

        // Calculate global max enemies (scales with time, capped at absoluteMax)
        int scaledMax = Mathf.RoundToInt(baseMaxEnemies + (extraCapPerMinute * minutes * difficultyMultiplier));
        int totalMaxEnemies = Mathf.Min(scaledMax, absoluteMaxEnemies);
        
        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (playerCount == 0) return;
        
        int currentBurst = Mathf.RoundToInt(baseBurstCount + (minutes * difficultyMultiplier));
        float currentDifficulty = 1.0f + (minutes * difficultyMultiplier);

        // Get GLOBAL enemy count (not per-player - that was broken)
        int currentEnemyCount = 0;
        if (useObjectPooling && EnemyPool.Instance != null)
        {
            currentEnemyCount = EnemyPool.Instance.GetActiveEnemyCount();
        }
        else
        {
            currentEnemyCount = FindObjectsOfType<SwarmController>().Length;
        }

        // Skip if at global cap
        if (currentEnemyCount >= totalMaxEnemies) return;

        // Calculate how many we can spawn this wave
        int remainingCap = totalMaxEnemies - currentEnemyCount;
        int totalToSpawn = Mathf.Min(currentBurst * playerCount, remainingCap);
        
        // Spawn enemies distributed equally around ALL players (round-robin)
        List<WaveData> activeWaves = GetActiveWaves(minutes);
        List<NetworkClient> validClients = new List<NetworkClient>();
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                validClients.Add(client);
            }
        }
        
        if (validClients.Count == 0) return;
        
        int spawnedCount = 0;
        int clientIndex = 0;
        
        while (spawnedCount < totalToSpawn)
        {
            // Round-robin: pick next player
            NetworkClient client = validClients[clientIndex % validClients.Count];
            clientIndex++;
            
            // Pick wave and enemy
            WaveData selectedWave = PickWeightedWave(activeWaves);
            GameObject prefab = selectedWave != null ? selectedWave.GetRandomEnemy() : fallbackPrefab;
            
            if (prefab == null) continue;

            float healthMult = selectedWave?.healthMultiplier ?? 1f;
            float damageMult = selectedWave?.damageMultiplier ?? 1f;

            // Spawn just outside this player's view
            Vector3 spawnPos = GetCameraEdgeSpawnPosition(client.PlayerObject.transform.position);
            SpawnEnemyAt(spawnPos, prefab, currentDifficulty, healthMult, damageMult);
            
            spawnedCount++;
        }
    }

    private List<WaveData> GetActiveWaves(float currentMinutes)
    {
        List<WaveData> active = new List<WaveData>();
        
        if (waves == null) return active;

        foreach (var wave in waves)
        {
            if (wave != null && wave.IsActiveAt(currentMinutes))
            {
                active.Add(wave);
            }
        }

        return active;
    }

    private WaveData PickWeightedWave(List<WaveData> activeWaves)
    {
        if (activeWaves.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var wave in activeWaves)
        {
            totalWeight += wave.spawnWeight;
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var wave in activeWaves)
        {
            cumulative += wave.spawnWeight;
            if (random <= cumulative)
            {
                return wave;
            }
        }

        return activeWaves[0];
    }

    /// <summary>
    /// Get a spawn position just outside the player's calculated camera view.
    /// Uses reference camera size since server doesn't know client's actual camera state.
    /// </summary>
    private Vector3 GetCameraEdgeSpawnPosition(Vector3 playerPosition)
    {
        float camHeight = referenceOrthographicSize * 2f;
        float camWidth = camHeight * referenceAspectRatio;
        
        float halfHeight = camHeight / 2f;
        float halfWidth = camWidth / 2f;

        // Pick a random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = Random.Range(0, 4);
        Vector3 spawnPos = playerPosition;

        switch (edge)
        {
            case 0: // Top
                spawnPos.y += halfHeight + spawnBuffer;
                spawnPos.x += Random.Range(-halfWidth - spawnBuffer, halfWidth + spawnBuffer);
                break;
            case 1: // Right
                spawnPos.x += halfWidth + spawnBuffer;
                spawnPos.y += Random.Range(-halfHeight - spawnBuffer, halfHeight + spawnBuffer);
                break;
            case 2: // Bottom
                spawnPos.y -= halfHeight + spawnBuffer;
                spawnPos.x += Random.Range(-halfWidth - spawnBuffer, halfWidth + spawnBuffer);
                break;
            case 3: // Left
                spawnPos.x -= halfWidth + spawnBuffer;
                spawnPos.y += Random.Range(-halfHeight - spawnBuffer, halfHeight + spawnBuffer);
                break;
        }

        spawnPos.z = 0; // Keep on game plane

        // Clamp to map bounds if enabled
        if (clampToMapBounds)
        {
            float maxX = mapHalfWidth - mapBoundsPadding;
            float maxY = mapHalfHeight - mapBoundsPadding;
            spawnPos.x = Mathf.Clamp(spawnPos.x, -maxX, maxX);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -maxY, maxY);
        }

        return spawnPos;
    }

    private void SpawnEnemyAt(Vector3 spawnPos, GameObject prefab, float difficulty, float healthMult, float damageMult)
    {
        GameObject enemyObj;

        if (useObjectPooling && EnemyPool.Instance != null)
        {
            // Use object pooling with global tracking (no per-player tracking - it was broken)
            enemyObj = EnemyPool.Instance.GetEnemy(prefab, spawnPos, difficulty, healthMult, damageMult);
        }
        else
        {
            // Traditional instantiate
            enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemyObj.GetComponent<NetworkObject>().Spawn();

            if (enemyObj.TryGetComponent(out SwarmController swarmScript))
            {
                swarmScript.InitializeDifficulty(difficulty, healthMult, damageMult);
            }
        }
    }

    public void StartSpawning()
    {
        isSpawningStopped = false;
        enabled = true;
        timer = spawnInterval;
        Debug.Log("Enemy Spawning Resumed.");
    }

    public void StopSpawning()
    {
        isSpawningStopped = true;
        Debug.Log("Enemy Spawning Stopped.");
    }
}

