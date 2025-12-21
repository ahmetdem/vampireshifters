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

        // 3. Trigger Client Event (Show UI)
        // We use a ClientRpc to tell the specific client "Show your menu!"
        ShowLevelUpUIClientRpc();
    }

    [ClientRpc]
    private void ShowLevelUpUIClientRpc()
    {
        if (!IsOwner) return;

        // Find our local weapon controller
        if (TryGetComponent(out WeaponController controller))
        {
            if (LevelUpUI.Instance != null)
            {
                LevelUpUI.Instance.ShowOptions(controller);
            }
        }
    }
}
