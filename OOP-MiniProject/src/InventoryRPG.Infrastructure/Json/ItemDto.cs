using System.Text.Json.Serialization;

namespace InventoryRPG.Infrastructure;

/// <summary>
/// DTO для серіалізації Item-ієрархії.
/// Вирішує проблему поліморфної серіалізації без кастомного JsonConverter.
/// </summary>
public sealed class ItemDto
{
    public string   TypeDiscriminator { get; set; } = string.Empty; // "Weapon"|"Armor"|...
    public string   Name              { get; set; } = string.Empty;
    public float    Weight            { get; set; }
    public string   Rarity            { get; set; } = "Common";
    public int      GridX             { get; set; }
    public int      GridY             { get; set; }

    // Weapon
    public int?   Damage { get; set; }
    public float? Range  { get; set; }

    // Armor
    public int?    Defense  { get; set; }
    public string? ArmorSlot { get; set; }

    // Consumable
    public int?    HealAmount { get; set; }
    public string? Effect     { get; set; }

    // Resource
    public int? Quantity { get; set; }
}

public sealed class CharacterSaveDto
{
    public string      Name        { get; set; } = string.Empty;
    public int         MaxHp       { get; set; }
    public int         CurrentHp   { get; set; }
    public List<ItemDto> Items     { get; set; } = new();
    public Dictionary<string, string?> EquippedSlots { get; set; } = new();
    public List<string?> QuickSlots { get; set; } = new();
    public DateTime    SavedAt     { get; set; }
}
