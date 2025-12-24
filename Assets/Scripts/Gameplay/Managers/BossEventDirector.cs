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
    public NetworkVariable<bool> isEventActive = new NetworkVariable<bool>(false);
    public bool IsEventActive => isEventActive.Value;

    private void Awake()
    {
        Instance = this;
        // Ensure the boss camera is off by default
        if (bossArenaCamera != null) bossArenaCamera.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentTimer = 0f;
            isEventActive.Value = false;
        }
    }

    private void Update()
    {
        if (!IsServer || isEventActive.Value) return;

        currentTimer += Time.deltaTime;
        if (currentTimer >= bossTimerDuration)
        {
            StartBossEvent();
        }
    }

    public void ForceStartEvent()
    {
        if (IsServer && !isEventActive.Value) StartBossEvent();
    }

    private void StartBossEvent()
    {
        isEventActive.Value = true;
        Debug.Log(">>> BOSS EVENT STARTED <<<");

        // 1. STOP NORMAL ENEMY SPAWNS
        if (mainEnemySpawner != null)
        {
            // You need to add this public method to your EnemySpawner script!
            mainEnemySpawner.StopSpawning();
        }

        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.IsPvPActive.Value = false; // Force PvP flag off
            PvPDirector.Instance.DisablePvPCamera();        // Force Camera off
        }

        // 2. TELEPORT & FIX CAMERA (Client Side)
        TeleportAndSwitchCameraClientRpc(arenaSpawnPoint.position);

        // 3. SPAWN BOSS
        SpawnBoss();
    }

    [ClientRpc]
    private void TeleportAndSwitchCameraClientRpc(Vector3 pos)
    {
        // A. Enable the Boss Camera with high priority
        // Because it has higher priority (setup below), Cinemachine will snap to it
        if (bossArenaCamera != null)
        {
            bossArenaCamera.gameObject.SetActive(true);
            bossArenaCamera.Priority = 20; // Higher than player camera
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

    [ClientRpc]
    public void ResetCameraClientRpc()
    {
        // Turn off the boss camera so the default follow camera takes priority again
        if (bossArenaCamera != null)
        {
            bossArenaCamera.gameObject.SetActive(false);
        }
    }

    public void OnBossDefeated()
    {
        // 1. Logic runs only on Server
        if (!IsServer) return;
        isEventActive.Value = false;

        Debug.Log(">>> BOSS DEFEATED! RETURNING TO FOREST <<<");

        if (mainEnemySpawner != null)
        {
            mainEnemySpawner.StartSpawning();
        }

        // 2. Loop through players on the Server
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // Server calculates a random spot for this specific player
            Vector3 targetPos = ConnectionHandler.Instance.GetRandomSpawnPosition();

            // Server whispers to this client: "Go to this specific spot"
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { client.ClientId }
                }
            };

            ReturnToForestClientRpc(targetPos, clientRpcParams);
        }
    }

    [ClientRpc]
    private void ReturnToForestClientRpc(Vector3 targetPos, ClientRpcParams clientRpcParams = default)
    {
        // Client just follows orders. No math, no checking lists.

        if (bossArenaCamera != null)
        {
            bossArenaCamera.gameObject.SetActive(false);
        }

        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.DisablePvPCamera();
        }

        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (player.TryGetComponent(out Rigidbody2D rb)) rb.velocity = Vector2.zero;

            // Move to the spot the server picked
            player.transform.position = targetPos;
        }
    }
}
