using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine; // Required for Cinemachine

public class PvPDirector : NetworkBehaviour
{
    public static PvPDirector Instance;

    [Header("Setup")]
    [SerializeField] private Transform pvpArenaSpawnPoint;
    [SerializeField] private EnemySpawner mainEnemySpawner;

    // NEW: The camera that covers the PvP map
    [SerializeField] private CinemachineVirtualCamera pvpArenaCamera;

    [Header("Settings")]
    public NetworkVariable<bool> IsPvPActive = new NetworkVariable<bool>(false);
    [SerializeField] private float timeUntilForcedPvP = 1200f; // 20 mins

    private float matchTimer;
    private bool timerActive = true;

    private void Awake()
    {
        Instance = this;
        // Ensure camera is off by default
        if (pvpArenaCamera != null) pvpArenaCamera.gameObject.SetActive(false);
    }

    private void TeleportPlayersToArena()
    {
        int index = 0;
        int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        float radius = 10f; // Distance from center

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            // Calculate circle formation
            float angle = index * Mathf.PI * 2f / totalPlayers;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 targetPos = pvpArenaSpawnPoint.position + offset;

            ClientRpcParams clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { client.ClientId } }
            };

            TeleportClientRpc(targetPos, clientParams);
            index++;
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 pos, ClientRpcParams clientRpcParams = default)
    {
        // 1. Activate the Static Arena Camera
        if (pvpArenaCamera != null)
        {
            pvpArenaCamera.gameObject.SetActive(true);
        }

        // 2. Teleport the local player
        if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out Rigidbody2D rb))
        {
            rb.velocity = Vector2.zero;
            rb.transform.position = pos;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Timer Logic
        if (timerActive)
        {
            matchTimer += Time.deltaTime;
            if (matchTimer >= timeUntilForcedPvP) StartPvPEvent();
        }

        // Win Condition
        if (IsPvPActive.Value)
        {
            CheckForWinner();
        }
    }

    private void CheckForWinner()
    {
        int aliveCount = 0;
        ulong lastSurvivorId = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.GetComponent<Health>().currentHealth.Value > 0)
            {
                aliveCount++;
                lastSurvivorId = client.ClientId;
            }
        }

        if (aliveCount <= 1)
        {
            // Trigger Game Over Logic here later
            Debug.Log($"Winner Found: {lastSurvivorId}");
            IsPvPActive.Value = false;
        }
    }

    // Add this inside PvPDirector class
    private void DespawnAllEnemies()
    {
        // Find every active SwarmController (Logic Object)
        SwarmController[] enemies = FindObjectsOfType<SwarmController>();

        Debug.Log($"[PvP] Nuke initiated. Destroying {enemies.Length} enemies.");

        foreach (var enemy in enemies)
        {
            if (enemy.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    // Update your StartPvPEvent method
    public void StartPvPEvent()
    {
        if (!IsServer) return;

        Debug.Log(">>> PVP MODE STARTED <<<");
        IsPvPActive.Value = true;
        timerActive = false;

        if (mainEnemySpawner != null) mainEnemySpawner.StopSpawning();

        // CALL THE NUKE
        DespawnAllEnemies();

        TeleportPlayersToArena();
    }
}
