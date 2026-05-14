namespace InventoryRPG.Domain;

/// <summary>
/// Рідкісність предмета — розширення без зміни базового Item.
/// Бізнес-правило: чим вища рідкісність, тим більший бонус до характеристик.
/// </summary>
public enum Rarity
{
    Common    = 0,   // +0%
    Uncommon  = 1,   // +10%
    Rare      = 2,   // +25%
    Epic      = 3,   // +50%
    Legendary = 4    // +100%
}

public static class RarityExtensions
{
    /// <summary>Extension method — LINQ-friendly бонус до статів.</summary>
    public static float BonusMultiplier(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => 1.00f,
        Rarity.Uncommon  => 1.10f,
        Rarity.Rare      => 1.25f,
        Rarity.Epic      => 1.50f,
        Rarity.Legendary => 2.00f,
        _                => 1.00f
    };

    public static string DisplayName(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => "⚪ Звичайний",
        Rarity.Uncommon  => "🟢 Незвичайний",
        Rarity.Rare      => "🔵 Рідкісний",
        Rarity.Epic      => "🟣 Епічний",
        Rarity.Legendary => "🟡 Легендарний",
        _                => "Unknown"
    };
}
