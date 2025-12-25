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
        if (LeaderboardManager.Instance == null) return;

        // Quest 14: Use NetworkList from LeaderboardManager instead of FindObjectsOfType
        List<LeaderboardEntry> networkEntries = LeaderboardManager.Instance.GetSortedEntries();
        List<UILeaderboardEntry> entries = new List<UILeaderboardEntry>();

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < networkEntries.Count; i++)
        {
            var entry = networkEntries[i];
            entries.Add(new UILeaderboardEntry
            {
                Rank = i + 1,
                Name = entry.PlayerName.ToString(),
                Level = entry.Level,
                Deaths = entry.Deaths,
                IsLocalPlayer = (entry.ClientId == localClientId)
            });
        }

        // Update UI
        RenderEntries(entries);
    }

    private void RenderEntries(List<UILeaderboardEntry> entries)
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

    private void UpdateRow(GameObject row, UILeaderboardEntry data)
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
            texts[3].text = $"{data.Deaths}";

            // Highlight local player
            Color textColor = data.IsLocalPlayer ? Color.yellow : Color.white;
            foreach (var t in texts) t.color = textColor;
        }
    }

    // UI-only data class (renamed to avoid conflict with network LeaderboardEntry struct)
    private class UILeaderboardEntry
    {
        public int Rank;
        public string Name;
        public int Level;
        public int Deaths;
        public bool IsLocalPlayer;
    }
}
