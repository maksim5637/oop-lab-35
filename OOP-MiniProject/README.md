# InventoryRPG

Система управління інвентарем персонажа рольової гри.
Підсумковий міні-проєкт, Лабораторна робота №34.

## Запуск

```bash
cd src/InventoryRPG.Console
dotnet run
```

## Запуск тестів

```bash
dotnet test
```

## Структура проєкту

```
InventoryRPG/
├── src/
│   ├── InventoryRPG.Domain/        # Сутності, інтерфейси, Result<T>
│   ├── InventoryRPG.Application/   # Сервіси, DTO, IInventoryRepository
│   ├── InventoryRPG.Infrastructure/# InMemoryRepository
│   └── InventoryRPG.Console/       # Консольне меню, Observer
├── tests/
│   └── InventoryRPG.Tests/         # xUnit тести
├── docs/
│   ├── vision.md                   # Постановка задачі
│   ├── backlog.md                  # Ітерації 1-4
│   ├── class-diagram.md            # UML діаграма класів
│   ├── sequence-diagram.md         # UML діаграма послідовності
│   └── iteration-1.md              # Передача до Lab 35
├── .github/workflows/dotnet.yml    # CI
└── README.md
```

## Патерни проектування

| Патерн | Де використовується |
|---|---|
| Factory Method | `Item` → `Weapon`, `Armor`, `Consumable`, `Resource` |
| Strategy | `Equipment._IsCompatible()` |
| Observer | `Inventory` → `IInventoryObserver` → `ConsoleObserver` |
| Composite | `InventoryGrid` + `GridCell` |
| Result\<T\> | Всі операції замість bool/винятків |

## Архітектура (MVC / Layered)

```
Console (View + Controller)
    ↓
InventoryService (Application)
    ↓
Character → Inventory → Equipment (Domain)
    ↓
IInventoryRepository (Infrastructure)
```
