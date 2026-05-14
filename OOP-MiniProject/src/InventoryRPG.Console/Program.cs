using InventoryRPG.Application;
using InventoryRPG.Domain;
using InventoryRPG.Domain.Filters;
using InventoryRPG.Infrastructure;

// ── Composition Root ─────────────────────────────────────────
const string SaveFile = "inventory_save.json";
var repo    = new JsonInventoryRepository(SaveFile);
var service = new InventoryService(repo);
var hero    = new Character("Артем", 120);

hero.Inventory.Subscribe(new ConsoleObserver());

// Намагаємось відновити збереження
var restore = service.RestoreState(hero);
if (restore.IsSuccess)
    PrintColored("✅ Стан відновлено з файлу.", ConsoleColor.Green);
else
    SeedItems(); // Якщо нема збереження — додаємо тестові предмети

bool running = true;
while (running)
{
    Console.Clear();
    PrintHeader();
    PrintStats(hero);
    PrintMainMenu();
    var key = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    switch (key)
    {
        case ConsoleKey.D1: RunAddItems();       break;
        case ConsoleKey.D2: RunShowInventory();  break;
        case ConsoleKey.D3: RunEquip();          break;
        case ConsoleKey.D4: RunUseItem();        break;
        case ConsoleKey.D5: RunSearch();         break;
        case ConsoleKey.D6: RunStatistics();     break;
        case ConsoleKey.D7: RunSave();           break;
        case ConsoleKey.D8: RunTakeDamage();     break;
        case ConsoleKey.Q:  running = false;     break;
    }

    if (running) { Console.WriteLine("\nНатисніть будь-яку клавішу..."); Console.ReadKey(true); }
}
Console.WriteLine("До побачення!");

// ── Сценарій 1: Додати предмети ──────────────────────────────
void SeedItems()
{
    var items = new Item[]
    {
        new Weapon("Сталевий меч",     3.5f, 25, 1.5f, Rarity.Common),
        new Weapon("Ельфійський лук",  2.0f, 18, 30f,  Rarity.Rare),
        new Weapon("Легендарний спис", 4.0f, 40, 3.0f, Rarity.Legendary),
        new Armor("Нагрудник лицаря",  8.0f, 30, EquipSlot.Body, Rarity.Uncommon),
        new Armor("Шолом варвара",     3.0f, 15, EquipSlot.Head, Rarity.Common),
        new Consumable("Мале зілля",   0.3f, 30, 0, "зцілення",   Rarity.Common),
        new Consumable("Велике зілля", 0.5f, 80, 5, "регенерація",Rarity.Rare),
        new Resource("Дерево",         0.5f, 10, Rarity.Common),
        new Resource("Залізна руда",   1.0f,  5, Rarity.Uncommon),
    };
    foreach (var i in items) hero.PickUp(i);
}

void RunAddItems()
{
    Console.WriteLine("── Додати нові предмети ──");
    Console.Write("  Назва: "); var name = Console.ReadLine() ?? "Предмет";
    Console.Write("  Тип (1-Weapon, 2-Armor, 3-Consumable, 4-Resource): ");
    var typeChoice = Console.ReadKey(true).KeyChar;
    Console.Write("  Рідкісність (0-Common..4-Legendary): ");
    var rarityChoice = Console.ReadKey(true).KeyChar;
    Console.WriteLine();
    var rarity = Enum.TryParse<Rarity>(rarityChoice.ToString(), out var r) ? r : Rarity.Common;

    Item? item = typeChoice switch
    {
        '1' => new Weapon(name, 3.0f, 20, 1.5f, rarity),
        '2' => new Armor(name, 5.0f, 20, EquipSlot.Body, rarity),
        '3' => new Consumable(name, 0.3f, 40, 0, "ефект", rarity),
        '4' => new Resource(name, 1.0f, 5, rarity),
        _   => null
    };

    if (item == null) { Console.WriteLine("❌ Невірний тип."); return; }
    var result = service.AddItem(hero, item);
    Console.WriteLine(result.IsSuccess ? $"✅ {item}" : $"❌ {result.Error}");
}

// ── Сценарій 2: Переглянути інвентар ─────────────────────────
void RunShowInventory()
{
    Console.WriteLine("── Інвентар ──");
    var grouped = hero.Inventory.GroupByType();
    foreach (var (type, items) in grouped)
    {
        Console.WriteLine($"\n  [{type}] ({items.Count} предметів)");
        foreach (var item in items)
        {
            Console.ForegroundColor = item.Rarity.ConsoleColor();
            Console.WriteLine($"    • {item}  @ ({item.GridX},{item.GridY})");
            Console.ResetColor();
        }
    }
    Console.WriteLine($"\n  ⚖ Вага: {hero.Inventory.CurrentWeight:F1}/{Inventory.MaxWeight} кг");
}

// ── Сценарій 3: Екіпірування ─────────────────────────────────
void RunEquip()
{
    Console.WriteLine("── Екіпірування ──");
    var weapons = hero.Inventory.Items.OfType<Weapon>().ToList();
    var armors  = hero.Inventory.Items.OfType<Armor>().ToList();

    if (!weapons.Any() && !armors.Any())
    { Console.WriteLine("  Нема що екіпірувати."); return; }

    foreach (var w in weapons)
    {
        var r = service.EquipItem(hero, w, EquipSlot.Weapon);
        Console.WriteLine(r.IsSuccess ? $"  ✅ {r.Value}" : $"  ❌ {r.Error}");
        break; // Екіпіруємо першу зброю
    }
    foreach (var a in armors)
    {
        var r = service.EquipItem(hero, a, a.Slot);
        Console.WriteLine(r.IsSuccess ? $"  ✅ {r.Value}" : $"  ❌ {r.Error}");
    }
}

// ── Сценарій 4: Використати предмет ─────────────────────────
void RunUseItem()
{
    var potions = hero.Inventory.Items.OfType<Consumable>().ToList();
    if (!potions.Any()) { Console.WriteLine("  Немає зілля."); return; }

    hero.TakeDamage(30);
    Console.WriteLine($"  💔 Отримано 30 шкоди. HP = {hero.Hp}/{hero.MaxHp}");

    var result = service.UseItem(hero, potions[0]);
    Console.WriteLine(result.IsSuccess
        ? $"  ✅ {result.Value}\n  HP = {hero.Hp}/{hero.MaxHp}"
        : $"  ❌ {result.Error}");
}

// ── Сценарій 5: Пошук і фільтрація ──────────────────────────
void RunSearch()
{
    Console.WriteLine("── Пошук ──");
    Console.WriteLine("  [1] За типом   [2] За рідкісністю   [3] За вагою   [4] Комбінований");
    var choice = Console.ReadKey(true).KeyChar;
    Console.WriteLine();

    IItemFilter filter = choice switch
    {
        '1' => new TypeFilter(ItemType.Weapon),
        '2' => new RarityFilter(Rarity.Rare),
        '3' => new MaxWeightFilter(3.0f),
        '4' => new TypeFilter(ItemType.Weapon).And(new RarityFilter(Rarity.Rare)),
        _   => new TypeFilter(ItemType.Weapon)
    };

    var results = service.SearchItems(hero, filter);
    Console.WriteLine($"  Фільтр: {filter.Description} → {results.Count} результатів");
    foreach (var item in results)
    {
        Console.ForegroundColor = item.Rarity.ConsoleColor();
        Console.WriteLine($"    • {item}");
        Console.ResetColor();
    }

    // Топ-зброя
    Console.WriteLine("\n  Топ-3 зброї за ефективністю:");
    foreach (var w in hero.Inventory.TopWeapons(3))
    {
        Console.ForegroundColor = w.Rarity.ConsoleColor();
        Console.WriteLine($"    ⚔ {w.Name} ({w.Rarity}): {w.EffectiveDamage} ефективної шкоди");
        Console.ResetColor();
    }
}

// ── Сценарій 6: Статистика ────────────────────────────────────
void RunStatistics()
{
    var stats = service.GetStatistics(hero);
    Console.WriteLine("── Статистика інвентарю ──");
    Console.WriteLine($"  Всього предметів : {stats.TotalItems}");
    Console.WriteLine($"  Вага             : {stats.TotalWeight:F1}/{stats.MaxWeight} кг " +
                      $"({stats.WeightUsagePercent:F0}%)");
    Console.WriteLine($"  Атака/Захист     : +{stats.TotalAttackBonus} / +{stats.TotalDefenseBonus}");
    Console.WriteLine($"  Зброя: {stats.WeaponCount}  Броня: {stats.ArmorCount}  " +
                      $"Зілля: {stats.ConsumableCount}  Ресурси: {stats.ResourceCount}");
    Console.WriteLine("\n  За рідкісністю:");
    foreach (var (rarity, count) in stats.ItemsByRarity.OrderBy(kv => kv.Key))
    {
        Console.ForegroundColor = rarity.ConsoleColor();
        Console.WriteLine($"    {rarity.Icon()} {rarity,-12}: {count}");
        Console.ResetColor();
    }
    if (stats.HeaviestItem is { } h)
        Console.WriteLine($"\n  Найважчий    : {h.Name} ({h.Weight:F1}кг)");
    if (stats.MostPowerfulItem is { } p)
        Console.WriteLine($"  Найпотужніший: {p.Name} ({p.EffectiveDamage} ефективної шкоди)");

    var hasPotions = service.HasEnoughHealingPotions(hero);
    Console.WriteLine($"\n  Достатньо зілля: {(hasPotions ? "✅ Так" : "⚠ Ні")}");
}

// ── Збереження ───────────────────────────────────────────────
void RunSave()
{
    service.SaveState(hero);
    PrintColored($"✅ Збережено у {SaveFile}", ConsoleColor.Green);
}

void RunTakeDamage()
{
    hero.TakeDamage(20);
    Console.WriteLine($"  💔 Отримано 20 шкоди. HP = {hero.Hp}/{hero.MaxHp}");
}

// ── UI-хелпери ───────────────────────────────────────────────
void PrintHeader()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("╔═══════════════════════════════════════╗");
    Console.WriteLine("║    InventoryRPG — Lab 35 (Ітерація 2) ║");
    Console.WriteLine("╚═══════════════════════════════════════╝");
    Console.ResetColor();
}

void PrintStats(Character c)
{
    Console.ForegroundColor = c.Hp < 40 ? ConsoleColor.Red : ConsoleColor.Green;
    Console.Write($"  ❤ HP: {c.Hp}/{c.MaxHp} ({c.HpPercent:F0}%)  ");
    Console.ResetColor();
    Console.WriteLine($"⚔ ATK: {c.Equipment.GetAttackBonus()}  🛡 DEF: {c.Equipment.GetDefenseBonus()}  " +
                      $"⚖ {c.Inventory.CurrentWeight:F1}/{Inventory.MaxWeight}кг");
}

void PrintMainMenu()
{
    Console.WriteLine();
    Console.WriteLine("  [1] Додати предмет      [5] Пошук та фільтрація");
    Console.WriteLine("  [2] Переглянути інвентар [6] Статистика");
    Console.WriteLine("  [3] Екіпірувати         [7] Зберегти");
    Console.WriteLine("  [4] Використати зілля   [8] Отримати удар");
    Console.WriteLine("  [Q] Вийти");
    Console.Write("\n  Вибір: ");
}

void PrintColored(string msg, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ResetColor();
}

// ── Observer ─────────────────────────────────────────────────
class ConsoleObserver : IInventoryObserver
{
    public void OnInventoryChanged(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  [LOG] {message}");
        Console.ResetColor();
    }
}
