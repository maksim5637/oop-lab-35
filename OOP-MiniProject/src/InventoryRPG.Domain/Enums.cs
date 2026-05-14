namespace InventoryRPG.Domain;

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Resource
}

public enum EquipSlot
{
    Weapon,
    Head,
    Body,
    Legs,
    None
}

/// <summary>
/// Рідкісність предмета — впливає на бонуси через RarityBonusStrategy.
/// </summary>
public enum Rarity
{
    Common    = 0,
    Uncommon  = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}
