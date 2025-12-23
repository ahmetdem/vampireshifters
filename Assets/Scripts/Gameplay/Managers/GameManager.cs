using Unity.Netcode;
using UnityEngine;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public const int WIN_LEVEL = 100;
    public const int PVP_WIN_LEVEL_BOOST = 5;

    public NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> WinnerId = new NetworkVariable<ulong>(0);

    // Event for UI to subscribe to
    public static event Action<ulong> OnGameWon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when any player levels up. Checks if they've reached the win level.
    /// </summary>
    public void CheckWinCondition(ulong playerId, int newLevel)
    {
        if (!IsServer) return;
        if (IsGameOver.Value) return;

        Debug.Log($"[GameManager] Checking win condition for Player {playerId} at Level {newLevel}");

        if (newLevel >= WIN_LEVEL)
        {
            TriggerGameWin(playerId);
        }
    }

    /// <summary>
    /// Called when a player wins the game (reached level 100).
    /// </summary>
    private void TriggerGameWin(ulong winnerId)
    {
        if (!IsServer) return;
        if (IsGameOver.Value) return;

        Debug.Log($"[GameManager] >>> GAME WON by Player {winnerId}! <<<");

        IsGameOver.Value = true;
        WinnerId.Value = winnerId;

        // Notify all clients
        ShowGameWonClientRpc(winnerId);
    }

    [ClientRpc]
    private void ShowGameWonClientRpc(ulong winnerId)
    {
        Debug.Log($"[GameManager] Game Won Event Received! Winner: {winnerId}");
        
        // Trigger UI event
        OnGameWon?.Invoke(winnerId);

        // Show the game over UI
        if (GameOverUI.Instance != null)
        {
            bool isLocalPlayerWinner = NetworkManager.Singleton.LocalClientId == winnerId;
            GameOverUI.Instance.ShowGameOver(winnerId, isLocalPlayerWinner);
        }
    }
}
