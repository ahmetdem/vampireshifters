using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quest 7: Handles automatic return to menu when disconnected from host.
/// Attach this to a persistent GameObject in the game scene.
/// </summary>
public class DisconnectHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string menuSceneName = "01_MainMenu";
    [SerializeField] private float disconnectMessageDuration = 3f;

    private bool isReturningToMenu = false;

    private void Start()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            // Client-side: Detect when we get disconnected
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            
            // Also handle transport failure (host crash, network loss)
            NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }
    }

    /// <summary>
    /// Called when a client disconnects. On clients, this fires when WE disconnect.
    /// </summary>
    private void OnClientDisconnect(ulong clientId)
    {
        // If we're the server, this is just a client leaving - ignore
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // If we're a client and our own clientId disconnected, we lost connection to host
        if (NetworkManager.Singleton != null && 
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[DisconnectHandler] Lost connection to host. Returning to menu...");
            ReturnToMenu("Connection to host lost");
        }
    }

    /// <summary>
    /// Called when the network transport fails completely.
    /// </summary>
    private void OnTransportFailure()
    {
        Debug.Log("[DisconnectHandler] Transport failure detected. Returning to menu...");
        ReturnToMenu("Network connection failed");
    }

    /// <summary>
    /// Safely returns to the main menu, cleaning up network state.
    /// </summary>
    private void ReturnToMenu(string reason)
    {
        if (isReturningToMenu) return; // Prevent multiple calls
        isReturningToMenu = true;

        Debug.Log($"[DisconnectHandler] Returning to menu. Reason: {reason}");

        // Shutdown network manager before loading menu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
