# Iteration 2 Plan — Lab 35

## Сценарії з backlog що переходять у реалізацію
1. **JSON-серіалізація** — `JsonInventoryRepository` замість InMemory
2. **LINQ-запити** — пошук за типом, вагою, назвою, статистика
3. **Система рідкісності** — `Rarity` enum до `Item`
4. **Розширена бізнес-логіка** — правила обмежень, фільтри, агрегати

## Класи з Lab 34 що залишаються без змін
- `Item`, `Weapon`, `Armor`, `Consumable`, `Resource` — стабільні
- `Equipment` — без змін
- `Result<T>` — без змін
- `IInventoryObserver` — без змін
- `Character` — без змін

## Точки розширення
- `IInventoryRepository` → `JsonInventoryRepository`
- `IItemFilter` (новий) — Strategy для фільтрації
- `IDataStore<T>` (новий) — асинхронний persistence-контракт

## Ризики
- JSON-серіалізація поліморфних Item потребує кастомного JsonConverter
- Сітка InventoryGrid не серіалізується напряму — потрібен DTO
