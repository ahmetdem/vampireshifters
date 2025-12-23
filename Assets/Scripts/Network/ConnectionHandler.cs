using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ConnectionHandler : MonoBehaviour
{
    public static ConnectionHandler Instance { get; private set; }

    private Dictionary<ulong, string> clientNames = new Dictionary<ulong, string>();
    private Dictionary<ulong, int> deathCounts = new Dictionary<ulong, int>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    public void HandlePlayerDeath(ulong clientId)
    {
        if (!deathCounts.ContainsKey(clientId)) deathCounts[clientId] = 0;
        deathCounts[clientId]++;

        // Quest 12: Respawn timer increases with every death
        float delay = 2.0f + (deathCounts[clientId] * 1.5f);

        Debug.Log($"[ConnectionHandler] Client {clientId} died. Total deaths: {deathCounts[clientId]}. Respawning in {delay}s");

        StartCoroutine(RespawnRoutine(clientId, delay));

        // Reset any active event cameras
        if (BossEventDirector.Instance != null)
        {
            BossEventDirector.Instance.ResetCameraClientRpc();
        }

        // Also reset PvP camera if player died in PvP arena
        if (PvPDirector.Instance != null)
        {
            PvPDirector.Instance.ResetPvPCameraForClientRpc(clientId);
        }
    }

    private IEnumerator RespawnRoutine(ulong clientId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (NetworkManager.Singleton != null)
        {
            // Quest 11: Random spawn points
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

            GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            Debug.Log($"[ConnectionHandler] Client {clientId} successfully respawned at {spawnPos}");
        }
    }

    public Vector3 GetRandomSpawnPosition()
    {
        // Simply pick a random point within the radius
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;

        // Return that position
        return new Vector3(randomPoint.x, randomPoint.y, 0f);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        byte[] payloadBytes = request.Payload;
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(payloadJson);

        ulong id = request.ClientNetworkId;

        if (!clientNames.ContainsKey(id)) clientNames.Add(id, payload.playerName);
        if (!deathCounts.ContainsKey(id)) deathCounts.Add(id, 0);

        Vector3 spawnPos = GetRandomSpawnPosition();

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = spawnPos;
        response.Rotation = Quaternion.identity;

        Debug.Log($"[ConnectionHandler] Approved {payload.playerName} (ID: {id})");
    }

    public string GetPlayerName(ulong clientId)
    {
        return clientNames.TryGetValue(clientId, out string name) ? name : "Unknown";
    }

    private void OnServerStarted()
    {
        if (!clientNames.ContainsKey(NetworkManager.ServerClientId))
        {
            clientNames.Add(NetworkManager.ServerClientId, PlayerPrefs.GetString("PlayerName", "Host"));
            deathCounts.Add(NetworkManager.ServerClientId, 0);
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        clientNames.Remove(clientId);
        deathCounts.Remove(clientId);
        Debug.Log($"[ConnectionHandler] Cleaned up data for Client {clientId}");
    }
}
