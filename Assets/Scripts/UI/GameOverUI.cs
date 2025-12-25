using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Image resultImage;

    [Header("Result Images")]
    [SerializeField] private Sprite winnerSprite;
    [SerializeField] private Sprite loserSprite;

    [Header("Canvases to Hide")]
    [Tooltip("Other canvases/UI elements to hide when game over is shown")]
    [SerializeField] private GameObject[] canvasesToHide;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        
        // Setup button listener
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        }
    }

    /// <summary>
    /// Shows the game over screen and hides other UI.
    /// </summary>
    /// <param name="winnerId">The client ID of the winner</param>
    /// <param name="isLocalPlayerWinner">True if the local player won</param>
    public void ShowGameOver(ulong winnerId, bool isLocalPlayerWinner)
    {
        if (panel == null) return;

        // Hide other canvases/UI elements
        foreach (var canvas in canvasesToHide)
        {
            if (canvas != null)
            {
                canvas.SetActive(false);
            }
        }

        // Show game over panel
        panel.SetActive(true);

        // Set the appropriate image
        if (isLocalPlayerWinner)
        {
            if (resultImage != null && winnerSprite != null) resultImage.sprite = winnerSprite;
        }
        else
        {
            if (resultImage != null && loserSprite != null) resultImage.sprite = loserSprite;
        }

        // Make sure the image is visible
        if (resultImage != null) resultImage.gameObject.SetActive(true);

        Debug.Log($"[GameOverUI] Displaying game over. Winner: {winnerId}, LocalWin: {isLocalPlayerWinner}");
    }

    /// <summary>
    /// Handle returning to lobby - disconnects from network and loads main menu.
    /// </summary>
    private void ReturnToLobby()
    {
        Debug.Log("[GameOverUI] Returning to lobby...");
        
        // Disconnect from network session
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[GameOverUI] Network shutdown complete.");
        }
        
        // Load the main menu scene (which has the lobby UI)
        SceneManager.LoadScene("01_MainMenu");
    }
}
