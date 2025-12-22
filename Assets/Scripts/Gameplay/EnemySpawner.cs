using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject swarmPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxEnemies = 20;

    [Header("Viewport Logic")]
    [SerializeField] private float minSpawnDistance = 10f; // Don't spawn on top of player
    [SerializeField] private float maxSpawnDistance = 20f; // Don't spawn too far

    private float timer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // Simple Wave Logic: Spawn every X seconds if under cap
        if (timer <= 0f)
        {
            timer = spawnInterval;
            SpawnBatch();
        }
    }

    private void SpawnBatch()
    {
        // Optimization: Don't spawn if we hit the limit
        // (In a real game, you'd track a list of active enemies to get the count)
        if (FindObjectsOfType<SwarmController>().Length >= maxEnemies) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            SpawnEnemyAround(client.PlayerObject.transform.position);
        }
    }

    private void SpawnEnemyAround(Vector3 center)
    {
        // Get a random point within the "Doughnut" shape (between min and max distance)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = center + (Vector3)(randomDir * distance);

        GameObject enemy = Instantiate(swarmPrefab, spawnPos, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
    }

    // Add this inside EnemySpawner.cs
    public void StopSpawning()
    {
        CancelInvoke(); // Stops all InvokeRepeating timers
        enabled = false; // Stops the Update loop
        Debug.Log("Enemy Spawning Stopped.");
    }
}
