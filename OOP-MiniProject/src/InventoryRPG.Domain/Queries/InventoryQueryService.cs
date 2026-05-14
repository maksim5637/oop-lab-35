namespace InventoryRPG.Domain;

// ── Strategy: фільтри предметів ──────────────────────────────
/// <summary>
/// Strategy Pattern — кожен фільтр реалізує свою логіку.
/// Нові фільтри додаються без змін у коді запитів.
/// </summary>
public interface IItemFilter
{
    bool Matches(Item item);
    string Description { get; }
}

public sealed class ByTypeFilter : IItemFilter
{
    private readonly ItemType _type;
    public ByTypeFilter(ItemType type) => _type = type;
    public bool   Matches(Item item)  => item.Type == _type;
    public string Description         => $"тип = {_type}";
}

public sealed class ByRarityFilter : IItemFilter
{
    private readonly Rarity _rarity;
    public ByRarityFilter(Rarity rarity) => _rarity = rarity;
    public bool   Matches(Item item)     => item.Rarity == _rarity;
    public string Description            => $"рідкісність = {_rarity}";
}

public sealed class ByMaxWeightFilter : IItemFilter
{
    private readonly float _max;
    public ByMaxWeightFilter(float max) => _max = max;
    public bool   Matches(Item item)    => item.Weight <= _max;
    public string Description           => $"вага ≤ {_max}кг";
}

public sealed class ByNameFilter : IItemFilter
{
    private readonly string _term;
    public ByNameFilter(string term) => _term = term;
    public bool   Matches(Item item) =>
        item.Name.Contains(_term, StringComparison.OrdinalIgnoreCase);
    public string Description        => $"назва містить '{_term}'";
}

// Composite фільтр — AND логіка
public sealed class CompositeFilter : IItemFilter
{
    private readonly IReadOnlyList<IItemFilter> _filters;
    public CompositeFilter(params IItemFilter[] filters) => _filters = filters;
    public bool   Matches(Item item) => _filters.All(f => f.Matches(item));
    public string Description        =>
        string.Join(" AND ", _filters.Select(f => f.Description));
}

// ── LINQ-сервіс запитів ──────────────────────────────────────
/// <summary>
/// Сервіс аналітики та пошуку по інвентарю.
/// Всі методи повертають нові колекції, не змінюють стан.
/// </summary>
public static class InventoryQueryService
{
    // LINQ 1: Фільтрація з Strategy
    public static IEnumerable<Item> Filter(
        IEnumerable<Item> items, IItemFilter filter) =>
        items.Where(filter.Matches);

    // LINQ 2: Сортування за вагою (зростання/спадання)
    public static IEnumerable<Item> SortByWeight(
        IEnumerable<Item> items, bool descending = false) =>
        descending
            ? items.OrderByDescending(i => i.Weight)
            : items.OrderBy(i => i.Weight);

    // LINQ 3: Групування за типом
    public static IReadOnlyDictionary<ItemType, List<Item>> GroupByType(
        IEnumerable<Item> items) =>
        items
            .GroupBy(i => i.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

    // LINQ 4: Топ-N найважчих предметів
    public static IEnumerable<Item> TopHeaviest(
        IEnumerable<Item> items, int count = 5) =>
        items.OrderByDescending(i => i.Weight).Take(count);

    // LINQ 5: Агрегована статистика
    public static InventoryStats GetStats(IEnumerable<Item> items)
    {
        var list = items.ToList();
        if (list.Count == 0)
            return new InventoryStats(0, 0f, 0f, 0f, string.Empty,
                new Dictionary<ItemType, int>(),
                new Dictionary<Rarity, int>());

        return new InventoryStats(
            TotalCount    : list.Count,
            TotalWeight   : list.Sum(i => i.Weight),
            AverageWeight : list.Average(i => i.Weight),
            MaxWeight     : list.Max(i => i.Weight),
            HeaviestItem  : list.MaxBy(i => i.Weight)?.Name ?? string.Empty,
            ByType        : list.GroupBy(i => i.Type)
                                .ToDictionary(g => g.Key, g => g.Count()),
            ByRarity      : list.GroupBy(i => i.Rarity)
                                .ToDictionary(g => g.Key, g => g.Count())
        );
    }

    // LINQ 6: Предмети які персонаж може використати (рівень)
    public static IEnumerable<Item> AvailableForLevel(
        IEnumerable<Item> items, int level) =>
        items.Where(i => i.RequiredLevel <= level)
             .OrderBy(i => i.RequiredLevel);

    // LINQ 7: Пошук дублікатів за назвою
    public static IEnumerable<string> FindDuplicateNames(
        IEnumerable<Item> items) =>
        items
            .GroupBy(i => i.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
}

// ── DTO статистики (record — value object) ───────────────────
public sealed record InventoryStats(
    int                          TotalCount,
    float                        TotalWeight,
    float                        AverageWeight,
    float                        MaxWeight,
    string                       HeaviestItem,
    IReadOnlyDictionary<ItemType, int> ByType,
    IReadOnlyDictionary<Rarity,  int>  ByRarity
);
