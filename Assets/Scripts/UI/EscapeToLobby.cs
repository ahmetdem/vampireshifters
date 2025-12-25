using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple escape handler - pressing ESC returns to lobby selection.
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

    public void ReturnToLobby()
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;

        Debug.Log("[EscapeToLobby] ESC pressed - returning to lobby...");

        // Shutdown network manager before loading menu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
