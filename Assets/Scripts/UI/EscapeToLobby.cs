using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple escape handler - pressing ESC returns to lobby selection.
/// If the player is the host, the lobby is also deleted.
/// Attach to a persistent GameObject in the game scene.
/// </summary>
public class EscapeToLobby : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string menuSceneName = "01_MainMenu";

    private bool isReturningToMenu = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isReturningToMenu)
        {
            ReturnToLobby();
        }
    }

    public async void ReturnToLobby()
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;

        Debug.Log("[EscapeToLobby] ESC pressed - returning to lobby...");

        // If we're the host, delete the lobby first
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            // Find the LobbyBeat component to get lobby ID
            LobbyBeat lobbyBeat = NetworkManager.Singleton.GetComponent<LobbyBeat>();
            if (lobbyBeat != null && !string.IsNullOrEmpty(lobbyBeat.LobbyId))
            {
                try
                {
                    Debug.Log($"[EscapeToLobby] Host leaving - deleting lobby {lobbyBeat.LobbyId}");
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyBeat.LobbyId);
                    Debug.Log("[EscapeToLobby] Lobby deleted successfully.");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[EscapeToLobby] Failed to delete lobby: {e.Message}");
                }
            }
        }

        // Shutdown network manager before loading menu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
