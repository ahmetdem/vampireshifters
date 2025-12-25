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
    private NetworkObject trackedPlayerObject; // Track the player object to detect respawn

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        if (NetworkManager.Singleton.IsClient) FindLocalInventory();
    }

    private void Update()
    {
        // Check if our tracked player was destroyed (player died) and we need to find the new one
        if (localInventory == null || trackedPlayerObject == null || !trackedPlayerObject.IsSpawned)
        {
            // Player object was destroyed or changed, try to find the new one
            FindLocalInventory();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) FindLocalInventory();
    }

    private void FindLocalInventory()
    {
        // Unsubscribe from old inventory if it still exists
        if (localInventory != null)
        {
            try
            {
                localInventory.inventorySlots.OnListChanged -= OnInventoryChanged;
            }
            catch { } // Ignore errors if object was destroyed
        }
        
        localInventory = null;
        trackedPlayerObject = null;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;
        
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            trackedPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            localInventory = trackedPlayerObject.GetComponent<PlayerInventory>();

            if (localInventory != null)
            {
                // Subscribe to changes so UI updates automatically
                localInventory.inventorySlots.OnListChanged += OnInventoryChanged;
                UpdateIcons(); // Initial Draw
                Debug.Log("[InventoryUI] Found and subscribed to new player inventory");
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
        if (localInventory != null)
        {
            try
            {
                localInventory.inventorySlots.OnListChanged -= OnInventoryChanged;
            }
            catch { } // Ignore errors if object was destroyed
        }
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}
