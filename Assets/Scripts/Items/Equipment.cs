using System;
using System.Collections.Generic;
using UnityEngine;

// Equipped items, one per slot. UI binds to OnChanged. Equipping/unequipping will recompute stat
// modifiers onto the player's StatBlock once the affix/loot pass lands (RecomputeStats stub below).
public class Equipment : MonoBehaviour
{
    // Display order for the equipment "doll" in the character screen.
    public static readonly EquipSlot[] SlotOrder =
    {
        EquipSlot.Weapon, EquipSlot.Offhand, EquipSlot.Head, EquipSlot.Chest,
        EquipSlot.Hands, EquipSlot.Legs, EquipSlot.Feet, EquipSlot.Ring, EquipSlot.Amulet
    };

    readonly Dictionary<EquipSlot, ItemData> equipped = new Dictionary<EquipSlot, ItemData>();
    public event Action OnChanged;

    public ItemData Get(EquipSlot slot) => equipped.TryGetValue(slot, out var v) ? v : null;

    // Returns the item that was previously in the slot (so the caller can return it to the bag).
    public ItemData Equip(EquipSlot slot, ItemData item)
    {
        if (item != null && item.equipSlot != slot) return item;   // wrong slot — reject
        ItemData previous = Get(slot);
        if (item == null) equipped.Remove(slot);
        else equipped[slot] = item;
        RecomputeStats();
        OnChanged?.Invoke();
        return previous;
    }

    public ItemData Unequip(EquipSlot slot)
    {
        ItemData previous = Get(slot);
        equipped.Remove(slot);
        RecomputeStats();
        OnChanged?.Invoke();
        return previous;
    }

    // TODO (post-loot): sum equipped items' StatModifiers and apply to the HealthComponent's StatBlock.
    void RecomputeStats() { }
}
