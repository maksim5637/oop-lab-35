namespace InventoryRPG.Domain;

/// <summary>
/// Агрегована статистика інвентарю — обчислюється через LINQ.
/// Використовується для аналітичних запитів у консолі та тестах.
/// </summary>
public sealed record InventoryStatistics(
    int   TotalItems,
    float TotalWeight,
    float MaxWeight,
    int   WeaponCount,
    int   ArmorCount,
    int   ConsumableCount,
    int   ResourceCount,
    int   TotalAttackBonus,
    int   TotalDefenseBonus,
    float WeightUsagePercent,
    IReadOnlyDictionary<Rarity, int> ItemsByRarity,
    Item? HeaviestItem,
    Item? MostPowerfulItem
);

/// <summary>
/// LINQ-запити для аналітики інвентарю.
/// Extension methods — не змінюють Inventory, розширюють його.
/// </summary>
public static class InventoryAnalytics
{
    /// <summary>Повна статистика інвентарю.</summary>
    public static InventoryStatistics GetStatistics(
        this Inventory inventory, Equipment equipment)
    {
        var items = inventory.Items;

        // Dictionary — групування за рідкісністю
        var byRarity = items
            .GroupBy(i => i.Rarity)
            .ToDictionary(g => g.Key, g => g.Count());

        // Найважчий предмет
        var heaviest = items.MaxBy(i => i.Weight);

        // Найпотужніша зброя
        var strongest = items
            .OfType<Weapon>()
            .MaxBy(w => w.EffectiveDamage);

        return new InventoryStatistics(
            TotalItems        : items.Count,
            TotalWeight       : inventory.CurrentWeight,
            MaxWeight         : Inventory.MaxWeight,
            WeaponCount       : items.Count(i => i.Type == ItemType.Weapon),
            ArmorCount        : items.Count(i => i.Type == ItemType.Armor),
            ConsumableCount   : items.Count(i => i.Type == ItemType.Consumable),
            ResourceCount     : items.Count(i => i.Type == ItemType.Resource),
            TotalAttackBonus  : equipment.GetAttackBonus(),
            TotalDefenseBonus : equipment.GetDefenseBonus(),
            WeightUsagePercent: inventory.CurrentWeight / Inventory.MaxWeight * 100f,
            ItemsByRarity     : byRarity,
            HeaviestItem      : heaviest,
            MostPowerfulItem  : strongest
        );
    }

    /// <summary>Пошук за кількома критеріями через делегат-предикат.</summary>
    public static IReadOnlyList<Item> Search(
        this Inventory inventory,
        Func<Item, bool> predicate) =>
        inventory.Items.Where(predicate).ToList().AsReadOnly();

    /// <summary>Сортування предметів з пагінацією.</summary>
    public static IReadOnlyList<Item> GetPagedByWeight(
        this Inventory inventory, int page, int pageSize) =>
        inventory.Items
            .OrderByDescending(i => i.Weight)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

    /// <summary>Топ-N найпотужніших предметів.</summary>
    public static IReadOnlyList<Weapon> TopWeapons(
        this Inventory inventory, int count = 3) =>
        inventory.Items
            .OfType<Weapon>()
            .OrderByDescending(w => w.EffectiveDamage)
            .Take(count)
            .ToList()
            .AsReadOnly();

    /// <summary>Групування за типом — Dictionary.</summary>
    public static IReadOnlyDictionary<ItemType, IReadOnlyList<Item>> GroupByType(
        this Inventory inventory) =>
        inventory.Items
            .GroupBy(i => i.Type)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Item>)g.ToList().AsReadOnly()
            );

    /// <summary>
    /// Бізнес-правило: предмети що займають більше 10% вагового ліміту.
    /// </summary>
    public static IReadOnlyList<Item> GetHeavyItems(
        this Inventory inventory, float thresholdPercent = 0.1f) =>
        inventory.Items
            .Where(i => i.Weight >= Inventory.MaxWeight * thresholdPercent)
            .OrderByDescending(i => i.Weight)
            .ToList()
            .AsReadOnly();
}
