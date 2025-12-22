using UnityEngine;
using Unity.Services.Lobbies;

public class LobbyBeat : MonoBehaviour
{
    private string _lobbyId;
    private float _timer = 15f;

    public void Initialize(string lobbyId)
    {
        _lobbyId = lobbyId;
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_lobbyId)) return;

        _timer -= Time.deltaTime;
        if (_timer < 0f)
        {
            _timer = 15f;
            Debug.Log("Sending Lobby Heartbeat...");
            LobbyService.Instance.SendHeartbeatPingAsync(_lobbyId);
        }
    }
}
