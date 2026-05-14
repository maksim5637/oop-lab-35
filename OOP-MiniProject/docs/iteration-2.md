# Iteration 2 — Lab 35

## Use Cases що повністю готові
1. ✅ Підібрати предмет + JSON збереження
2. ✅ Екіпірувати предмет зі слотом
3. ✅ Використати витратний предмет (Consumable)
4. ✅ Зберегти/відновити стан з JSON-файлу
5. ✅ Пошук та фільтрація (Strategy: Type/Rarity/Weight/Name/Composite)
6. ✅ Статистика та LINQ-аналітика

## Бізнес-правила зафіксовані
1. Ефективний бонус = базовий × множник рідкісності (Rarity.BonusMultiplier)
2. Composite-фільтр AND — комбінування стратегій без зміни коду
3. HasEnoughHealingPotions — перевірка виживання (≥2 зілля або ≥50% MaxHP)
4. Пошкоджений JSON не ламає програму — graceful fallback до порожнього стану
5. Item-інваріанти перевіряються в конструкторі (не можна створити некоректний предмет)

## Класи та контракти що змінилися
| Клас | Зміна |
|---|---|
| `Item` | Додано `Rarity`, `ToString()` з іконкою |
| `Weapon/Armor/Consumable` | Додано `EffectiveDamage/Defense/Heal` |
| `IInventoryRepository` | Без змін — InMemory легко замінено JSON |
| `InventoryService` | Додано 5 нових методів (LINQ, persistence) |
| Новий: `IItemFilter` + 5 реалізацій | Strategy Pattern для пошуку |
| Новий: `RarityExtensions` | Extension methods |
| Новий: `InventoryAnalytics` | LINQ extension methods |
| Новий: `JsonInventoryRepository` | Async persistence |

## Ризикові сценарії для Lab 36 (інтеграційні тести)
- Серіалізація/десеріалізація з усіма типами предметів одночасно
- Відновлення швидких слотів після перезапуску
- Конкурентний запис у файл (CancellationToken)

## Що підготовлено під розширення
- `IItemFilter` → новий фільтр = новий клас, нічого не змінюється
- `IDataStore<T>` → можна реалізувати XML або БД без змін у сервісі
- `RarityExtensions` → новий рівень рідкісності = одна нова гілка switch
