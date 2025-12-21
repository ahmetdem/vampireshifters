using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button connectButton;

    private async void Start()
    {
        connectButton.interactable = false;

        // 1. Setup Initialization Options for ParrelSync
        InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
        // If we are a ParrelSync clone, use a unique profile name
        if (ParrelSync.ClonesManager.IsClone())
        {
            string cloneName = ParrelSync.ClonesManager.GetArgument();
            options.SetProfile(cloneName);
        }
#endif

        // 2. Initialize with those options
        await UnityServices.InitializeAsync(options);

        // 3. Sign in (This will now result in a unique PlayerID for the clone)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Load saved name
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        nameInput.text = savedName;

        nameInput.onValueChanged.AddListener(ValidateName);
        connectButton.onClick.AddListener(EnterLobby);

        ValidateName(savedName);
    }
    private void ValidateName(string name)
    {
        // Simple validation: 2-12 characters
        bool isValid = name.Length >= 2 && name.Length <= 12;
        connectButton.interactable = isValid;
    }

    private void EnterLobby()
    {
        // Save name for the next scene
        PlayerPrefs.SetString("PlayerName", nameInput.text);

        // Load the Main Menu
        SceneManager.LoadScene("01_MainMenu");
    }
}
