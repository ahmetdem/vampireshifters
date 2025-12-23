using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSlots = 3;

    public NetworkList<Unity.Collections.FixedString32Bytes> inventorySlots;
    [SerializeField] private List<InventoryItemData> allPossibleItems;

    private void Awake()
    {
        inventorySlots = new NetworkList<Unity.Collections.FixedString32Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) inventorySlots.OnListChanged += HandleInventoryChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) inventorySlots.OnListChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged(NetworkListEvent<Unity.Collections.FixedString32Bytes> changeEvent)
    {
        // UI updates automatically via InventoryUI.cs
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) RequestUseItemServerRpc(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) RequestUseItemServerRpc(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) RequestUseItemServerRpc(2);
    }

    // --- FIX 1: Prevent Duplicate Pickups ---
    public bool AddItem(string itemId)
    {
        if (!IsServer) return false;

        // 1. Check for Duplicates
        if (inventorySlots.Contains(itemId))
        {
            Debug.Log($"[Inventory] Player already has item: {itemId}");
            return false; // Reject the pickup (Item won't despawn)
        }

        // 2. Check Capacity
        if (inventorySlots.Count < maxSlots)
        {
            inventorySlots.Add(itemId);
            return true; // Accept pickup (Item will despawn)
        }
        return false;
    }

    [ServerRpc]
    private void RequestUseItemServerRpc(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;

        string itemId = inventorySlots[index].ToString();
        InventoryItemData data = GetItemData(itemId);

        if (data != null)
        {
            // --- FIX 2: Only remove item if usage was successful ---
            bool success = TryApplyEffect(data.effectType);

            if (success)
            {
                inventorySlots.RemoveAt(index);
                Debug.Log($"[Inventory] Used item: {itemId}");
            }
            else
            {
                Debug.Log("[Inventory] Cannot use item right now!");
                // Optional: Send ClientRpc here to play a "Error Sound" or show text "Cannot use in Arena!"
            }
        }
    }

    private bool TryApplyEffect(ItemEffectType effect)
    {
        switch (effect)
        {
            case ItemEffectType.SummonBoss:
                // Rule: Cannot use Boss Scroll if Boss Event is already active
                if (BossEventDirector.Instance != null)
                {
                    if (BossEventDirector.Instance.IsEventActive) return false; // Fail

                    BossEventDirector.Instance.ForceStartEvent();
                    return true; // Success
                }
                return false;

            case ItemEffectType.TriggerPvP:
                // Rule: Cannot use PvP Token if PvP is already active
                if (PvPDirector.Instance != null)
                {
                    if (PvPDirector.Instance.IsPvPActive.Value) return false; // Fail

                    PvPDirector.Instance.StartPvPEvent();
                    return true; // Success
                }
                return false;

            case ItemEffectType.HealSelf:
                if (TryGetComponent(out Health hp))
                {
                    hp.Heal(50);
                    return true;
                }
                return false;
        }
        return false;
    }

    public InventoryItemData GetItemData(string id)
    {
        return allPossibleItems.Find(x => x.itemId == id);
    }
}
