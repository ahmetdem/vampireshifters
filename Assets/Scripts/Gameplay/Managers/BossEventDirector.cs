using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Cinemachine; // Required for Camera switching

public class BossEventDirector : NetworkBehaviour
{
    public static BossEventDirector Instance;

    [Header("Setup")]
    [SerializeField] private Transform arenaSpawnPoint;
    [SerializeField] private GameObject bossPrefab;

    // NEW: Reference to the main spawner so we can disable it
    [SerializeField] private EnemySpawner mainEnemySpawner;

    // NEW: Reference to a static camera that covers the whole arena
    [SerializeField] private CinemachineVirtualCamera bossArenaCamera;

    [Header("Settings")]
    public float bossTimerDuration = 300f;
    private float currentTimer;
    private bool eventStarted = false;

    private void Awake()
    {
        Instance = this;
        // Ensure the boss camera is off by default
        if (bossArenaCamera != null) bossArenaCamera.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentTimer = 0f;
    }

    private void Update()
    {
        if (!IsServer || eventStarted) return;

        currentTimer += Time.deltaTime;
        if (currentTimer >= bossTimerDuration)
        {
            StartBossEvent();
        }
    }

    public void ForceStartEvent()
    {
        if (IsServer && !eventStarted) StartBossEvent();
    }

    private void StartBossEvent()
    {
        eventStarted = true;
        Debug.Log(">>> BOSS EVENT STARTED <<<");

        // 1. STOP NORMAL ENEMY SPAWNS
        if (mainEnemySpawner != null)
        {
            // You need to add this public method to your EnemySpawner script!
            mainEnemySpawner.StopSpawning();
        }

        // 2. TELEPORT & FIX CAMERA (Client Side)
        TeleportAndSwitchCameraClientRpc(arenaSpawnPoint.position);

        // 3. SPAWN BOSS
        SpawnBoss();
    }

    [ClientRpc]
    private void TeleportAndSwitchCameraClientRpc(Vector3 pos)
    {
        // A. Enable the Boss Camera
        // Because it has higher priority (setup below), Cinemachine will snap to it
        if (bossArenaCamera != null)
        {
            bossArenaCamera.gameObject.SetActive(true);
        }

        // B. Teleport Local Player
        // We only move the player belonging to this specific client
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (player.TryGetComponent(out Rigidbody2D rb)) rb.velocity = Vector2.zero;
            player.transform.position = pos;
        }
    }

    private void SpawnBoss()
    {
        Vector3 bossPos = arenaSpawnPoint.position + new Vector3(0, 5, 0);
        GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.identity);
        boss.GetComponent<NetworkObject>().Spawn();
    }
}
