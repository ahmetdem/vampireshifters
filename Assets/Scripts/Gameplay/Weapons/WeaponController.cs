using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Progression")]
    public List<WeaponData> allWeaponsPool;

    // Helper to know what we already have (to avoid duplicates later)
    private List<int> unlockedWeaponIndices = new List<int>();

    // List of equipped weapon behaviors
    private List<BaseWeapon> activeWeapons = new List<BaseWeapon>();

    // For testing: Drag a WeaponData here to start with it
    [SerializeField] private WeaponData startingWeapon;

    public override void OnNetworkSpawn()
    {
        // Only Server runs combat logic (authoritative)
        if (IsServer && startingWeapon != null)
        {
            AddWeapon(startingWeapon);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Run the cooldown loop for all weapons
        foreach (var weapon in activeWeapons)
        {
            weapon.WeaponUpdate();
        }
    }

    [ServerRpc]
    public void RequestUnlockWeaponServerRpc(int index)
    {
        // Validation: Ensure index is valid
        if (index < 0 || index >= allWeaponsPool.Count) return;

        // If we want to prevent duplicates, check here:
        if (unlockedWeaponIndices.Contains(index)) return;

        // Apply the upgrade
        WeaponData selectedWeapon = allWeaponsPool[index];
        AddWeapon(selectedWeapon);

        // Track it
        unlockedWeaponIndices.Add(index);
    }

    // Update your existing AddWeapon to be cleaner if needed
    public void AddWeapon(WeaponData newData)
    {
        if (newData == null) return;

        GameObject weaponObj = new GameObject($"Weapon_{newData.weaponName}");
        weaponObj.transform.SetParent(transform);
        weaponObj.transform.localPosition = Vector3.zero;

        System.Type type = System.Type.GetType(newData.behaviorScript);

        if (type != null)
        {
            BaseWeapon newWeapon = (BaseWeapon)weaponObj.AddComponent(type);

            // Ensure you are passing BOTH the data and the NetworkObjectId
            // This is where it likely failed before
            newWeapon.Initialize(newData, NetworkObjectId);

            activeWeapons.Add(newWeapon);
            Debug.Log($"[WeaponController] Initialized {newData.weaponName}");
        }
    }
}
