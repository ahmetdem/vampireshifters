using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Shows the game over screen.
    /// </summary>
    /// <param name="winnerId">The client ID of the winner</param>
    /// <param name="isLocalPlayerWinner">True if the local player won</param>
    public void ShowGameOver(ulong winnerId, bool isLocalPlayerWinner)
    {
        if (panel == null) return;

        panel.SetActive(true);

        if (isLocalPlayerWinner)
        {
            if (titleText != null) titleText.text = "VICTORY!";
            if (messageText != null) messageText.text = "You reached Level 100 and won the game!";
        }
        else
        {
            if (titleText != null) titleText.text = "GAME OVER";
            if (messageText != null) messageText.text = $"Player {winnerId} reached Level 100 and won!";
        }

        Debug.Log($"[GameOverUI] Displaying game over. Winner: {winnerId}, LocalWin: {isLocalPlayerWinner}");
    }
}
