namespace InventoryRPG.Domain.Filters;

// ── Strategy: інтерфейс фільтра предметів ────────────────────
/// <summary>
/// Strategy Pattern — кожен фільтр реалізує свою логіку.
/// Нові фільтри додаються без зміни Inventory або InventoryService.
/// </summary>
public interface IItemFilter
{
    bool Matches(Item item);
    string Description { get; }
}

// ── Конкретні стратегії фільтрації ───────────────────────────

/// <summary>Фільтр за типом предмета.</summary>
public sealed class TypeFilter : IItemFilter
{
    private readonly ItemType _type;
    public TypeFilter(ItemType type) => _type = type;
    public bool   Matches(Item item)  => item.Type == _type;
    public string Description         => $"Тип = {_type}";
}

/// <summary>Фільтр за максимальною вагою.</summary>
public sealed class MaxWeightFilter : IItemFilter
{
    private readonly float _maxWeight;
    public MaxWeightFilter(float maxWeight) => _maxWeight = maxWeight;
    public bool   Matches(Item item)  => item.Weight <= _maxWeight;
    public string Description         => $"Вага ≤ {_maxWeight:F1} кг";
}

/// <summary>Фільтр за рідкісністю.</summary>
public sealed class RarityFilter : IItemFilter
{
    private readonly Rarity _minRarity;
    public RarityFilter(Rarity minRarity) => _minRarity = minRarity;
    public bool   Matches(Item item)  => item.Rarity >= _minRarity;
    public string Description         => $"Рідкісність ≥ {_minRarity}";
}

/// <summary>Фільтр за назвою (підрядок).</summary>
public sealed class NameFilter : IItemFilter
{
    private readonly string _query;
    public NameFilter(string query) => _query = query;
    public bool   Matches(Item item)  =>
        item.Name.Contains(_query, StringComparison.OrdinalIgnoreCase);
    public string Description         => $"Назва містить '{_query}'";
}

/// <summary>
/// Composite-фільтр: об'єднує кілька фільтрів через AND.
/// Делегати / Func дозволяють будувати довільні пайплайни.
/// </summary>
public sealed class CompositeFilter : IItemFilter
{
    private readonly IReadOnlyList<IItemFilter> _filters;

    public CompositeFilter(params IItemFilter[] filters)
    {
        _filters = filters;
    }

    public bool Matches(Item item) => _filters.All(f => f.Matches(item));

    public string Description =>
        string.Join(" AND ", _filters.Select(f => f.Description));
}

/// <summary>
/// Extension methods для зручного побудови фільтрів.
/// Використовує делегати/Func для конвеєра.
/// </summary>
public static class ItemFilterExtensions
{
    public static IEnumerable<Item> ApplyFilter(
        this IEnumerable<Item> items, IItemFilter filter) =>
        items.Where(filter.Matches);

    public static IItemFilter And(this IItemFilter a, IItemFilter b) =>
        new CompositeFilter(a, b);
}
