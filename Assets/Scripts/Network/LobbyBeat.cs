using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbyBeat : MonoBehaviour
{
    private string _lobbyId;
    private float _timer = 15f;
    private float _diagnosticTimer = 5f;
    private bool _hasLoggedDiagnostic = false;

    public void Initialize(string lobbyId)
    {
        _lobbyId = lobbyId;
        Debug.Log($"[LobbyBeat] Initialized with lobby ID: {lobbyId}");
    }

    private void Update()
    {
        // Diagnostic logging (once after scene settles)
        if (!_hasLoggedDiagnostic)
        {
            _diagnosticTimer -= Time.deltaTime;
            if (_diagnosticTimer < 0f)
            {
                _hasLoggedDiagnostic = true;
                LogNetworkDiagnostics();
            }
        }

        if (string.IsNullOrEmpty(_lobbyId)) return;

        _timer -= Time.deltaTime;
        if (_timer < 0f)
        {
            _timer = 15f;
            Debug.Log("[LobbyBeat] Sending Lobby Heartbeat...");
            LobbyService.Instance.SendHeartbeatPingAsync(_lobbyId);
        }
    }

    private void LogNetworkDiagnostics()
    {
        Debug.Log("=== [LobbyBeat] Network Diagnostics ===");
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[LobbyBeat] NetworkManager.Singleton is NULL!");
            return;
        }

        var nm = NetworkManager.Singleton;
        Debug.Log($"[LobbyBeat] IsHost: {nm.IsHost}, IsServer: {nm.IsServer}, IsClient: {nm.IsClient}, IsListening: {nm.IsListening}");
        
        // ConnectedClients is only accessible on server
        if (nm.IsServer)
        {
            Debug.Log($"[LobbyBeat] Connected Clients: {nm.ConnectedClients.Count}");
        }

        var transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            Debug.Log($"[LobbyBeat] Transport Protocol: {transport.Protocol}");
            Debug.Log($"[LobbyBeat] Transport ConnectionData: Address={transport.ConnectionData.Address}, Port={transport.ConnectionData.Port}");
        }
        else
        {
            Debug.LogError("[LobbyBeat] UnityTransport component not found!");
        }
        
        Debug.Log("=== End Network Diagnostics ===");
    }
}
