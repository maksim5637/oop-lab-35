using InventoryRPG.Domain;
using InventoryRPG.Application;
using InventoryRPG.Infrastructure;
using Xunit;

namespace InventoryRPG.Tests;

// ── BR-01: Ваговий ліміт ─────────────────────────────────────
public class WeightLimitTests
{
    [Theory]
    [InlineData(24.9f, true)]
    [InlineData(25.0f, true)]
    [InlineData(25.1f, false)]
    public void AddItem_WeightBoundary(float weight, bool expected)
    {
        var inv    = new Inventory();
        var result = inv.AddItem(new Resource("X", weight, 1));
        Assert.Equal(expected, result.IsSuccess);
    }

    [Fact]
    public void AddItem_Cumulative_RespectsLimit()
    {
        var inv = new Inventory();
        inv.AddItem(new Resource("A", 20f, 1));
        inv.AddItem(new Resource("B", 4.9f, 1));
        var r = inv.AddItem(new Resource("C", 1.0f, 1));
        Assert.False(r.IsSuccess);
        Assert.Contains("вага", r.Error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── BR-02: Сітка ─────────────────────────────────────────────
public class GridTests
{
    [Fact]
    public void AddItem_FullGrid_RejectsFurther()
    {
        var inv = new Inventory(2, 2);
        for (int i = 0; i < 4; i++)
            inv.AddItem(new Resource($"R{i}", 0.1f, 1));
        var r = inv.AddItem(new Resource("Extra", 0.1f, 1));
        Assert.False(r.IsSuccess);
        Assert.Contains("сітці", r.Error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── BR-03: Унікальність (HashSet) ────────────────────────────
public class UniqueItemTests
{
    [Fact]
    public void AddItem_SameItemTwice_ReturnsFail()
    {
        var inv   = new Inventory();
        var sword = new Weapon("Меч", 3f, 25, 1.5f);
        inv.AddItem(sword);
        var r = inv.AddItem(sword);
        Assert.False(r.IsSuccess);
        Assert.Contains("вже є", r.Error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── BR-04: Рідкісність + Strategy ────────────────────────────
public class RarityBonusTests
{
    [Theory]
    [InlineData(Rarity.Common,    25, 25)]
    [InlineData(Rarity.Uncommon,  25, 27)]  // ceil(25*1.1)=27
    [InlineData(Rarity.Rare,      25, 31)]  // ceil(25*1.25)=31
    [InlineData(Rarity.Epic,      25, 37)]  // ceil(25*1.5)=37
    [InlineData(Rarity.Legendary, 25, 50)]  // 25*2.0=50
    public void MultiplicativeStrategy_AppliesCorrectBonus(
        Rarity rarity, int baseVal, int expected)
    {
        var strategy = new MultiplicativeRarityBonus();
        Assert.Equal(expected, strategy.ApplyBonus(baseVal, rarity));
    }

    [Fact]
    public void Equipment_AttackBonus_UsesStrategy()
    {
        var eq     = new Equipment();
        var sword  = new Weapon("Меч", 3f, 25, 1.5f, Rarity.Legendary);
        eq.Equip(sword, EquipSlot.Weapon);
        Assert.Equal(50, eq.GetAttackBonus()); // 25 * 2.0
    }

    [Fact]
    public void Equipment_StrategyCanBeSwapped()
    {
        var eq    = new Equipment { BonusStrategy = new AdditiveRarityBonus(5) };
        var sword = new Weapon("Меч", 3f, 25, 1.5f, Rarity.Epic); // tier=3
        eq.Equip(sword, EquipSlot.Weapon);
        Assert.Equal(40, eq.GetAttackBonus()); // 25 + 3*5
    }
}

// ── BR-05: Персонаж мертвий ───────────────────────────────────
public class CharacterAliveTests
{
    [Fact]
    public void UseItem_DeadCharacter_ReturnsFail()
    {
        var repo    = new InMemoryInventoryRepository();
        var service = new InventoryService(repo);
        var hero    = new Character("Тест", 10);
        var potion  = new Consumable("Зілля", 0.3f, 30, 0, "ефект");
        hero.Inventory.AddItem(potion);
        hero.TakeDamage(999); // вбиваємо
        var r = service.UseItem(hero, potion);
        Assert.False(r.IsSuccess);
        Assert.Contains("мертв", r.Error, StringComparison.OrdinalIgnoreCase);
    }
}

// ── Тести Equipment (Strategy) ───────────────────────────────
public class EquipmentTests
{
    [Theory]
    [InlineData(EquipSlot.Weapon, true)]
    [InlineData(EquipSlot.Head,   false)]
    [InlineData(EquipSlot.Body,   false)]
    public void Weapon_CompatibleOnlyWithWeaponSlot(EquipSlot slot, bool expected)
    {
        var eq    = new Equipment();
        var sword = new Weapon("Меч", 3f, 25, 1.5f);
        var r     = eq.Equip(sword, slot);
        Assert.Equal(expected, r.IsSuccess);
    }

    [Theory]
    [InlineData(EquipSlot.Body, EquipSlot.Body, true)]
    [InlineData(EquipSlot.Body, EquipSlot.Head, false)]
    [InlineData(EquipSlot.Head, EquipSlot.Head, true)]
    public void Armor_CompatibleOnlyWithCorrectSlot(
        EquipSlot armorSlot, EquipSlot equipSlot, bool expected)
    {
        var eq    = new Equipment();
        var armor = new Armor("Броня", 5f, 20, armorSlot);
        var r     = eq.Equip(armor, equipSlot);
        Assert.Equal(expected, r.IsSuccess);
    }

    [Fact]
    public void UnequipItem_ReturnsItemToInventory()
    {
        var hero  = new Character("Тест", 100);
        var sword = new Weapon("Меч", 3f, 25, 1.5f);
        hero.PickUp(sword);
        hero.EquipItem(sword, EquipSlot.Weapon);
        hero.UnequipItem(EquipSlot.Weapon);
        Assert.Contains(sword, hero.Inventory.Items);
    }
}

// ── Тести Character ───────────────────────────────────────────
public class CharacterTests
{
    [Fact]
    public void Hp_NeverBelowZero() =>
        Assert.Equal(0, new Character("X", 100) { }.Also(c => c.TakeDamage(999)).Hp);

    [Fact]
    public void Hp_NeverExceedsMaxHp()
    {
        var c = new Character("X", 100);
        c.TakeDamage(50);
        c.Heal(999);
        Assert.Equal(c.MaxHp, c.Hp);
    }

    [Fact]
    public void UseConsumable_HealsAndRemoves()
    {
        var hero   = new Character("X", 100);
        var potion = new Consumable("Зілля", 0.3f, 30, 0, "ефект");
        hero.PickUp(potion);
        hero.TakeDamage(40);
        var r = hero.UseItem(potion);
        Assert.True(r.IsSuccess);
        Assert.Equal(90, hero.Hp);
        Assert.DoesNotContain(potion, hero.Inventory.Items);
    }
}

// ── Тести JSON Persistence ────────────────────────────────────
public class JsonPersistenceTests : IDisposable
{
    private readonly string _testDir = Path.Combine(Path.GetTempPath(), $"inv_test_{Guid.NewGuid()}");

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesItems()
    {
        var repo = new JsonInventoryRepository(_testDir);
        var inv  = new Inventory();
        inv.AddItem(new Weapon("Меч",  3f, 25, 1.5f, Rarity.Rare));
        inv.AddItem(new Resource("Дерево", 0.5f, 10));

        await repo.SaveAsync("hero", inv);
        var loaded = await repo.LoadAsync("hero");

        Assert.Equal(2, loaded.Items.Count);
        Assert.Contains(loaded.Items, i => i.Name == "Меч" && i.Rarity == Rarity.Rare);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFoundException()
    {
        var repo = new JsonInventoryRepository(_testDir);
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => repo.LoadAsync("does_not_exist"));
    }

    [Fact]
    public async Task LoadAsync_CorruptedJson_ThrowsInvalidOperation()
    {
        var repo = new JsonInventoryRepository(_testDir);
        Directory.CreateDirectory(_testDir);
        await File.WriteAllTextAsync(Path.Combine(_testDir, "bad.json"), "{ NOT JSON }");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.LoadAsync("bad"));
    }

    [Fact]
    public void Exists_ReturnsTrueAfterSave()
    {
        var repo = new JsonInventoryRepository(_testDir);
        var inv  = new Inventory();
        repo.Save("hero2", inv);
        Assert.True(repo.Exists("hero2"));
        Assert.False(repo.Exists("nobody"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}

// ── Тести LINQ-запитів ────────────────────────────────────────
public class LinqQueryTests
{
    private Inventory MakeInventory()
    {
        var inv = new Inventory();
        inv.AddItem(new Weapon("Меч",     3f,   25, 1.5f, Rarity.Rare));
        inv.AddItem(new Armor ("Шолом",   2f,   15, EquipSlot.Head, Rarity.Common));
        inv.AddItem(new Consumable("Зілля", 0.3f, 30, 0, "ефект", Rarity.Uncommon));
        inv.AddItem(new Resource("Дерево", 0.5f, 10));
        return inv;
    }

    [Fact]
    public void GetByType_ReturnsOnlyMatchingType()
    {
        var inv   = MakeInventory();
        var items = inv.GetByType(ItemType.Weapon).ToList();
        Assert.Single(items);
        Assert.All(items, i => Assert.Equal(ItemType.Weapon, i.Type));
    }

    [Fact]
    public void GetByMaxWeight_ReturnsOrderedItems()
    {
        var inv   = MakeInventory();
        var items = inv.GetByMaxWeight(1.0f).ToList();
        Assert.All(items, i => Assert.True(i.Weight <= 1.0f));
        Assert.Equal(items, items.OrderBy(i => i.Weight).ToList());
    }

    [Fact]
    public void GetStats_ReturnsCorrectCounts()
    {
        var inv   = MakeInventory();
        var stats = inv.GetStats();
        Assert.Equal(4, stats.TotalItems);
        Assert.Equal(1, stats.ByType[ItemType.Weapon]);
        Assert.Equal(1, stats.ByRarity[Rarity.Rare]);
    }
}

// ── Тести інваріантів ─────────────────────────────────────────
public class InvariantTests
{
    [Fact]
    public void Weapon_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => new Weapon("", 1f, 10, 1f));

    [Fact]
    public void Weapon_ZeroWeight_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weapon("X", 0f, 10, 1f));

    [Fact]
    public void Armor_WeaponSlot_Throws() =>
        Assert.Throws<ArgumentException>(() => new Armor("X", 1f, 10, EquipSlot.Weapon));

    [Fact]
    public void Resource_ZeroQuantity_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Resource("X", 1f, 0));
}

// ── Хелпер ───────────────────────────────────────────────────
internal static class TestExtensions
{
    public static T Also<T>(this T self, Action<T> action) { action(self); return self; }
}
