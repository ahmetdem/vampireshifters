using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Slots")]
    [SerializeField] private Image[] slotImages;
    [SerializeField] private Sprite emptySlotSprite;

    private PlayerInventory localInventory;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        if (NetworkManager.Singleton.IsClient) FindLocalInventory();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) FindLocalInventory();
    }

    private void FindLocalInventory()
    {
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            localInventory = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInventory>();

            if (localInventory != null)
            {
                // Subscribe to changes so UI updates automatically
                localInventory.inventorySlots.OnListChanged += OnInventoryChanged;
                UpdateIcons(); // Initial Draw
            }
        }
    }

    private void OnInventoryChanged(NetworkListEvent<Unity.Collections.FixedString32Bytes> changeEvent)
    {
        UpdateIcons();
    }

    private void UpdateIcons()
    {
        if (localInventory == null) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i < localInventory.inventorySlots.Count)
            {
                // 1. Get ID
                string itemId = localInventory.inventorySlots[i].ToString();

                // 2. Get Data
                InventoryItemData data = localInventory.GetItemData(itemId);

                // 3. Show Icon
                if (data != null)
                {
                    slotImages[i].sprite = data.icon;
                    slotImages[i].enabled = true;
                    slotImages[i].color = Color.white; // Always white (no greying out)
                }
            }
            else
            {
                // Empty Slot
                slotImages[i].sprite = emptySlotSprite;

                // If you use a semi-transparent background sprite, keep it enabled.
                // If you have NO background sprite, disable the image.
                if (emptySlotSprite == null)
                {
                    slotImages[i].enabled = false;
                }
                else
                {
                    slotImages[i].enabled = true;
                    slotImages[i].color = new Color(1, 1, 1, 0.5f);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (localInventory != null) localInventory.inventorySlots.OnListChanged -= OnInventoryChanged;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}
