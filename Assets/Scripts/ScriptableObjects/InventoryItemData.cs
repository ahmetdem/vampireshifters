using UnityEngine;

public enum ItemEffectType
{
    None,
    SummonBoss,
    TriggerPvP,
    HealSelf
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public ItemEffectType effectType;
    public int valueAmount; // e.g., Heal amount, or unused for triggers
}
