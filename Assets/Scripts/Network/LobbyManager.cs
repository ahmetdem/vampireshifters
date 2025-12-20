using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Start()
    {
        createLobbyButton.onClick.AddListener(CreateLobby);
        refreshButton.onClick.AddListener(RefreshLobbyList);
    }

    private void Update()
    {
        if (currentLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = 15f;
                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private async void CreateLobby()
    {
        try
        {
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(3);
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Game Lobby", 4, options);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();
            lobbyPanel.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
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
                texts[0].text = lobby.Name;
                texts[1].text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

                newItem.GetComponentInChildren<Button>().onClick.AddListener(() => JoinLobby(lobby.Id));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async void JoinLobby(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            string joinCode = lobby.Data["joinCode"].Value;

            JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            lobbyPanel.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}
