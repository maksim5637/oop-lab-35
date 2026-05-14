namespace InventoryRPG.Domain;

/// <summary>
/// Factory Pattern — створення предметів без прив'язки до конкретних класів.
/// Використовується при завантаженні з JSON та в тестах.
/// </summary>
public interface IItemFactory
{
    Weapon     CreateWeapon    (string name, float weight, int damage,
                                float range, Rarity rarity = Rarity.Common);
    Armor      CreateArmor     (string name, float weight, int defense,
                                EquipSlot slot, Rarity rarity = Rarity.Common);
    Consumable CreateConsumable(string name, float weight, int heal,
                                string effect, Rarity rarity = Rarity.Common);
    Resource   CreateResource  (string name, float weight, int qty,
                                Rarity rarity = Rarity.Common);
}

public sealed class DefaultItemFactory : IItemFactory
{
    public Weapon CreateWeapon(string name, float weight, int damage,
                               float range, Rarity rarity = Rarity.Common) =>
        new(name, weight, damage, range, rarity);

    public Armor CreateArmor(string name, float weight, int defense,
                             EquipSlot slot, Rarity rarity = Rarity.Common) =>
        new(name, weight, defense, slot, rarity);

    public Consumable CreateConsumable(string name, float weight, int heal,
                                       string effect, Rarity rarity = Rarity.Common) =>
        new(name, weight, heal, 0, effect, rarity);

    public Resource CreateResource(string name, float weight, int qty,
                                   Rarity rarity = Rarity.Common) =>
        new(name, weight, qty, rarity);
}
