using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Progression")]
    // FIX: Change 'WeaponData' to 'UpgradeData'
    public List<UpgradeData> allUpgradesPool;
    public float globalDamageMultiplier = 1.0f;

    private List<int> unlockedWeaponIndices = new List<int>();
    private List<BaseWeapon> activeWeapons = new List<BaseWeapon>();

    [SerializeField] private WeaponData startingWeapon;

    public override void OnNetworkSpawn()
    {
        if (IsServer && startingWeapon != null)
        {
            AddWeapon(startingWeapon);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        foreach (var weapon in activeWeapons)
        {
            weapon.WeaponUpdate();
        }
    }

    [ServerRpc]
    public void RequestUnlockWeaponServerRpc(int index)
    {
        // This now works because the list type matches the variable type
        if (index < 0 || index >= allUpgradesPool.Count) return;

        UpgradeData selectedUpgrade = allUpgradesPool[index];

        selectedUpgrade.Apply(gameObject);

        Debug.Log($"[Upgrade] Player {OwnerClientId} picked {selectedUpgrade.upgradeName}");

        // After upgrade is applied, tell the client it's safe to resume
        ResumeGameplayClientRpc();
    }

    /// <summary>
    /// Directly applies an upgrade at the given index (server-side, no UI).
    /// Used for random upgrade selection on level up.
    /// </summary>
    public void ApplyUpgradeAtIndex(int index)
    {
        if (!IsServer) return;
        if (index < 0 || index >= allUpgradesPool.Count) return;

        UpgradeData selectedUpgrade = allUpgradesPool[index];
        selectedUpgrade.Apply(gameObject);

        Debug.Log($"[Upgrade] Player {OwnerClientId} auto-selected: {selectedUpgrade.upgradeName}");
    }

    [ClientRpc]
    private void ResumeGameplayClientRpc()
    {
        if (!IsOwner) return;

        // Re-enable movement/actions here if you disabled them
        // PlayerMovement.Instance.SetEnabled(true);
    }

    public void IncreaseGlobalDamage(float amount)
    {
        globalDamageMultiplier += amount;
    }

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
            newWeapon.Initialize(newData, NetworkObjectId);
            activeWeapons.Add(newWeapon);
        }
    }
}
