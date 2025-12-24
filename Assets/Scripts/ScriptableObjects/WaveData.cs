using UnityEngine;

/// <summary>
/// Defines a wave of enemies that spawns during a time window.
/// Create multiple WaveData assets and assign to EnemySpawner.
/// </summary>
[CreateAssetMenu(fileName = "NewWave", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Timing")]
    [Tooltip("When this wave starts spawning (in minutes)")]
    public float startTimeMinutes = 0f;
    
    [Tooltip("When this wave stops spawning (-1 = never stops)")]
    public float endTimeMinutes = -1f;

    [Header("Enemy Pool")]
    [Tooltip("Enemy prefabs that can spawn during this wave")]
    public GameObject[] enemyPrefabs;
    
    [Tooltip("Spawn weight - higher = more likely to be chosen")]
    [Range(0.1f, 10f)]
    public float spawnWeight = 1f;

    [Header("Difficulty Modifiers")]
    [Tooltip("Extra health multiplier for enemies in this wave")]
    public float healthMultiplier = 1f;
    
    [Tooltip("Extra damage multiplier for enemies in this wave")]
    public float damageMultiplier = 1f;

    /// <summary>
    /// Check if this wave is active at the given time.
    /// </summary>
    public bool IsActiveAt(float currentMinutes)
    {
        bool afterStart = currentMinutes >= startTimeMinutes;
        bool beforeEnd = endTimeMinutes < 0 || currentMinutes <= endTimeMinutes;
        return afterStart && beforeEnd;
    }

    /// <summary>
    /// Get a random enemy prefab from this wave's pool.
    /// </summary>
    public GameObject GetRandomEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }
}
