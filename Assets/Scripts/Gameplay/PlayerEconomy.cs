using Unity.Netcode;
using UnityEngine;

public class PlayerEconomy : NetworkBehaviour
{
    public NetworkVariable<int> totalCoins = new NetworkVariable<int>(0);

    // XP System
    public NetworkVariable<int> currentXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1);

    // Formula: XP needed for next level
    // Example: Level 1 needs 100, Level 2 needs 120...
    public int XpToNextLevel => 100 + ((currentLevel.Value - 1) * 50);

    public void CollectCoin(int amount)
    {
        if (!IsServer) return;

        // 1. Add Gold
        totalCoins.Value += amount;

        // 2. Add XP
        AddExperience(amount); // 1 Coin = 1 XP (or multiply it if you want)
    }

    private void AddExperience(int amount)
    {
        currentXP.Value += amount;

        // Check for Level Up
        if (currentXP.Value >= XpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        // 1. Subtract cost (or keep accumulating if you prefer total XP style)
        currentXP.Value -= XpToNextLevel;

        // 2. Increase Level
        currentLevel.Value++;

        Debug.Log($"[Economy] Player {OwnerClientId} Leveled Up! New Level: {currentLevel.Value}");

        // 3. Check Win Condition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckWinCondition(OwnerClientId, currentLevel.Value);
        }

        // 4. Show upgrade UI to the player (instead of random upgrade)
        ShowUpgradeUIClientRpc();
    }

    /// <summary>
    /// ClientRpc to show the upgrade selection UI on the owning client.
    /// </summary>
    [ClientRpc]
    private void ShowUpgradeUIClientRpc()
    {
        if (!IsOwner) return; // Only show to this player
        
        if (TryGetComponent(out WeaponController controller))
        {
            if (LevelUpUI.Instance != null)
            {
                LevelUpUI.Instance.ShowOptions(controller);
            }
            else
            {
                Debug.LogWarning("[Economy] LevelUpUI.Instance is null! Make sure LevelUpUI is in the scene.");
            }
        }
    }

    /// <summary>
    /// Add multiple levels at once (e.g., PvP winner bonus).
    /// Uses random upgrades to avoid UI spam.
    /// </summary>
    public void AddLevels(int count)
    {
        if (!IsServer) return;

        for (int i = 0; i < count; i++)
        {
            currentLevel.Value++;

            Debug.Log($"[Economy] Player {OwnerClientId} gained bonus level! New Level: {currentLevel.Value}");

            // Check Win Condition after each level
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinCondition(OwnerClientId, currentLevel.Value);
            }

            // Apply a random upgrade for each level gained (no UI for bonus levels)
            ApplyRandomUpgrade();
        }
    }

    /// <summary>
    /// Automatically applies a random upgrade from the player's upgrade pool.
    /// Used for PvP bonus levels only.
    /// </summary>
    private void ApplyRandomUpgrade()
    {
        if (TryGetComponent(out WeaponController controller))
        {
            if (controller.allUpgradesPool != null && controller.allUpgradesPool.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, controller.allUpgradesPool.Count);
                controller.ApplyUpgradeAtIndex(randomIndex);
            }
        }
    }
}

