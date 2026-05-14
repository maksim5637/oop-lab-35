namespace InventoryRPG.Domain;

/// <summary>Рідкісність предмета — впливає на бонус характеристик.</summary>
public enum Rarity
{
    Common    = 0,
    Uncommon  = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}

/// <summary>
/// Розширення класу Item — нові поля Rarity та RequiredLevel.
/// Підкласи не змінюються (OCP).
/// </summary>
public static class RarityExtensions
{
    /// <summary>Множник бонусу залежно від рідкісності.</summary>
    public static float BonusMultiplier(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => 1.0f,
        Rarity.Uncommon  => 1.2f,
        Rarity.Rare      => 1.5f,
        Rarity.Epic      => 2.0f,
        Rarity.Legendary => 3.0f,
        _                => 1.0f
    };

    public static string DisplayColor(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => "сірий",
        Rarity.Uncommon  => "зелений",
        Rarity.Rare      => "синій",
        Rarity.Epic      => "фіолетовий",
        Rarity.Legendary => "золотий",
        _                => "сірий"
    };
}
