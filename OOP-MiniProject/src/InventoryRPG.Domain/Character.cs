namespace InventoryRPG.Domain;

/// <summary>
/// Центральний координатор. Lab 35: Result<T> скрізь, бізнес-правила BR-01..BR-05.
/// </summary>
public sealed class Character
{
    public string    Name      { get; }
    public int       MaxHp     { get; }
    public Inventory Inventory { get; }
    public Equipment Equipment { get; }

    private int _hp;
    public int Hp
    {
        get => _hp;
        private set => _hp = Math.Clamp(value, 0, MaxHp);
    }

    public bool  IsAlive    => Hp > 0;
    public float HpPercent  => (float)Hp / MaxHp * 100f;

    public Character(string name, int maxHp = 120)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ім'я не може бути порожнім.", nameof(name));
        if (maxHp <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHp));

        Name      = name;
        MaxHp     = maxHp;
        _hp       = maxHp;
        Inventory = new Inventory();
        Equipment = new Equipment();
    }

    public void Heal(int amount)       => Hp += amount;
    public void TakeDamage(int amount) => Hp -= amount;

    public Result<bool>   PickUp(Item item)             => Inventory.AddItem(item);

    public Result<string> EquipItem(Item item, EquipSlot slot)
    {
        if (!Inventory.Items.Contains(item))
            return Result<string>.Fail($"«{item.Name}» не в інвентарі.");

        var res = Equipment.Equip(item, slot);
        if (!res.IsSuccess) return Result<string>.Fail(res.Error);

        if (res.Value is { } old) Inventory.AddItem(old);
        return Result<string>.Ok(
            $"Екіпіровано «{item.Name}» → {slot} " +
            $"(ATK:{Equipment.GetAttackBonus()}, DEF:{Equipment.GetDefenseBonus()})");
    }

    public Result<bool> UnequipItem(EquipSlot slot)
    {
        var item = Equipment.Unequip(slot);
        if (item is null) return Result<bool>.Fail("Слот порожній.");
        Inventory.AddItem(item);
        return Result<bool>.Ok(true);
    }

    public Result<string> UseItem(Item item)
    {
        if (!Inventory.Items.Contains(item))
            return Result<string>.Fail($"«{item.Name}» не знайдено.");

        var msg = item.Use(this);
        if (item is Consumable) Inventory.RemoveItem(item);
        return Result<string>.Ok(msg);
    }

    public override string ToString() =>
        $"Character({Name}, HP={Hp}/{MaxHp}, " +
        $"ATK={Equipment.GetAttackBonus()}, DEF={Equipment.GetDefenseBonus()})";
}
