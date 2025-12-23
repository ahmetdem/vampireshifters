# Changelog - Agent Session

## New Features
- **Win Condition**: Reaching Level 100 now triggers a Game Win.
- **PvP Rewards**: Winner gets +5 Levels (instant random upgrades).
- **Post-PvP Flow**: Players are teleported back to the forest arena, enemy spawning resumes, and camera resets properly.
- **Auto-Leveling**: Level up upgrades are now chosen randomly without UI interruption.
- **Item Despawn**: Coins (30s) and Items (60s) now auto-despawn to improve performance.

## New Files
- `Assets/Scripts/Gameplay/Managers/GameManager.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

## Modified Files
- `Assets/Scripts/Gameplay/PlayerEconomy.cs` (Win check, AddLevels, Auto-upgrade)
- `Assets/Scripts/Gameplay/Managers/PvPDirector.cs` (+5 Level Boost, Forest Return, Camera Fix)
- `Assets/Scripts/Network/ConnectionHandler.cs` (PvP Camera Reset on Death)
- `Assets/Scripts/Gameplay/Weapons/WeaponController.cs` (Server-side Upgrade Application)
- `Assets/Scripts/Gameplay/CoinPickup.cs` (Auto-despawn)
- `Assets/Scripts/Gameplay/Items/InventoryPickup.cs` (Auto-despawn)
