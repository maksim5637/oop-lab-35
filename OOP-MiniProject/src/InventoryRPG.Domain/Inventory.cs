namespace InventoryRPG.Domain;

/// <summary>
/// Розширено для Lab 35:
/// BR-01: ліміт ваги 25 кг
/// BR-02: ліміт кількості слотів у сітці
/// BR-03: унікальність предметів (HashSet для O(1) Contains)
/// GetByType, GetByMaxWeight, Statistics — LINQ
/// </summary>
public sealed class Inventory
{
    public const float MaxWeight = 25.0f;
    private const int QuickSlotsCount = 4;

    private readonly List<Item>               _items      = new();
    private readonly HashSet<Item>            _itemSet    = new(); // O(1) Contains
    private readonly InventoryGrid            _grid;
    private readonly List<IInventoryObserver> _observers  = new();
    private readonly Item?[]                  _quickSlots = new Item?[QuickSlotsCount];

    public float CurrentWeight          => _items.Sum(i => i.Weight);
    public IReadOnlyList<Item> Items    => _items.AsReadOnly();
    public bool IsOverWeight            => CurrentWeight > MaxWeight;
    public int  ItemCount               => _items.Count;

    public Inventory(int gridRows = 8, int gridCols = 6)
        => _grid = new InventoryGrid(gridRows, gridCols);

    // ── Observer ─────────────────────────────────────────────
    public void Subscribe(IInventoryObserver o)   => _observers.Add(o);
    public void Unsubscribe(IInventoryObserver o) => _observers.Remove(o);
    private void Notify(string msg)
    {
        foreach (var o in _observers.ToList())
            o.OnInventoryChanged(msg);
    }

    // ── BR-01, BR-02: Додавання з перевірками ────────────────
    public Result<bool> AddItem(Item item)
    {
        // BR-03: предмет вже є в інвентарі
        if (_itemSet.Contains(item))
            return Result<bool>.Fail($"«{item.Name}» вже є в інвентарі.");

        float newWeight = CurrentWeight + item.Weight;
        if (newWeight > MaxWeight)
        {
            var msg = $"❌ Перевищення ваги: {newWeight:F1}/{MaxWeight} кг";
            Notify(msg);
            return Result<bool>.Fail(msg);
        }

        if (!_grid.TryPlace(item))
        {
            var msg = $"❌ Немає місця в сітці для «{item.Name}» ({item.GridWidth}×{item.GridHeight})";
            Notify(msg);
            return Result<bool>.Fail(msg);
        }

        _items.Add(item);
        _itemSet.Add(item);
        Notify($"✅ Додано: {item.Name} ({item.Rarity.DisplayName()})");
        return Result<bool>.Ok(true);
    }

    public Result<bool> RemoveItem(Item item)
    {
        if (!_itemSet.Contains(item))
            return Result<bool>.Fail($"Предмет «{item.Name}» не знайдено.");

        _grid.Remove(item);
        _items.Remove(item);
        _itemSet.Remove(item);
        for (int i = 0; i < QuickSlotsCount; i++)
            if (_quickSlots[i] == item) _quickSlots[i] = null;

        Notify($"🗑 Видалено: {item.Name}");
        return Result<bool>.Ok(true);
    }

    // ── Швидкі слоти ─────────────────────────────────────────
    public bool  AssignQuickSlot(Item item, int slot) =>
        slot >= 0 && slot < QuickSlotsCount && _itemSet.Contains(item)
            ? (_quickSlots[slot] = item) is not null
            : false;

    public Item? GetQuickSlot(int slot) =>
        slot >= 0 && slot < QuickSlotsCount ? _quickSlots[slot] : null;

    public void  ClearQuickSlot(int slot)
    {
        if (slot >= 0 && slot < QuickSlotsCount) _quickSlots[slot] = null;
    }

    public Item? GetItemAtGrid(int row, int col) => _grid.GetItemAt(row, col);

    // ── LINQ-запити ───────────────────────────────────────────
    public IEnumerable<Item> GetByType(ItemType type) =>
        _items.Where(i => i.Type == type);

    public IEnumerable<Item> GetByMaxWeight(float maxW) =>
        _items.Where(i => i.Weight <= maxW).OrderBy(i => i.Weight);

    public IEnumerable<Item> GetByRarity(Rarity rarity) =>
        _items.Where(i => i.Rarity == rarity);

    public IEnumerable<Item> Search(string query) =>
        _items.Where(i => i.Name.Contains(query,
                          StringComparison.OrdinalIgnoreCase));

    // BR-05: аналітика інвентарю
    public InventoryStats GetStats() => new(
        TotalItems     : _items.Count,
        TotalWeight    : CurrentWeight,
        MaxWeight      : MaxWeight,
        ByType         : _items.GroupBy(i => i.Type)
                               .ToDictionary(g => g.Key, g => g.Count()),
        ByRarity       : _items.GroupBy(i => i.Rarity)
                               .ToDictionary(g => g.Key, g => g.Count()),
        HeaviestItem   : _items.MaxBy(i => i.Weight),
        RarestItem     : _items.MaxBy(i => (int)i.Rarity)
    );
}

/// <summary>Value object — агрегована статистика інвентарю.</summary>
public record InventoryStats(
    int  TotalItems,
    float TotalWeight,
    float MaxWeight,
    Dictionary<ItemType, int> ByType,
    Dictionary<Rarity, int>   ByRarity,
    Item? HeaviestItem,
    Item? RarestItem
);
