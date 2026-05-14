namespace InventoryRPG.Domain;

// ── Рецепт крафтингу ─────────────────────────────────────────
/// <summary>
/// Value Object — рецепт крафтингу.
/// Незмінний, ідентифікується вмістом, а не посиланням.
/// </summary>
public sealed record CraftingRecipe(
    string                              Name,
    IReadOnlyDictionary<string, int>    Ingredients,  // назва ресурсу → кількість
    Item                                Result,
    int                                 RequiredLevel = 1
)
{
    public bool CanCraft(IEnumerable<Item> available, int characterLevel)
    {
        if (characterLevel < RequiredLevel) return false;

        // Бізнес-правило: всі інгредієнти мають бути в наявності
        var resources = available
            .OfType<Resource>()
            .GroupBy(r => r.Name)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

        return Ingredients.All(req =>
            resources.TryGetValue(req.Key, out var qty) && qty >= req.Value);
    }
}

// ── Сервіс крафтингу ─────────────────────────────────────────
/// <summary>
/// Бізнес-правила крафтингу:
/// 1. Персонаж має потрібний рівень
/// 2. Всі інгредієнти наявні в інвентарі
/// 3. Після крафтингу інгредієнти списуються
/// 4. Новий предмет додається до інвентарю
/// </summary>
public sealed class CraftingService
{
    private readonly List<CraftingRecipe> _recipes = new();

    public IReadOnlyList<CraftingRecipe> Recipes => _recipes.AsReadOnly();

    public void RegisterRecipe(CraftingRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        _recipes.Add(recipe);
    }

    public Result<Item> Craft(Character character, string recipeName)
    {
        // Бізнес-правило 1: рецепт існує
        var recipe = _recipes.FirstOrDefault(r => r.Name == recipeName);
        if (recipe is null)
            return Result<Item>.Fail($"Рецепт «{recipeName}» не знайдено.");

        // Бізнес-правило 2: рівень персонажа достатній
        if (character.Level < recipe.RequiredLevel)
            return Result<Item>.Fail(
                $"Потрібен рівень {recipe.RequiredLevel}, " +
                $"у вас {character.Level}.");

        // Бізнес-правило 3: всі інгредієнти є
        if (!recipe.CanCraft(character.Inventory.Items, character.Level))
            return Result<Item>.Fail("Недостатньо інгредієнтів для крафтингу.");

        // Бізнес-правило 4: списуємо інгредієнти
        foreach (var (name, required) in recipe.Ingredients)
            ConsumeResource(character.Inventory, name, required);

        // Бізнес-правило 5: додаємо результат до інвентарю
        var addResult = character.Inventory.AddItem(recipe.Result);
        if (!addResult.IsSuccess)
            return Result<Item>.Fail($"Не вдалося додати предмет: {addResult.Error}");

        return Result<Item>.Ok(recipe.Result);
    }

    private static void ConsumeResource(Inventory inv, string name, int amount)
    {
        int remaining = amount;
        var resources = inv.Items
            .OfType<Resource>()
            .Where(r => r.Name == name)
            .ToList();

        foreach (var res in resources)
        {
            if (remaining <= 0) break;
            if (res.Quantity <= remaining)
            {
                remaining -= res.Quantity;
                inv.RemoveItem(res);
            }
            else
            {
                // Часткове списання — створюємо новий з меншою кількістю
                inv.RemoveItem(res);
                inv.AddItem(new Resource(res.Name, res.Weight,
                    res.Quantity - remaining, res.Rarity));
                remaining = 0;
            }
        }
    }

    public IEnumerable<CraftingRecipe> GetAvailableRecipes(
        Character character) =>
        _recipes.Where(r => r.CanCraft(
            character.Inventory.Items, character.Level));
}
