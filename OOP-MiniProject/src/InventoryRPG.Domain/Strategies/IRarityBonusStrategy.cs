namespace InventoryRPG.Domain;

/// <summary>
/// Strategy Pattern — підрахунок бонусу від рідкісності.
/// Дозволяє додати нову формулу бонусу без зміни Item або Equipment.
/// </summary>
public interface IRarityBonusStrategy
{
    int ApplyBonus(int baseValue, Rarity rarity);
}

/// <summary>Мультиплікативний бонус (поточна реалізація).</summary>
public sealed class MultiplicativeRarityBonus : IRarityBonusStrategy
{
    public int ApplyBonus(int baseValue, Rarity rarity) =>
        (int)(baseValue * rarity.BonusMultiplier());
}

/// <summary>
/// Адитивний бонус — альтернативна стратегія.
/// Додається на Lab 37 без змін в існуючому коді.
/// </summary>
public sealed class AdditiveRarityBonus : IRarityBonusStrategy
{
    private readonly int _bonusPerTier;
    public AdditiveRarityBonus(int bonusPerTier = 5)
        => _bonusPerTier = bonusPerTier;

    public int ApplyBonus(int baseValue, Rarity rarity) =>
        baseValue + (int)rarity * _bonusPerTier;
}
