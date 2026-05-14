namespace InventoryRPG.Domain;

public interface IItem
{
    string   Name      { get; }
    float    Weight    { get; }
    ItemType Type      { get; }
    Rarity   Rarity    { get; }
    string   Use(Character character);
}

/// <summary>
/// Базовий клас предмета — Factory Method.
/// Lab 35: додано Rarity, EffectiveDamage/Defense через множник.
/// </summary>
public abstract class Item : IItem
{
    public string   Name       { get; }
    public float    Weight     { get; }
    public ItemType Type       { get; }
    public Rarity   Rarity     { get; }
    public int      GridWidth  { get; }
    public int      GridHeight { get; }
    public int      GridX      { get; set; } = -1;
    public int      GridY      { get; set; } = -1;

    protected Item(string name, float weight,
                   int gridW, int gridH,
                   ItemType type,
                   Rarity rarity = Rarity.Common)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва предмета не може бути порожньою.", nameof(name));
        if (weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Вага має бути > 0.");
        if (gridW <= 0 || gridH <= 0)
            throw new ArgumentOutOfRangeException("Розмір сітки має бути > 0.");

        Name       = name;
        Weight     = weight;
        GridWidth  = gridW;
        GridHeight = gridH;
        Type       = type;
        Rarity     = rarity;
    }

    public abstract string Use(Character character);

    public override string ToString() =>
        $"{Rarity.Icon()} [{Type}] {Name} ({Weight:F1}кг, {Rarity})";
}
