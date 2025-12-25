using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject rowPrefab;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showAlways = false;

    private float nextUpdate;
    private bool isVisible = false;

    // Cache rows to reuse them instead of destroying/instantiating every frame
    private List<GameObject> spawnedRows = new List<GameObject>();

    private void Start()
    {
        if (showAlways)
        {
            isVisible = true;
            leaderboardPanel.SetActive(true);
        }
        else
        {
            leaderboardPanel.SetActive(false);
        }
    }

    private void Update()
    {
        HandleInput();

        if (isVisible && Time.time >= nextUpdate)
        {
            UpdateLeaderboard();
            nextUpdate = Time.time + updateInterval;
        }
    }

    private void HandleInput()
    {
        if (showAlways) return;

        // Toggle with TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isVisible = true;
            leaderboardPanel.SetActive(true);
            UpdateLeaderboard(); // Immediate update on open
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            isVisible = false;
            leaderboardPanel.SetActive(false);
        }
    }

    private void UpdateLeaderboard()
    {
        if (NetworkManager.Singleton == null) return;

        // 1. Collect Data
        // NOTE: ConnectedClientsList is SERVER-ONLY in Netcode for GameObjects!
        // Instead, find all player objects in scene using their PlayerNetworkState component
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        // Find all players by their PlayerNetworkState component
        PlayerNetworkState[] allPlayers = FindObjectsOfType<PlayerNetworkState>();
        
        foreach (var networkState in allPlayers)
        {
            if (networkState == null) continue;
            
            // Get the NetworkObject to determine ownership
            NetworkObject netObj = networkState.GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned) continue;
            
            var economy = networkState.GetComponent<PlayerEconomy>();

            string pName = networkState.playerName.Value.ToString();
            if (string.IsNullOrEmpty(pName)) pName = $"Player {netObj.OwnerClientId}";
            
            int pLevel = economy != null ? economy.currentLevel.Value : 1;
            
            // Death count is server-only data, use 0 on clients
            int pDeaths = 0;
            if (ConnectionHandler.Instance != null && NetworkManager.Singleton.IsServer)
            {
                pDeaths = ConnectionHandler.Instance.GetDeathCount(netObj.OwnerClientId);
            }
            
            bool isMe = (netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId);

            entries.Add(new LeaderboardEntry
            {
                Rank = 0, // Assigned after sort
                Name = pName,
                Level = pLevel,
                Deaths = pDeaths,
                IsLocalPlayer = isMe
            });
        }

        // 2. Sort by Level descending, then by Deaths ascending (fewer deaths break ties)
        entries = entries.OrderByDescending(x => x.Level).ThenBy(x => x.Deaths).ToList();

        // 3. Assign Ranks
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        // 4. Update UI
        RenderEntries(entries);
    }

    private void RenderEntries(List<LeaderboardEntry> entries)
    {
        // Ensure we have enough rows
        while (spawnedRows.Count < entries.Count)
        {
            GameObject newRow = Instantiate(rowPrefab, contentParent);
            spawnedRows.Add(newRow);
        }

        // Hide unused rows
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (i < entries.Count)
            {
                spawnedRows[i].SetActive(true);
                UpdateRow(spawnedRows[i], entries[i]);
            }
            else
            {
                spawnedRows[i].SetActive(false);
            }
        }
    }

    private void UpdateRow(GameObject row, LeaderboardEntry data)
    {
        // Assumption: Row prefab has 4 TextMeshProUGUI components in order: Rank, Name, Level, Deaths
        // Or we can verify by name using transform.Find if needed. 
        // For robustness, let's try to find by name, but fallback to GetComponentsInChildren.
        
        TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

        // We expect at least 4 text components.
        // Index 0: Rank
        // Index 1: Name
        // Index 2: Level
        // Index 3: Deaths
        
        if (texts.Length >= 4)
        {
            texts[0].text = $"#{data.Rank}";
            texts[1].text = data.Name;
            texts[2].text = $"Lv.{data.Level}";
            texts[3].text = $"Deaths: {data.Deaths}";

            // Highlight local player
            Color textColor = data.IsLocalPlayer ? Color.yellow : Color.white;
            foreach (var t in texts) t.color = textColor;
        }
    }

    private class LeaderboardEntry
    {
        public int Rank;
        public string Name;
        public int Level;
        public int Deaths;
        public bool IsLocalPlayer;
    }
}
