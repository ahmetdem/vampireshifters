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
        NetworkObject winnerObject = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.GetComponent<Health>().currentHealth.Value > 0)
            {
                aliveCount++;
                lastSurvivorId = client.ClientId;
                winnerObject = client.PlayerObject;
            }
        }

        if (aliveCount <= 1 && winnerObject != null)
        {
            Debug.Log($"[PvP] Winner Found: {lastSurvivorId}");
            IsPvPActive.Value = false;

            // Grant +5 level boost to winner
            if (winnerObject.TryGetComponent(out PlayerEconomy economy))
            {
                Debug.Log($"[PvP] Granting {GameManager.PVP_WIN_LEVEL_BOOST} bonus levels to winner!");
                economy.AddLevels(GameManager.PVP_WIN_LEVEL_BOOST);
            }

            // Return all players to forest and resume gameplay
            EndPvPAndReturnToForest();
        }
    }

    /// <summary>
    /// Ends PvP mode, teleports all players back to forest, and resumes normal gameplay.
    /// </summary>
    private void EndPvPAndReturnToForest()
    {
        Debug.Log("[PvP] Returning all players to Forest Arena...");

        // 1. Teleport all surviving players back to forest
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            // Get a random spawn position in the forest
            Vector3 forestPos = Vector3.zero;
            if (ConnectionHandler.Instance != null)
            {
                forestPos = ConnectionHandler.Instance.GetRandomSpawnPosition();
            }
            else
            {
                // Fallback: random position near origin
                forestPos = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0f);
            }

            ClientRpcParams clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { client.ClientId } }
            };

            ReturnToForestClientRpc(forestPos, clientParams);
        }

        // 2. Resume enemy spawning
        if (mainEnemySpawner != null)
        {
            mainEnemySpawner.StartSpawning();
        }

        // 3. Reset timer for next potential PvP event
        timerActive = true;
        matchTimer = 0f;

        Debug.Log("[PvP] Forest gameplay resumed!");
    }

    [ClientRpc]
    private void ReturnToForestClientRpc(Vector3 pos, ClientRpcParams clientRpcParams = default)
    {
        // 1. Disable the PvP Arena Camera
        if (pvpArenaCamera != null)
        {
            pvpArenaCamera.gameObject.SetActive(false);
        }

        // 2. Teleport the local player
        if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out Rigidbody2D rb))
        {
            rb.velocity = Vector2.zero;
            rb.transform.position = pos;
        }

        Debug.Log($"[PvP] Returned to Forest at {pos}");
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

    // Add this helper method
    public void DisablePvPCamera()
    {
        if (pvpArenaCamera != null)
        {
            pvpArenaCamera.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the PvP camera for a specific client (e.g., when they die during PvP).
    /// </summary>
    [ClientRpc]
    public void ResetPvPCameraForClientRpc(ulong clientId)
    {
        // Only the dead player should reset their camera
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        if (pvpArenaCamera != null)
        {
            pvpArenaCamera.gameObject.SetActive(false);
            Debug.Log($"[PvP] Camera reset for dead player {clientId}");
        }
    }
}
