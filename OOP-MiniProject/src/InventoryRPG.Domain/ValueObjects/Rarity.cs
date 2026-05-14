namespace InventoryRPG.Domain;

/// <summary>
/// Рідкісність предмета — Value Object.
/// Нові рівні можна додавати без змін у існуючих класах (OCP).
/// </summary>
public enum Rarity
{
    Common    = 0,   // ⚪ Звичайний   — базові бонуси
    Uncommon  = 1,   // 🟢 Незвичайний — +10% бонус
    Rare      = 2,   // 🔵 Рідкісний   — +25% бонус
    Epic      = 3,   // 🟣 Епічний     — +50% бонус
    Legendary = 4,   // 🟡 Легендарний — +100% бонус
}

/// <summary>
/// Розширення для Rarity — без зміни enum (extension methods).
/// </summary>
public static class RarityExtensions
{
    /// <summary>Повертає множник бонусу для рідкісності.</summary>
    public static float BonusMultiplier(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => 1.0f,
        Rarity.Uncommon  => 1.1f,
        Rarity.Rare      => 1.25f,
        Rarity.Epic      => 1.5f,
        Rarity.Legendary => 2.0f,
        _                => 1.0f,
    };

    /// <summary>Повертає колір для консольного відображення.</summary>
    public static ConsoleColor ConsoleColor(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => System.ConsoleColor.Gray,
        Rarity.Uncommon  => System.ConsoleColor.Green,
        Rarity.Rare      => System.ConsoleColor.Cyan,
        Rarity.Epic      => System.ConsoleColor.Magenta,
        Rarity.Legendary => System.ConsoleColor.Yellow,
        _                => System.ConsoleColor.White,
    };

    /// <summary>Повертає іконку рідкісності.</summary>
    public static string Icon(this Rarity rarity) => rarity switch
    {
        Rarity.Common    => "⚪",
        Rarity.Uncommon  => "🟢",
        Rarity.Rare      => "🔵",
        Rarity.Epic      => "🟣",
        Rarity.Legendary => "🟡",
        _                => "⚪",
    };
}
