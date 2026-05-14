namespace InventoryRPG.Domain;

/// <summary>
/// Розширено: підтримка IRarityBonusStrategy — Strategy Pattern.
/// GetAttackBonus/GetDefenseBonus тепер враховують рідкісність.
/// </summary>
public sealed class Equipment
{
    private readonly Dictionary<EquipSlot, Item?> _slots = new()
    {
        [EquipSlot.Weapon] = null,
        [EquipSlot.Head]   = null,
        [EquipSlot.Body]   = null,
        [EquipSlot.Legs]   = null,
    };

    // Strategy — можна підмінити без зміни Equipment
    public IRarityBonusStrategy BonusStrategy { get; set; }
        = new MultiplicativeRarityBonus();

    private static bool IsCompatible(Item item, EquipSlot slot) => item switch
    {
        Weapon                => slot == EquipSlot.Weapon,
        Armor armor           => armor.Slot == slot,
        _                     => false
    };

    public Result<Item?> Equip(Item item, EquipSlot slot)
    {
        if (!IsCompatible(item, slot))
            return Result<Item?>.Fail($"«{item.Name}» несумісне зі слотом {slot}.");

        var old = _slots[slot];
        _slots[slot] = item;
        return Result<Item?>.Ok(old);
    }

    public Item? Unequip(EquipSlot slot)
    {
        var item = _slots.GetValueOrDefault(slot);
        if (item != null) _slots[slot] = null;
        return item;
    }

    public Item? GetItem(EquipSlot slot) => _slots.GetValueOrDefault(slot);

    // Бізнес-правило BR-04: бонус враховує рідкісність через Strategy
    public int GetAttackBonus() =>
        _slots.Values.OfType<Weapon>()
              .Sum(w => w.EffectiveDamage(BonusStrategy));

    public int GetDefenseBonus() =>
        _slots.Values.OfType<Armor>()
              .Sum(a => a.EffectiveDefense(BonusStrategy));

    public IEnumerable<(EquipSlot Slot, Item Item)> EquippedItems() =>
        _slots.Where(kv => kv.Value is not null)
              .Select(kv => (kv.Key, kv.Value!));
}
