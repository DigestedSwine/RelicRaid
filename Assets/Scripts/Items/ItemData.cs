using UnityEngine;

// Where an equippable item goes. None = not equippable (consumable/material).
public enum EquipSlot { None, Weapon, Offhand, Head, Chest, Hands, Legs, Feet, Ring, Amulet }

public enum ItemType { Equipment, Consumable, Material, Quest }

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

// Which StatBlock field a modifier touches (used by the affix/loot pass — wired later).
public enum StatType { MaxHP, MaxMP, Atk, MAtk, Def, MDef, Spd, Crit, Dodge }

[System.Serializable]
public struct StatModifier { public StatType stat; public float value; }

// Data-driven item definition (one ScriptableObject per item, GDD SO-discipline). The random-affix
// loot pass (post-DB) fills `modifiers` and rolls rarity; for now items are hand-authored placeholders.
[CreateAssetMenu(menuName = "RelicRaid/Item", fileName = "Item")]
public class ItemData : ScriptableObject
{
    public string displayName = "New Item";
    [TextArea] public string description;
    public Sprite icon;                              // null → UI shows a colored placeholder tile
    public ItemType type = ItemType.Equipment;
    public EquipSlot equipSlot = EquipSlot.None;     // for Equipment type
    public ItemRarity rarity = ItemRarity.Common;
    public int maxStack = 1;

    [Tooltip("Flat stat bonuses — reserved for the affix/loot pass; empty for now.")]
    public StatModifier[] modifiers;

    // Rarity tint used across all item UI (slots, tooltips, borders).
    public static readonly Color[] RarityColors = {
        new Color(0.78f, 0.80f, 0.84f),   // Common  (grey)
        new Color(0.35f, 0.85f, 0.40f),   // Uncommon(green)
        new Color(0.35f, 0.60f, 1.00f),   // Rare    (blue)
        new Color(0.70f, 0.40f, 1.00f),   // Epic    (purple)
        new Color(1.00f, 0.62f, 0.15f),   // Legendary(orange)
    };
    public Color RarityColor => RarityColors[Mathf.Clamp((int)rarity, 0, RarityColors.Length - 1)];
}
