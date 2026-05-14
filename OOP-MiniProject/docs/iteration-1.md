# Iteration 1 — Lab 34

## Що вже працює
- Повна предметна модель: `Item`, `Weapon`, `Armor`, `Consumable`, `Resource`
- `Character` координує `Inventory` та `Equipment`
- `Inventory` перевіряє ваговий ліміт (25 кг) та розміщення у сітці
- `Equipment` перевіряє сумісність слотів (Strategy Pattern)
- `Result<T>` замість bool для всіх операцій
- `InventoryService` — Application шар з DI через конструктор
- `InMemoryInventoryRepository` — Infrastructure шар
- Консольне меню — повний вертикальний зріз
- Observer — консоль отримує сповіщення про зміни інвентарю
- 20+ юніт-тестів з Theory/InlineData
- CI через GitHub Actions

## Артефакти у репозиторії
| Артефакт | Шлях |
|---|---|
| Постановка задачі | `docs/vision.md` |
| Backlog | `docs/backlog.md` |
| Діаграма класів | `docs/class-diagram.md` |
| Діаграма послідовності | `docs/sequence-diagram.md` |
| Domain | `src/InventoryRPG.Domain/` |
| Application | `src/InventoryRPG.Application/` |
| Infrastructure | `src/InventoryRPG.Infrastructure/` |
| Console UI | `src/InventoryRPG.Console/` |
| Тести | `tests/InventoryRPG.Tests/` |
| CI | `.github/workflows/dotnet.yml` |

## Сценарії для Lab 35
1. **JSON-серіалізація** — замінити `InMemoryInventoryRepository` на
   `JsonInventoryRepository` без змін у Domain/Application
2. **LINQ-запити** — пошук предметів за типом, вагою, назвою через консольний фільтр
3. **Система рідкісності** — додати `Rarity` enum до `Item` без змін в існуючих класах

## Ризики та невизначеності
- JSON-серіалізація поліморфних об'єктів (`Item` → підкласи) потребує
  кастомного JsonConverter — треба дослідити на Lab 35
- Сітка (InventoryGrid) зберігається як двовимірний масив — серіалізація
  потребуватиме окремого DTO

## Класи підготовлені під розширення
| Клас/Інтерфейс | Підготовлений для |
|---|---|
| `IInventoryRepository` | Заміна InMemory → JSON без змін у сервісі |
| `Item` (abstract) | Нові типи предметів (Amulet, Ring) — лише новий підклас |
| `Equipment` | Нові слоти (Ring1, Ring2) — лише додати у словник |
| `Result<T>` | Розширена обробка помилок, chain of responsibility |
| `IInventoryObserver` | Логування у файл, UI-оновлення |
