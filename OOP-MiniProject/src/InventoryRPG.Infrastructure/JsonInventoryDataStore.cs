using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryRPG.Application;
using InventoryRPG.Domain;

namespace InventoryRPG.Infrastructure;

// ── DTO для серіалізації ─────────────────────────────────────
// Використовуємо DTO щоб не серіалізувати складні об'єкти напряму
file sealed record ItemDto(
    string   ItemClass,   // "Weapon" | "Armor" | "Consumable" | "Resource"
    string   Name,
    float    Weight,
    string   Type,
    string   Rarity,
    int      RequiredLevel,
    // Weapon
    int?     Damage,
    float?   Range,
    // Armor
    int?     Defense,
    string?  Slot,
    // Consumable
    int?     HealAmount,
    int?     Duration,
    string?  Effect,
    // Resource
    int?     Quantity
);

file sealed record CharacterDto(
    string         Name,
    int            MaxHp,
    int            CurrentHp,
    int            Level,
    int            Experience,
    List<ItemDto>  Items,
    List<EquippedDto> Equipped,
    List<int?>     QuickSlots   // індекси у Items або null
);

file sealed record EquippedDto(string Slot, int ItemIndex);

// ── Асинхронне файлове сховище ───────────────────────────────
/// <summary>
/// IDataStore реалізація — JSON файл.
/// Реалізує контракт з Lab 34 (IInventoryRepository) +
/// новий асинхронний контракт IDataStore.
/// </summary>
public sealed class JsonInventoryDataStore : IInventoryRepository
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented    = true,
        Converters       = { new JsonStringEnumConverter() }
    };

    public JsonInventoryDataStore(string filePath = "inventory_save.json")
    {
        _filePath = filePath;
    }

    // ── Асинхронне збереження ─────────────────────────────────
    public async Task SaveAsync(Character character,
        CancellationToken ct = default)
    {
        var dto = ToDto(character);
        var json = JsonSerializer.Serialize(dto, _opts);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    // ── Асинхронне завантаження ───────────────────────────────
    public async Task<Result<Character>> LoadAsync(
        CancellationToken ct = default)
    {
        // Обробка: файл відсутній
        if (!File.Exists(_filePath))
            return Result<Character>.Fail($"Файл збереження не знайдено: {_filePath}");

        string json;
        try
        {
            json = await File.ReadAllTextAsync(_filePath, ct);
        }
        catch (IOException ex)
        {
            return Result<Character>.Fail($"Помилка читання файлу: {ex.Message}");
        }

        // Обробка: пошкоджений JSON
        CharacterDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CharacterDto>(json, _opts);
        }
        catch (JsonException ex)
        {
            return Result<Character>.Fail($"Пошкоджений JSON: {ex.Message}");
        }

        if (dto is null)
            return Result<Character>.Fail("Порожнє збереження.");

        // Відновлення об'єктів з DTO
        try
        {
            return Result<Character>.Ok(FromDto(dto));
        }
        catch (Exception ex)
        {
            return Result<Character>.Fail($"Помилка відновлення стану: {ex.Message}");
        }
    }

    // ── Синхронні методи (IInventoryRepository) ──────────────
    public void Save(string characterId, Inventory inventory)
    {
        // Спрощена версія для сумісності з Lab 34 контрактом
        // На Lab 35 рекомендується використовувати SaveAsync
    }

    public Inventory Load(string characterId) =>
        throw new NotSupportedException("Використайте LoadAsync.");

    public bool Exists(string characterId) =>
        File.Exists(_filePath);

    // ── DTO конвертери ────────────────────────────────────────
    private static CharacterDto ToDto(Character c)
    {
        var items = c.Inventory.Items.ToList();
        var itemDtos = items.Select(ItemToDto).ToList();

        var equipped = new List<EquippedDto>();
        foreach (EquipSlot slot in Enum.GetValues<EquipSlot>())
        {
            if (slot == EquipSlot.None) continue;
            var equippedItem = c.Equipment.GetItem(slot);
            if (equippedItem is null) continue;
            int idx = items.IndexOf(equippedItem);
            if (idx >= 0) equipped.Add(new EquippedDto(slot.ToString(), idx));
        }

        var quickSlots = Enumerable.Range(0, 4)
            .Select(i =>
            {
                var qs = c.Inventory.GetQuickSlot(i);
                return qs is null ? (int?)null : items.IndexOf(qs);
            })
            .ToList();

        return new CharacterDto(c.Name, c.MaxHp, c.Hp,
            c.Level, c.Experience, itemDtos, equipped, quickSlots);
    }

    private static Character FromDto(CharacterDto dto)
    {
        var character = new Character(dto.Name, dto.MaxHp, dto.Level);
        character.TakeDamage(dto.MaxHp - dto.CurrentHp);

        // Відновлення досвіду (через reflection-free спосіб)
        if (dto.Experience > 0)
            character.GainExperience(dto.Experience);

        var items = dto.Items.Select(DtoToItem).ToList();

        // Спочатку додаємо предмети
        foreach (var item in items)
            character.Inventory.AddItem(item);

        // Потім відновлюємо екіпірування
        foreach (var eq in dto.Equipped)
        {
            if (eq.ItemIndex >= 0 && eq.ItemIndex < items.Count &&
                Enum.TryParse<EquipSlot>(eq.Slot, out var slot))
                character.EquipItem(items[eq.ItemIndex], slot);
        }

        // Швидкі слоти
        for (int i = 0; i < dto.QuickSlots.Count; i++)
        {
            var idx = dto.QuickSlots[i];
            if (idx.HasValue && idx.Value < items.Count)
                character.Inventory.AssignQuickSlot(items[idx.Value], i);
        }

        return character;
    }

    private static ItemDto ItemToDto(Item item) => item switch
    {
        Weapon w => new ItemDto("Weapon", w.Name, w.Weight, w.Type.ToString(),
            w.Rarity.ToString(), w.RequiredLevel,
            w.Damage, w.Range, null, null, null, null, null, null),

        Armor a => new ItemDto("Armor", a.Name, a.Weight, a.Type.ToString(),
            a.Rarity.ToString(), a.RequiredLevel,
            null, null, a.Defense, a.Slot.ToString(), null, null, null, null),

        Consumable c => new ItemDto("Consumable", c.Name, c.Weight, c.Type.ToString(),
            c.Rarity.ToString(), c.RequiredLevel,
            null, null, null, null, c.HealAmount, c.Duration, c.Effect, null),

        Resource r => new ItemDto("Resource", r.Name, r.Weight, r.Type.ToString(),
            r.Rarity.ToString(), r.RequiredLevel,
            null, null, null, null, null, null, null, r.Quantity),

        _ => throw new InvalidOperationException($"Невідомий тип: {item.GetType().Name}")
    };

    private static Item DtoToItem(ItemDto dto)
    {
        var rarity = Enum.Parse<Rarity>(dto.Rarity);
        return dto.ItemClass switch
        {
            "Weapon"     => new Weapon(dto.Name, dto.Weight,
                                dto.Damage!.Value, dto.Range!.Value,
                                rarity, dto.RequiredLevel),
            "Armor"      => new Armor(dto.Name, dto.Weight,
                                dto.Defense!.Value,
                                Enum.Parse<EquipSlot>(dto.Slot!),
                                rarity, dto.RequiredLevel),
            "Consumable" => new Consumable(dto.Name, dto.Weight,
                                dto.HealAmount!.Value, dto.Duration!.Value,
                                dto.Effect ?? string.Empty,
                                rarity, dto.RequiredLevel),
            "Resource"   => new Resource(dto.Name, dto.Weight,
                                dto.Quantity!.Value,
                                rarity, dto.RequiredLevel),
            _ => throw new InvalidOperationException(
                     $"Невідомий клас предмета: {dto.ItemClass}")
        };
    }
}
