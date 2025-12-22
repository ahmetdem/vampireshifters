using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;

    private Lobby currentLobby;
    private float heartbeatTimer;
    private const string GameSceneName = "02_GameArena";

    private void Start()
    {
        createLobbyButton.onClick.AddListener(CreateLobby);
        refreshButton.onClick.AddListener(RefreshLobbyList);
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    // Prepare the payload (Name + AuthID) for Connection Approval
    private void SetConnectionPayload()
    {
        var payload = new ConnectionPayload
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Unknown"),
            authId = AuthenticationService.Instance.PlayerId
        };

        string payloadJson = JsonUtility.ToJson(payload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
    }

    private async void CreateLobby()
    {
        try
        {
            // 1. Create Relay
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(4);
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2. Create Lobby with Join Code
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
        {
            { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
        };

            string playerName = PlayerPrefs.GetString("PlayerName", "Host");
            currentLobby = await LobbyService.Instance.CreateLobbyAsync($"{playerName}'s Lobby", 4, options);

            // NEW: Attach heartbeat to the NetworkManager so it survives scene loads
            if (NetworkManager.Singleton.TryGetComponent(out LobbyBeat oldBeat))
            {
                Destroy(oldBeat); // Clean up if one already exists
            }
            LobbyBeat heartbeat = NetworkManager.Singleton.gameObject.AddComponent<LobbyBeat>();
            heartbeat.Initialize(currentLobby.Id);

            // 3. Setup Transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "dtls"));

            // 4. Set Payload and Start Host
            SetConnectionPayload();

            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Create Lobby Failed: {e.Message}");
        }
    }
    private async void JoinLobby(string lobbyId)
    {
        try
        {
            // 1. Join Lobby
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            string joinCode = lobby.Data["joinCode"].Value;

            // 2. Join Relay
            JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // 3. Setup Transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(joinAllocation, "dtls"));

            // 4. Set Payload and Start Client
            SetConnectionPayload();
            NetworkManager.Singleton.StartClient();

            // Client does NOT use SceneManager.LoadScene.
            // NGO automatically syncs the client to the Host's active scene.
            lobbyPanel.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Join Lobby Failed: {e.Message}");
        }
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

            foreach (Transform child in container) Destroy(child.gameObject);

            foreach (Lobby lobby in lobbies.Results)
            {
                GameObject newItem = Instantiate(lobbyItemPrefab, container);
                TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();
                // Layout: [Lobby Name] [Player Count]
                texts[0].text = lobby.Name;
                texts[1].text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

                newItem.GetComponentInChildren<Button>().onClick.AddListener(() => JoinLobby(lobby.Id));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Refresh Failed: {e.Message}");
        }
    }
}
