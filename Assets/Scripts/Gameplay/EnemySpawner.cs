using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private GameObject swarmPrefab;
    [SerializeField] private float spawnInterval = 2f;

    [Header("Difficulty Scaling")]
    [SerializeField] private int baseMaxEnemies = 20;
    [SerializeField] private float difficultyMultiplier = 1.0f; // Set this higher (e.g., 5 or 10) for fast testing
    [SerializeField] private float extraCapPerMinute = 10f;
    [SerializeField] private int baseBurstCount = 1;

    [Header("Viewport Logic")]
    [SerializeField] private float minSpawnDistance = 18f;
    [SerializeField] private float maxSpawnDistance = 28f;

    private float timer;
    private float timeElapsed;
    private bool isSpawningStopped = false;

    private void Start()
    {
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("CRITICAL ERROR: EnemySpawner is missing a 'NetworkObject' component!");
        }

        if (swarmPrefab == null)
        {
            Debug.LogError("CRITICAL ERROR: EnemySpawner has no 'Swarm Prefab' assigned!");
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
        if (swarmPrefab == null) return;

        float minutes = timeElapsed / 60f;

        int currentMaxEnemies = Mathf.RoundToInt(baseMaxEnemies + (extraCapPerMinute * minutes * difficultyMultiplier));
        int currentBurst = Mathf.RoundToInt(baseBurstCount + (minutes * difficultyMultiplier));

        float currentDifficulty = 1.0f + (minutes * difficultyMultiplier);

        int currentCount = FindObjectsOfType<SwarmController>().Length;
        if (currentCount >= currentMaxEnemies)
        {
            return;
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            for (int i = 0; i < currentBurst; i++)
            {
                if (currentCount + i >= currentMaxEnemies) break;

                SpawnEnemyAround(client.PlayerObject.transform.position, currentDifficulty);
            }
        }
    }
    private void SpawnEnemyAround(Vector3 center, float currentDifficulty)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = center + (Vector3)(randomDir * distance);

        GameObject enemyObj = Instantiate(swarmPrefab, spawnPos, Quaternion.identity);
        enemyObj.GetComponent<NetworkObject>().Spawn();

        if (enemyObj.TryGetComponent(out SwarmController swarmScript))
        {
            swarmScript.InitializeDifficulty(currentDifficulty);
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
