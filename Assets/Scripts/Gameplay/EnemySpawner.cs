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

    [Header("Spawn Distance (Camera-Based)")]
    [Tooltip("Extra distance beyond camera edge to spawn enemies")]
    [SerializeField] private float spawnBufferDistance = 3f;
    
    [Tooltip("Fallback distance if camera not available")]
    [SerializeField] private float fallbackSpawnDistance = 20f;

    [Header("Object Pooling")]
    [Tooltip("Enable object pooling for better performance (requires EnemyPool in scene)")]
    [SerializeField] private bool useObjectPooling = true;

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

        // Calculate scaled max, then clamp to absolute cap
        int scaledMax = Mathf.RoundToInt(baseMaxEnemies + (extraCapPerMinute * minutes * difficultyMultiplier));
        int currentMaxEnemies = Mathf.Min(scaledMax, absoluteMaxEnemies);
        int currentBurst = Mathf.RoundToInt(baseBurstCount + (minutes * difficultyMultiplier));

        float currentDifficulty = 1.0f + (minutes * difficultyMultiplier);

        // Get current enemy count
        int currentCount = useObjectPooling && EnemyPool.Instance != null 
            ? EnemyPool.Instance.GetActiveEnemyCount() 
            : FindObjectsOfType<SwarmController>().Length;

        if (currentCount >= currentMaxEnemies) return;

        // Get active waves for current time
        List<WaveData> activeWaves = GetActiveWaves(minutes);
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            for (int i = 0; i < currentBurst; i++)
            {
                if (currentCount + i >= currentMaxEnemies) break;

                // Pick wave and enemy
                WaveData selectedWave = PickWeightedWave(activeWaves);
                GameObject prefab = selectedWave != null ? selectedWave.GetRandomEnemy() : fallbackPrefab;
                
                if (prefab == null) continue;

                float healthMult = selectedWave?.healthMultiplier ?? 1f;
                float damageMult = selectedWave?.damageMultiplier ?? 1f;

                // Spawn just outside camera view
                Vector3 spawnPos = GetCameraEdgeSpawnPosition(client.PlayerObject.transform.position);
                SpawnEnemyAt(spawnPos, prefab, currentDifficulty, healthMult, damageMult);
            }
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
    /// Get a spawn position just outside the camera view.
    /// </summary>
    private Vector3 GetCameraEdgeSpawnPosition(Vector3 playerPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // Fallback to fixed distance if no camera
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            return playerPosition + (Vector3)(randomDir * fallbackSpawnDistance);
        }

        // Get camera bounds in world space
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // Pick a random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = Random.Range(0, 4);
        Vector3 spawnPos = cam.transform.position;

        switch (edge)
        {
            case 0: // Top
                spawnPos.y += camHeight + spawnBufferDistance;
                spawnPos.x += Random.Range(-camWidth, camWidth);
                break;
            case 1: // Right
                spawnPos.x += camWidth + spawnBufferDistance;
                spawnPos.y += Random.Range(-camHeight, camHeight);
                break;
            case 2: // Bottom
                spawnPos.y -= camHeight + spawnBufferDistance;
                spawnPos.x += Random.Range(-camWidth, camWidth);
                break;
            case 3: // Left
                spawnPos.x -= camWidth + spawnBufferDistance;
                spawnPos.y += Random.Range(-camHeight, camHeight);
                break;
        }

        spawnPos.z = 0; // Keep on game plane
        return spawnPos;
    }

    private void SpawnEnemyAt(Vector3 spawnPos, GameObject prefab, float difficulty, float healthMult, float damageMult)
    {
        GameObject enemyObj;

        if (useObjectPooling && EnemyPool.Instance != null)
        {
            // Use object pooling
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

