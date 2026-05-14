using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryRPG.Application;
using InventoryRPG.Domain;

namespace InventoryRPG.Infrastructure;

/// <summary>
/// JSON-реалізація IInventoryRepository — підмінює InMemory без змін у Application.
/// Реалізує async I/O через SaveAsync/LoadAsync.
/// Обробляє: відсутній файл, пошкоджений JSON, конфлікт даних.
/// </summary>
public sealed class JsonInventoryRepository : IInventoryRepository
{
    private readonly string _directory;
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented          = true,
        Converters             = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public JsonInventoryRepository(string directory = "saves")
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    // ── Sync wrapper (з Lab 34 контракту) ────────────────────
    public void Save(string characterId, Inventory inventory)
        => SaveAsync(characterId, inventory).GetAwaiter().GetResult();

    public Inventory Load(string characterId)
        => LoadAsync(characterId).GetAwaiter().GetResult();

    public bool Exists(string characterId)
        => File.Exists(GetPath(characterId));

    // ── Async I/O ─────────────────────────────────────────────
    public async Task SaveAsync(string characterId, Inventory inventory,
                                CancellationToken ct = default)
    {
        var dto  = MapToDto(characterId, inventory);
        var json = JsonSerializer.Serialize(dto, _options);
        await File.WriteAllTextAsync(GetPath(characterId), json, ct);
    }

    public async Task<Inventory> LoadAsync(string characterId,
                                           CancellationToken ct = default)
    {
        var path = GetPath(characterId);

        // Обробка: відсутній файл
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Збереження для «{characterId}» не знайдено.", path);

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path, ct);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Помилка читання файлу збереження: {ex.Message}", ex);
        }

        // Обробка: пошкоджений JSON
        CharacterSaveDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CharacterSaveDto>(json, _options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Пошкоджений файл збереження для «{characterId}»: {ex.Message}", ex);
        }

        if (dto is null)
            throw new InvalidOperationException("Порожній файл збереження.");

        return MapFromDto(dto);
    }

    // ── Mapping ───────────────────────────────────────────────
    private static CharacterSaveDto MapToDto(string id, Inventory inv)
    {
        var dto = new CharacterSaveDto
        {
            Name      = id,
            MaxHp     = 120,
            CurrentHp = 120,
            SavedAt   = DateTime.UtcNow
        };

        dto.Items.AddRange(inv.Items.Select(ItemToDto));
        return dto;
    }

    private static ItemDto ItemToDto(Item item) => item switch
    {
        Weapon w => new ItemDto
        {
            TypeDiscriminator = "Weapon",
            Name   = w.Name,  Weight = w.Weight,
            Rarity = w.Rarity.ToString(),
            GridX  = w.GridX, GridY  = w.GridY,
            Damage = w.BaseDamage, Range = w.Range
        },
        Armor a => new ItemDto
        {
            TypeDiscriminator = "Armor",
            Name   = a.Name,  Weight = a.Weight,
            Rarity = a.Rarity.ToString(),
            GridX  = a.GridX, GridY  = a.GridY,
            Defense = a.BaseDefense, ArmorSlot = a.Slot.ToString()
        },
        Consumable c => new ItemDto
        {
            TypeDiscriminator = "Consumable",
            Name   = c.Name,  Weight = c.Weight,
            Rarity = c.Rarity.ToString(),
            GridX  = c.GridX, GridY  = c.GridY,
            HealAmount = c.HealAmount, Effect = c.Effect
        },
        Resource r => new ItemDto
        {
            TypeDiscriminator = "Resource",
            Name   = r.Name,  Weight = r.Weight,
            Rarity = r.Rarity.ToString(),
            GridX  = r.GridX, GridY  = r.GridY,
            Quantity = r.Quantity
        },
        _ => throw new NotSupportedException($"Невідомий тип: {item.GetType().Name}")
    };

    private static Inventory MapFromDto(CharacterSaveDto dto)
    {
        var inv = new Inventory();
        var factory = new DefaultItemFactory();

        foreach (var d in dto.Items)
        {
            var rarity = Enum.TryParse<Rarity>(d.Rarity, out var r) ? r : Rarity.Common;
            Item item = d.TypeDiscriminator switch
            {
                "Weapon" => factory.CreateWeapon(
                    d.Name, d.Weight, d.Damage ?? 1, d.Range ?? 1f, rarity),
                "Armor" => factory.CreateArmor(
                    d.Name, d.Weight, d.Defense ?? 0,
                    Enum.Parse<EquipSlot>(d.ArmorSlot ?? "Body"), rarity),
                "Consumable" => factory.CreateConsumable(
                    d.Name, d.Weight, d.HealAmount ?? 0, d.Effect ?? "", rarity),
                "Resource" => factory.CreateResource(
                    d.Name, d.Weight, d.Quantity ?? 1, rarity),
                _ => throw new InvalidOperationException(
                    $"Невідомий тип предмета: {d.TypeDiscriminator}")
            };
            inv.AddItem(item);
        }

        return inv;
    }

    private string GetPath(string id) =>
        Path.Combine(_directory, $"{SanitizeId(id)}.json");

    private static string SanitizeId(string id) =>
        string.Concat(id.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
}
