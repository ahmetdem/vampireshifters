using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Quest 14: Server-authoritative leaderboard manager using NetworkList with custom INetworkSerializable type.
/// This replaces the old FindObjectsOfType approach with proper networked state.
/// </summary>
public class LeaderboardManager : NetworkBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // NetworkList of custom serializable entries - synced to all clients automatically
    public NetworkList<LeaderboardEntry> Entries;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 1.0f;
    private float nextUpdateTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // NetworkList must be initialized in Awake, before OnNetworkSpawn
        Entries = new NetworkList<LeaderboardEntry>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initial population
            RefreshLeaderboard();
        }
    }

    private void Update()
    {
        // Server periodically updates the leaderboard
        if (IsServer && Time.time >= nextUpdateTime)
        {
            RefreshLeaderboard();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    /// <summary>
    /// Server-only: Refreshes the NetworkList with current player data.
    /// </summary>
    private void RefreshLeaderboard()
    {
        if (!IsServer) return;

        // Build a temporary list of current entries
        Dictionary<ulong, LeaderboardEntry> currentEntries = new Dictionary<ulong, LeaderboardEntry>();

        // Find all players by their PlayerNetworkState component
        PlayerNetworkState[] allPlayers = FindObjectsOfType<PlayerNetworkState>();

        foreach (var networkState in allPlayers)
        {
            if (networkState == null) continue;

            NetworkObject netObj = networkState.GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned) continue;

            ulong clientId = netObj.OwnerClientId;
            string playerName = networkState.playerName.Value.ToString();
            if (string.IsNullOrEmpty(playerName)) playerName = $"Player {clientId}";

            int coins = 0;
            int level = 1;

            var economy = networkState.GetComponent<PlayerEconomy>();
            if (economy != null)
            {
                coins = economy.totalCoins.Value;
                level = economy.currentLevel.Value;
            }

            int deaths = 0;
            if (ConnectionHandler.Instance != null)
            {
                deaths = ConnectionHandler.Instance.GetDeathCount(clientId);
            }

            currentEntries[clientId] = new LeaderboardEntry(clientId, playerName, coins, level, deaths);
        }

        // Update the NetworkList efficiently
        // Remove entries for disconnected players
        for (int i = Entries.Count - 1; i >= 0; i--)
        {
            if (!currentEntries.ContainsKey(Entries[i].ClientId))
            {
                Entries.RemoveAt(i);
            }
        }

        // Update existing or add new entries
        foreach (var kvp in currentEntries)
        {
            int existingIndex = FindEntryIndex(kvp.Key);
            if (existingIndex >= 0)
            {
                // Update if changed (IEquatable handles comparison)
                if (!Entries[existingIndex].Equals(kvp.Value))
                {
                    Entries[existingIndex] = kvp.Value;
                }
            }
            else
            {
                // Add new entry
                Entries.Add(kvp.Value);
            }
        }
    }

    private int FindEntryIndex(ulong clientId)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].ClientId == clientId)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns a sorted list of entries for UI display.
    /// Can be called by any client since NetworkList is synced.
    /// </summary>
    public List<LeaderboardEntry> GetSortedEntries()
    {
        List<LeaderboardEntry> sorted = new List<LeaderboardEntry>();
        foreach (var entry in Entries)
        {
            sorted.Add(entry);
        }

        // Sort by Level descending, then Deaths ascending
        sorted.Sort((a, b) =>
        {
            int levelCompare = b.Level.CompareTo(a.Level);
            if (levelCompare != 0) return levelCompare;
            return a.Deaths.CompareTo(b.Deaths);
        });

        return sorted;
    }
}
