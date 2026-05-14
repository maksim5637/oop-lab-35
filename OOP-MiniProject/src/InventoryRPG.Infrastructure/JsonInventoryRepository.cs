using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryRPG.Application;
using InventoryRPG.Domain;

namespace InventoryRPG.Infrastructure;

// ── Async persistence контракт ────────────────────────────────
public interface IDataStore<T>
{
    Task<IReadOnlyCollection<T>> LoadAsync(
        CancellationToken cancellationToken = default);
    Task SaveAsync(
        IReadOnlyCollection<T> items,
        CancellationToken cancellationToken = default);
}

// ── DTO для серіалізації (уникаємо проблем з поліморфізмом) ──
public sealed record ItemDto(
    string   TypeName,
    string   Name,
    float    Weight,
    int      Stat,
    float    Range,
    string   Effect,
    int      Duration,
    int      Quantity,
    string   Slot,
    string   Rarity
);

public sealed record InventoryDto(
    string CharacterId,
    List<ItemDto> Items,
    string[] QuickSlotNames
);

// ── Конвертер DTO ↔ Domain ────────────────────────────────────
public static class ItemDtoConverter
{
    public static ItemDto ToDto(Item item) => item switch
    {
        Weapon w    => new("Weapon",     w.Name, w.Weight, w.Damage,  w.Range,   "",        0,          0,          "Weapon", w.Rarity.ToString()),
        Armor a     => new("Armor",      a.Name, a.Weight, a.Defense, 0,         "",        0,          0,          a.Slot.ToString(), a.Rarity.ToString()),
        Consumable c=> new("Consumable", c.Name, c.Weight, c.HealAmount, 0,      c.Effect,  c.Duration, 0,          "None",   c.Rarity.ToString()),
        Resource r  => new("Resource",   r.Name, r.Weight, 0,         0,         "",        0,          r.Quantity, "None",   r.Rarity.ToString()),
        _           => throw new NotSupportedException($"Unknown item type: {item.GetType()}")
    };

    public static Item FromDto(ItemDto dto)
    {
        var rarity = Enum.Parse<Rarity>(dto.Rarity);
        return dto.TypeName switch
        {
            "Weapon"     => new Weapon(dto.Name, dto.Weight, dto.Stat,
                                       dto.Range, rarity),
            "Armor"      => new Armor(dto.Name, dto.Weight, dto.Stat,
                                      Enum.Parse<EquipSlot>(dto.Slot), rarity),
            "Consumable" => new Consumable(dto.Name, dto.Weight, dto.Stat,
                                           dto.Duration, dto.Effect, rarity),
            "Resource"   => new Resource(dto.Name, dto.Weight,
                                         Math.Max(1, dto.Quantity), rarity),
            _ => throw new NotSupportedException($"Unknown type: {dto.TypeName}")
        };
    }
}

// ── JSON файловий репозиторій ────────────────────────────────
/// <summary>
/// Реалізує IInventoryRepository + IDataStore.
/// Підміняє InMemoryInventoryRepository без змін у Application.
/// </summary>
public sealed class JsonInventoryRepository
    : IInventoryRepository, IDataStore<InventoryDto>
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonInventoryRepository(string filePath = "inventory_save.json")
    {
        _filePath = filePath;
    }

    // ── IInventoryRepository ─────────────────────────────────
    public void Save(string characterId, Inventory inventory)
        => SaveAsync(characterId, inventory).GetAwaiter().GetResult();

    public Inventory Load(string characterId)
        => LoadAsync(characterId).GetAwaiter().GetResult();

    public bool Exists(string characterId)
    {
        if (!File.Exists(_filePath)) return false;
        var all = LoadAllDtos().GetAwaiter().GetResult();
        return all.Any(d => d.CharacterId == characterId);
    }

    // ── Async ────────────────────────────────────────────────
    public async Task SaveAsync(string characterId, Inventory inventory,
        CancellationToken ct = default)
    {
        var all  = (await LoadAllDtos(ct)).ToList();
        var dto  = BuildDto(characterId, inventory);
        var idx  = all.FindIndex(d => d.CharacterId == characterId);
        if (idx >= 0) all[idx] = dto;
        else          all.Add(dto);

        var json = JsonSerializer.Serialize(all, _options);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    public async Task<Inventory> LoadAsync(string characterId,
        CancellationToken ct = default)
    {
        var all = await LoadAllDtos(ct);
        var dto = all.FirstOrDefault(d => d.CharacterId == characterId)
                  ?? throw new KeyNotFoundException(
                      $"Збереження для '{characterId}' не знайдено.");
        return RestoreInventory(dto);
    }

    // ── IDataStore<InventoryDto> ─────────────────────────────
    public async Task<IReadOnlyCollection<InventoryDto>> LoadAsync(
        CancellationToken ct = default)
        => await LoadAllDtos(ct);

    public async Task SaveAsync(IReadOnlyCollection<InventoryDto> items,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(items, _options);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    // ── Приватні методи ──────────────────────────────────────
    private async Task<List<InventoryDto>> LoadAllDtos(
        CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return new List<InventoryDto>();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize<List<InventoryDto>>(json, _options)
                   ?? new List<InventoryDto>();
        }
        catch (JsonException ex)
        {
            // Бізнес-правило: пошкоджений файл → повертаємо порожній стан
            Console.WriteLine($"⚠ Пошкоджений файл збереження: {ex.Message}");
            return new List<InventoryDto>();
        }
    }

    private static InventoryDto BuildDto(string characterId, Inventory inventory)
    {
        var itemDtos = inventory.Items.Select(ItemDtoConverter.ToDto).ToList();
        var quickNames = Enumerable.Range(0, 4)
            .Select(i => inventory.GetQuickSlot(i)?.Name ?? "")
            .ToArray();
        return new InventoryDto(characterId, itemDtos, quickNames);
    }

    private static Inventory RestoreInventory(InventoryDto dto)
    {
        var inventory = new Inventory();
        // HashSet для швидкого пошуку quick-slot назв
        var quickNames = new HashSet<string>(dto.QuickSlotNames.Where(n => n != ""));

        foreach (var itemDto in dto.Items)
        {
            try
            {
                var item = ItemDtoConverter.FromDto(itemDto);
                inventory.AddItem(item);
                // Відновлюємо швидкі слоти
                for (int i = 0; i < dto.QuickSlotNames.Length; i++)
                    if (dto.QuickSlotNames[i] == item.Name)
                        inventory.AssignQuickSlot(item, i);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Пропускаємо пошкоджений предмет: {ex.Message}");
            }
        }
        return inventory;
    }
}
