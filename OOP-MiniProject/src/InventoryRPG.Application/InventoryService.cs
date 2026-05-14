using InventoryRPG.Domain;
using InventoryRPG.Domain.Filters;

namespace InventoryRPG.Application;

// ── Контракт репозиторію ─────────────────────────────────────
public interface IInventoryRepository
{
    void      Save(string characterId, Inventory inventory);
    Inventory Load(string characterId);
    bool      Exists(string characterId);
}

// ── DTO ──────────────────────────────────────────────────────
public record InventoryReport(
    string CharacterName,
    float  CurrentWeight,
    float  MaxWeight,
    int    ItemCount,
    int    AttackBonus,
    int    DefenseBonus,
    IReadOnlyList<string> ItemNames
);

// ── Сервіс ───────────────────────────────────────────────────
/// <summary>
/// Application-шар: оркеструє Domain, зберігає через репозиторій.
/// Lab 35: додано LINQ-запити, фільтри, збереження/відновлення.
/// </summary>
public sealed class InventoryService
{
    private readonly IInventoryRepository _repo;

    public InventoryService(IInventoryRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    // ── Use Case 1: Підібрати та зберегти ────────────────────
    public Result<bool> AddItem(Character character, Item item)
    {
        var result = character.PickUp(item);
        if (result.IsSuccess)
            _repo.Save(character.Name, character.Inventory);
        return result;
    }

    // ── Use Case 2: Екіпірувати ───────────────────────────────
    public Result<string> EquipItem(Character character,
                                    Item item, EquipSlot slot)
    {
        var result = character.EquipItem(item, slot);
        if (result.IsSuccess)
            _repo.Save(character.Name, character.Inventory);
        return result;
    }

    // ── Use Case 3: Використати предмет ──────────────────────
    public Result<string> UseItem(Character character, Item item) =>
        character.UseItem(item);

    // ── Use Case 4: Зберегти стан ────────────────────────────
    public void SaveState(Character character) =>
        _repo.Save(character.Name, character.Inventory);

    // ── Use Case 5: Відновити стан із файлу ─────────────────
    public Result<bool> RestoreState(Character character)
    {
        if (!_repo.Exists(character.Name))
            return Result<bool>.Fail($"Збереження для «{character.Name}» не знайдено.");
        try
        {
            var saved = _repo.Load(character.Name);
            // Переносимо предмети з збереженого інвентарю
            foreach (var item in saved.Items)
                character.Inventory.AddItem(item);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Помилка відновлення: {ex.Message}");
        }
    }

    // ── LINQ-запити ───────────────────────────────────────────

    /// <summary>Пошук предметів за фільтром (Strategy).</summary>
    public IReadOnlyList<Item> SearchItems(
        Character character, IItemFilter filter) =>
        character.Inventory.Items
            .ApplyFilter(filter)
            .OrderBy(i => i.Name)
            .ToList()
            .AsReadOnly();

    /// <summary>Предмети відсортовані за ефективністю.</summary>
    public IReadOnlyList<Item> GetItemsByEffectiveness(Character character) =>
        character.Inventory.Items
            .OrderByDescending(i => i switch
            {
                Weapon w     => (float)w.EffectiveDamage,
                Armor a      => (float)a.EffectiveDefense,
                Consumable c => (float)c.EffectiveHeal,
                _            => 0f
            })
            .ToList()
            .AsReadOnly();

    /// <summary>Бізнес-правило: перевірка чи достатньо зілля для виживання.</summary>
    public bool HasEnoughHealingPotions(Character character, int minCount = 2)
    {
        var potions = character.Inventory.Items
            .OfType<Consumable>()
            .Sum(c => c.EffectiveHeal);
        return potions >= character.MaxHp * 0.5f || // або загальний хіл >= 50% HP
               character.Inventory.Items.OfType<Consumable>().Count() >= minCount;
    }

    /// <summary>Повна статистика.</summary>
    public InventoryStatistics GetStatistics(Character character) =>
        character.Inventory.GetStatistics(character.Equipment);

    public InventoryReport GetReport(Character character) => new(
        CharacterName : character.Name,
        CurrentWeight : character.Inventory.CurrentWeight,
        MaxWeight     : Inventory.MaxWeight,
        ItemCount     : character.Inventory.Items.Count,
        AttackBonus   : character.Equipment.GetAttackBonus(),
        DefenseBonus  : character.Equipment.GetDefenseBonus(),
        ItemNames     : character.Inventory.Items
                            .Select(i => i.ToString()!)
                            .ToList().AsReadOnly()
    );
}
