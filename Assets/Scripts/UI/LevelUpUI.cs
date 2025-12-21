using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance; // Singleton so Player can find it easily

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    // Optional: Add Image[] optionIcons if you want to show sprites

    private WeaponController localPlayerController;
    private List<int> currentOptions = new List<int>();

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false); // Hide on start
    }

    public void ShowOptions(WeaponController controller)
    {
        localPlayerController = controller;
        currentOptions.Clear();

        // 1. Pick 3 Random Weapons from the Player's Pool
        // (Logic simplified: just picking random indices for now)
        int poolSize = controller.allWeaponsPool.Count;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (poolSize == 0) break;

            int randomIndex = Random.Range(0, poolSize);
            // TODO: In the future, add logic here to ensure we don't pick the same one twice

            currentOptions.Add(randomIndex);

            // 2. Update the UI Text
            WeaponData data = controller.allWeaponsPool[randomIndex];
            optionTexts[i].text = data.weaponName;

            // Setup the Button Listener
            int indexToSend = randomIndex; // Capture for lambda
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectUpgrade(indexToSend));
        }

        // 3. Show Panel
        panel.SetActive(true);
    }

    private void SelectUpgrade(int weaponIndex)
    {
        // 4. Send request to Server
        if (localPlayerController != null)
        {
            localPlayerController.RequestUnlockWeaponServerRpc(weaponIndex);
        }

        // 5. Hide Panel
        panel.SetActive(false);
    }
}
