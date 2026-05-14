using InventoryRPG.Domain;
using Xunit;

namespace InventoryRPG.Tests;

// ── Тести Rarity ─────────────────────────────────────────────
public class RarityTests
{
    [Theory]
    [InlineData(Rarity.Common,    1.0f)]
    [InlineData(Rarity.Uncommon,  1.2f)]
    [InlineData(Rarity.Rare,      1.5f)]
    [InlineData(Rarity.Epic,      2.0f)]
    [InlineData(Rarity.Legendary, 3.0f)]
    public void BonusMultiplier_ReturnsCorrectValue(Rarity rarity, float expected)
    {
        Assert.Equal(expected, rarity.BonusMultiplier(), precision: 1);
    }

    [Fact]
    public void Weapon_EffectiveDamage_ScalesWithRarity()
    {
        var common    = new Weapon("Меч", 3f, 20, 1.5f, Rarity.Common);
        var legendary = new Weapon("Меч", 3f, 20, 1.5f, Rarity.Legendary);
        Assert.Equal(20,  common.EffectiveDamage);
        Assert.Equal(60,  legendary.EffectiveDamage);   // 20 * 3.0
    }

    [Fact]
    public void Consumable_EffectiveHeal_ScalesWithRarity()
    {
        var epic = new Consumable("Зілля", 0.5f, 50, 0, "ефект", Rarity.Epic);
        Assert.Equal(100, epic.EffectiveHeal);           // 50 * 2.0
    }
}

// ── Тести RequiredLevel ──────────────────────────────────────
public class RequiredLevelTests
{
    [Fact]
    public void PickUp_ItemRequiresHigherLevel_ReturnsFail()
    {
        var hero  = new Character("Тест", 100, level: 1);
        var sword = new Weapon("Лук", 2f, 18, 30f, Rarity.Rare, requiredLevel: 3);
        var result = hero.PickUp(sword);
        Assert.False(result.IsSuccess);
        Assert.Contains("рівень", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PickUp_ItemLevelMet_ReturnsOk()
    {
        var hero  = new Character("Тест", 100, level: 3);
        var sword = new Weapon("Лук", 2f, 18, 30f, Rarity.Rare, requiredLevel: 3);
        var result = hero.PickUp(sword);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Item_Constructor_ThrowsOnInvalidRequiredLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Weapon("Меч", 3f, 25, 1.5f, Rarity.Common, requiredLevel: 0));
    }
}

// ── Тести Experience та Level ─────────────────────────────────
public class ExperienceTests
{
    [Fact]
    public void GainExperience_LevelUp_IncreasesLevel()
    {
        var hero = new Character("Тест", 100, level: 1);
        // Level 1→2 потребує 400 XP (2*2*100)
        hero.GainExperience(400);
        Assert.Equal(2, hero.Level);
    }

    [Fact]
    public void GainExperience_MultipleLevel_CorrectLevel()
    {
        var hero = new Character("Тест", 100, level: 1);
        hero.GainExperience(900);  // достатньо для рівня 3 (3*3*100=900)
        Assert.True(hero.Level >= 3);
    }

    [Fact]
    public void GainExperience_ZeroAmount_ReturnsFail()
    {
        var hero = new Character("Тест", 100);
        var r = hero.GainExperience(0);
        Assert.False(r.IsSuccess);
    }
}

// ── Тести CraftingService ─────────────────────────────────────
public class CraftingTests
{
    private static CraftingService MakeService()
    {
        var cs = new CraftingService();
        cs.RegisterRecipe(new CraftingRecipe(
            "Дерев'яний меч",
            new Dictionary<string, int> { ["Дерево"] = 3 },
            new Weapon("Дерев'яний меч", 2f, 12, 1f),
            RequiredLevel: 1));
        return cs;
    }

    [Fact]
    public void Craft_WithSufficientIngredients_ReturnsOk()
    {
        var cs   = MakeService();
        var hero = new Character("Тест", 100, level: 1);
        hero.Inventory.AddItem(new Resource("Дерево", 0.5f, 5));

        var result = cs.Craft(hero, "Дерев'яний меч");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Craft_InsufficientIngredients_ReturnsFail()
    {
        var cs   = MakeService();
        var hero = new Character("Тест", 100, level: 1);
        hero.Inventory.AddItem(new Resource("Дерево", 0.5f, 1)); // потрібно 3

        var result = cs.Craft(hero, "Дерев'яний меч");
        Assert.False(result.IsSuccess);
        Assert.Contains("нгредієнт", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Craft_LowLevel_ReturnsFail()
    {
        var cs = new CraftingService();
        cs.RegisterRecipe(new CraftingRecipe(
            "Рецепт Lv5",
            new Dictionary<string, int> { ["Залізо"] = 1 },
            new Resource("Артефакт", 0.1f, 1),
            RequiredLevel: 5));

        var hero = new Character("Тест", 100, level: 1);
        hero.Inventory.AddItem(new Resource("Залізо", 1f, 3));

        var result = cs.Craft(hero, "Рецепт Lv5");
        Assert.False(result.IsSuccess);
        Assert.Contains("рівень", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Craft_ConsumesIngredients()
    {
        var cs   = MakeService();
        var hero = new Character("Тест", 100, level: 1);
        hero.Inventory.AddItem(new Resource("Дерево", 0.5f, 5));

        cs.Craft(hero, "Дерев'яний меч");

        // Після крафтингу має залишитися 5-3=2 одиниці
        var remaining = hero.Inventory.Items
            .OfType<Resource>()
            .Where(r => r.Name == "Дерево")
            .Sum(r => r.Quantity);
        Assert.Equal(2, remaining);
    }

    [Fact]
    public void Craft_UnknownRecipe_ReturnsFail()
    {
        var cs   = MakeService();
        var hero = new Character("Тест", 100);
        var r = cs.Craft(hero, "Неіснуючий рецепт");
        Assert.False(r.IsSuccess);
    }
}

// ── Тести InventoryQueryService ───────────────────────────────
public class QueryTests
{
    private static List<Item> MakeItems() =>
    [
        new Weapon("Меч", 3f, 25, 1.5f, Rarity.Common),
        new Armor("Броня", 5f, 20, EquipSlot.Body, Rarity.Rare),
        new Consumable("Зілля", 0.3f, 30, 0, "ефект"),
        new Resource("Дерево", 0.5f, 10),
        new Resource("Залізо", 1.0f, 5, Rarity.Uncommon),
    ];

    [Fact]
    public void Filter_ByType_ReturnsOnlyMatchingItems()
    {
        var items  = MakeItems();
        var result = InventoryQueryService
            .Filter(items, new ByTypeFilter(ItemType.Resource)).ToList();
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(ItemType.Resource, i.Type));
    }

    [Fact]
    public void Filter_ByMaxWeight_ReturnsLightItems()
    {
        var items  = MakeItems();
        var result = InventoryQueryService
            .Filter(items, new ByMaxWeightFilter(0.5f)).ToList();
        Assert.All(result, i => Assert.True(i.Weight <= 0.5f));
    }

    [Fact]
    public void GroupByType_ReturnsAllTypes()
    {
        var items  = MakeItems();
        var groups = InventoryQueryService.GroupByType(items);
        Assert.True(groups.ContainsKey(ItemType.Weapon));
        Assert.True(groups.ContainsKey(ItemType.Armor));
    }

    [Fact]
    public void GetStats_CorrectAggregation()
    {
        var items = MakeItems();
        var stats = InventoryQueryService.GetStats(items);
        Assert.Equal(5, stats.TotalCount);
        Assert.True(stats.TotalWeight > 0);
        Assert.NotEmpty(stats.HeaviestItem);
    }

    [Fact]
    public void CompositeFilter_CombinesConditions()
    {
        var items  = MakeItems();
        var filter = new CompositeFilter(
            new ByTypeFilter(ItemType.Resource),
            new ByMaxWeightFilter(0.6f));
        var result = InventoryQueryService.Filter(items, filter).ToList();
        Assert.Single(result);  // тільки Дерево (0.5кг) — не Залізо (1.0кг)
        Assert.Equal("Дерево", result[0].Name);
    }
}
