namespace InventoryRPG.Domain;

// ── Зброя ────────────────────────────────────────────────────
public sealed class Weapon : Item
{
    public int       Damage          { get; }
    public float     Range           { get; }
    public EquipSlot Slot            => EquipSlot.Weapon;

    /// <summary>
    /// Бізнес-правило: ефективна шкода = Damage * множник рідкісності.
    /// </summary>
    public int EffectiveDamage =>
        (int)Math.Round(Damage * Rarity.BonusMultiplier());

    public Weapon(string name, float weight, int damage, float range,
                  Rarity rarity = Rarity.Common)
        : base(name, weight, 1, 4, ItemType.Weapon, rarity)
    {
        if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (range  <= 0) throw new ArgumentOutOfRangeException(nameof(range));
        Damage = damage;
        Range  = range;
    }

    public override string Use(Character character) =>
        $"{character.Name} атакує «{Name}»! " +
        $"Ефективна шкода: {EffectiveDamage} ({Rarity}), Дальність: {Range}м.";
}

// ── Броня ─────────────────────────────────────────────────────
public sealed class Armor : Item
{
    public int       Defense         { get; }
    public EquipSlot Slot            { get; }

    /// <summary>
    /// Бізнес-правило: ефективний захист = Defense * множник рідкісності.
    /// </summary>
    public int EffectiveDefense =>
        (int)Math.Round(Defense * Rarity.BonusMultiplier());

    public Armor(string name, float weight, int defense, EquipSlot slot,
                 Rarity rarity = Rarity.Common)
        : base(name, weight, 2, 3, ItemType.Armor, rarity)
    {
        if (defense < 0)
            throw new ArgumentOutOfRangeException(nameof(defense));
        if (slot == EquipSlot.None || slot == EquipSlot.Weapon)
            throw new ArgumentException("Броня не може займати слот зброї.", nameof(slot));
        Defense = defense;
        Slot    = slot;
    }

    public override string Use(Character character) =>
        $"Броня «{Name}» ({Rarity}) надягнута. Захист +{EffectiveDefense}.";
}

// ── Витратний предмет ──────────────────────────────────────────
public sealed class Consumable : Item
{
    public int    HealAmount { get; }
    public int    Duration   { get; }
    public string Effect     { get; }

    /// <summary>
    /// Бізнес-правило: ефективне зцілення = HealAmount * множник рідкісності.
    /// </summary>
    public int EffectiveHeal =>
        (int)Math.Round(HealAmount * Rarity.BonusMultiplier());

    public Consumable(string name, float weight,
                      int healAmount, int duration, string effect,
                      Rarity rarity = Rarity.Common)
        : base(name, weight, 1, 1, ItemType.Consumable, rarity)
    {
        if (healAmount < 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
        HealAmount = healAmount;
        Duration   = duration;
        Effect     = effect ?? string.Empty;
    }

    public override string Use(Character character)
    {
        character.Heal(EffectiveHeal);
        return $"«{Name}» ({Rarity}): +{EffectiveHeal} HP. Ефект: {Effect}";
    }
}

// ── Ресурс ─────────────────────────────────────────────────────
public sealed class Resource : Item
{
    public int Quantity { get; private set; }

    public Resource(string name, float weight, int quantity,
                    Rarity rarity = Rarity.Common)
        : base(name, weight, 1, 1, ItemType.Resource, rarity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }

    public void AddQuantity(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Quantity += amount;
    }

    public override string Use(Character character) =>
        $"Ресурс «{Name}» ({Rarity}, кількість: {Quantity}).";
}
