namespace InventoryRPG.Domain.Crafting;

/// <summary>
/// Рецепт крафтингу — описує які ресурси потрібні для створення предмета.
/// </summary>
public sealed class CraftingRecipe
{
    public string Id          { get; }
    public string ResultName  { get; }
    public ItemType ResultType { get; }

    // Інгредієнти: Tag ресурсу → кількість
    public IReadOnlyDictionary<string, int> Ingredients { get; }

    public CraftingRecipe(string id, string resultName,
                          ItemType resultType,
                          Dictionary<string, int> ingredients)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(nameof(id));
        if (!ingredients.Any())
            throw new ArgumentException("Рецепт не може бути порожнім.", nameof(ingredients));

        Id          = id;
        ResultName  = resultName;
        ResultType  = resultType;
        Ingredients = ingredients.AsReadOnly();
    }

    public override string ToString() =>
        $"{ResultName} ← " +
        string.Join(", ", Ingredients.Select(kv => $"{kv.Value}x{kv.Key}"));
}

/// <summary>
/// Реєстр рецептів — HashSet для швидкого пошуку по Id.
/// </summary>
public sealed class RecipeRegistry
{
    private readonly Dictionary<string, CraftingRecipe> _recipes = new();

    public void Register(CraftingRecipe recipe) =>
        _recipes[recipe.Id] = recipe;

    public CraftingRecipe? Find(string id) =>
        _recipes.GetValueOrDefault(id);

    public IReadOnlyCollection<CraftingRecipe> All =>
        _recipes.Values.ToList().AsReadOnly();
}
