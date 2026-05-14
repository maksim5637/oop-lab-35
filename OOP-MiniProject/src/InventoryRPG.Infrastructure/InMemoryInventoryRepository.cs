using InventoryRPG.Application;
using InventoryRPG.Domain;

namespace InventoryRPG.Infrastructure;

/// <summary>
/// In-memory реалізація репозиторію — для Lab 34.
/// На Lab 35 замінюється на JSON-файловий репозиторій
/// без змін у Application або Domain.
/// </summary>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<string, Inventory> _store = new();

    public void Save(string characterId, Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        _store[characterId] = inventory;
    }

    public Inventory Load(string characterId)
    {
        if (!_store.TryGetValue(characterId, out var inventory))
            throw new KeyNotFoundException(
                $"Інвентар для персонажа '{characterId}' не знайдено.");
        return inventory;
    }

    public bool Exists(string characterId) =>
        _store.ContainsKey(characterId);
}
