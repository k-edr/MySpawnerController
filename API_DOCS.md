# Space Engineers Plugin API Documentation

Документация основана на анализе DLL из `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\`
(версия игры `1209024`).

---

## 1. Архитектура игры

### Стек сборок (снизу вверх)

| DLL | Назначение |
|-----|-----------|
| `VRage.dll` | Базовый движок: сериализация, идентификаторы, Plugins API |
| `VRage.Math.dll` | Математика: Vector3, MatrixD, BoundingBox и т.д. |
| `VRage.Game.dll` | Игровой фреймворк: Entity, Components, ObjectBuilders, Definitions |
| `VRage.Render.dll` | Рендеринг |
| `Sandbox.Common.dll` | ModAPI интерфейсы (`IMyCubeGrid`, `IMyEntities`, `MyAPIGateway`) |
| `Sandbox.Game.dll` | Реализация Sandbox: `MyEntities`, `MySession`, `MyPrefabManager` |
| `SpaceEngineers.Game.dll` | Игровая логика SE |
| `SpaceEngineers.ObjectBuilders.dll` | ObjectBuilders специфичные для SE |
| `PluginLoader.dll` | Загрузчик плагинов (avaness.PluginLoader) |

### Версия .NET

- **Целевой фреймворк**: .NET Framework 4.6.1 (из `SpaceEngineers.exe.config`)
- **Фактический**: совместим с 4.8 (присутствуют сборки System.Memory 4.0.1.2, System.Runtime.CompilerServices.Unsafe 6.0.0.0)

---

## 2. PluginLoader API

### Интерфейс плагина (`VRage.Plugins`)

```csharp
namespace VRage.Plugins
{
    public interface IPlugin
    {
        void Init(object gameInstance);
        void Update();
        void Dispose();
    }
}
```

PluginLoader ищет в сборке класс, реализующий `IPlugin`, создаёт его экземпляр и вызывает:

1. `Init(gameInstance)` — при загрузке плагина. `gameInstance` — экземпляр `SpaceEngineersGame`.
2. `Update()` — каждый кадр.
3. `Dispose()` — при выгрузке.

### Авторегистрация SessionComponent

Классы с атрибутом `[MySessionComponentDescriptor]` автоматически регистрируются в `MySession`:

```csharp
[MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority)]
public class MyComponent : MySessionComponentBase
{
    public override void UpdateAfterSimulation() { }
    public override void UpdateBeforeSimulation() { }
    protected override void UnloadData() { }
}
```

**UpdateOrder** определяет, на каком этапе игрового цикла вызывается компонент:

| Значение | Когда вызывается |
|----------|-----------------|
| `BeforeSimulation` | До физической симуляции |
| `AfterSimulation` | После физической симуляции |
| `NoUpdate` | Только событийная модель |

### Конфигурация PluginLoader

Файл: `Bin64\Plugins\config.xml`
```xml
<PluginConfig>
  <Plugins>
    <Id>D:\...\Bin64\Plugins\MyPlugin.dll</Id>
  </Plugins>
</PluginConfig>
```

---

## 3. MyAPIGateway — главная точка входа

```csharp
Sandbox.ModAPI.MyAPIGateway
```

| Свойство | Тип | Назначение |
|----------|-----|-----------|
| `Session` | `IMySession` | Текущая сессия |
| `Entities` | `IMyEntities` | Все сущности в мире |
| `Players` | — | Игроки |
| `PrefabManager` | `IMyPrefabManager` | Спавн префабов |
| `Utilities` | `IMyUtilities` | I/O, моды, нотификации |
| `Multiplayer` | `IMyMultiplayer` | Сеть |
| `Physics` | — | Физика |
| `Gui` | `IMyGui` | GUI |
| `CubeBuilder` | — | Строительство |
| `TerminalControls` | — | Управление блоками |

---

## 4. Спавн сущностей (IMyEntities)

### Создание из ObjectBuilder

```csharp
// 1. Только создать объект (без добавления в мир)
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilder(objectBuilder);

// 2. Создать и сразу добавить в мир
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(objectBuilder);

// 3. Добавить существующую сущность в мир
MyAPIGateway.Entities.AddEntity(entity, insertIntoScene: true);
```

### Ремап EntityId

При спавне из чертежа/префаба ID сущностей могут конфликтовать. Нужен ремап:

```csharp
// Ремап коллекции object builders
MyAPIGateway.Entities.RemapObjectBuilderCollection(builders);

// Ремап одиночного object builder
MyAPIGateway.Entities.RemapObjectBuilder(builder);
```

### Поиск свободного места

```csharp
Vector3D? pos = MyAPIGateway.Entities.FindFreePlace(
    position,       // Vector3D — желаемая позиция
    sphereRadius,   // float
    maxTestCount,   // int
    testsPerDistance, // int
    stepSize        // float
);
```

---

## 5. Спавн префабов (IMyPrefabManager)

### SpawnPrefab (синхронизированный)

```csharp
MyAPIGateway.PrefabManager.SpawnPrefab(
    resultList,              // List<IMyCubeGrid> — сюда попадут заспавненные гриды
    prefabName,              // string — имя префаба (SubtypeId)
    position,                // Vector3D
    forward,                 // Vector3
    up,                      // Vector3
    initialLinearVelocity,   // Vector3
    initialAngularVelocity,  // Vector3
    beaconName,              // string — имя маяка (опционально)
    spawningOptions,         // SpawningOptions
    ownerId,                 // long — ID владельца
    spawnAtSync,             // bool — синхронизировать по сети
    callback                 // Action — вызывается после создания
);
```

### SpawningOptions (флаги)

```csharp
[Flags]
public enum SpawningOptions
{
    None                        = 0,
    RotateFirstCockpitTowardsDirection = 1 << 0,
    SpawnRandomCargo            = 1 << 1,
    DisableDampeners            = 1 << 2,
    SetNeutralOwner            = 1 << 3,
    TurnOffReactors            = 1 << 4,
    DisableSave                = 1 << 5,
    UseGridOrigin              = 1 << 6,
    SetAuthorship              = 1 << 7,
    ReplaceColor               = 1 << 8,
    UseOnlyWorldMatrix         = 1 << 9,
    RandomizeColor             = 1 << 10,
    SetNpcSpawnedGrid          = 1 << 11,
    SetOwnerNobody             = 1 << 12,
}
```

### MyPrefabManager (внутренний, без синхронизации)

```csharp
// Несинхронизированный спавн
Sandbox.Game.World.MyPrefabManager.AddShipPrefab(
    prefabName,       // string
    position,         // MatrixD?
    ownerId,          // long
    spawnWithWelder   // bool
);

// Спавн на случайной позиции
Sandbox.Game.World.MyPrefabManager.AddShipPrefabRandomPosition(
    prefabName,       // string
    origin,           // Vector3D
    radius,           // float
    ownerId,          // long
    spawnWithWelder   // bool
);
```

---

## 6. Сериализация / Десериализация

### MyObjectBuilderSerializer (публичный API)

```csharp
// ⚠️ DeserializePB НЕ работает для .sbc файлов с xsi:type — возвращает null
// Ниже — что пробовали и почему не сработало:

// Public DeserializePB — для .sbs (Protobuf), не .sbc (XML+xsi:type)
MyObjectBuilderSerializer.DeserializePB<T>(filePath, out result); // → null

// Private Keen DeserializePB — тоже null для SBC
VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializePB(...); // → null
```

### MyObjectBuilderSerializerKeen.DeserializeXML (рабочий)

```csharp
// Единственный работающий метод для десериализации bp.sbc:
using (var stream = File.OpenRead(bpFile))
{
    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
        stream,                                 // Stream (не path!)
        out MyObjectBuilder_Base obj,           // out — результат
        typeof(MyObjectBuilder_Definitions));    // Type — тип корня
    var definitions = obj as MyObjectBuilder_Definitions;
}
```

> `MyObjectBuilderSerializerKeen` лежит в `VRage.ObjectBuilders.Private`.
> Если класс окажется `internal` в будущих версиях игры — использовать
> рефлексию или публичный `MyObjectBuilderSerializer.DeserializeXML`
> (проверять сигнатуру в новых версиях).

### MyObjectBuilder_Definitions

Класс для десериализации SBC файлов. Содержит все типы определений:

```csharp
// VRage.Game.MyObjectBuilder_Definitions
public class MyObjectBuilder_Definitions
{
    public MyObjectBuilder_ShipBlueprintDefinition[] ShipBlueprints;
    public MyObjectBuilder_PrefabDefinition[] Prefabs;
    public MyObjectBuilder_BlueprintClassDefinition[] BlueprintClasses;
    // ... и другие
}
```

### MyObjectBuilder_ShipBlueprintDefinition

Определение чертежа (bp.sbc):

```csharp
public class MyObjectBuilder_ShipBlueprintDefinition
{
    public SerializableDefinitionId Id;    // Type + Subtype
    public ulong OwnerSteamId;
    public ulong WorkshopId;
    public bool Enabled;
    public MyObjectBuilder_CubeGrid[] CubeGrids;  // Гриды в чертеже
}
```

### MyObjectBuilder_CubeGrid

Object builder для грида:

```csharp
public class MyObjectBuilder_CubeGrid : MyObjectBuilder_EntityBase
{
    public string SubtypeName;
    public long EntityId;
    public MyPositionAndOrientation? PositionAndOrientation;
    public MyCubeSize GridSizeEnum;      // Small или Large
    public MyObjectBuilder_CubeBlock[] CubeBlocks;
    // ...
}
```

---

## 7. MySession — Игровая сессия

```csharp
// Текущая сессия
MySession.Static           // MySession
MySession.Static.Ready     // bool — мир полностью загружен
MySession.Static.Name      // string — имя мира
```

---

## 8. MyEntityIdentifier — Управление ID сущностей

```csharp
// Ремап коллекции
MyEntityIdentifier.RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase>)

// Выделение нового ID
MyEntityIdentifier.AllocateId(ID_OBJECT_TYPE, ID_ALLOCATION_METHOD)

// Зарегистрировать ID как используемый
MyEntityIdentifier.MarkIdUsed(long id)
```

---

## 9. MyCubeGrid — Интерфейс грида

```csharp
// Основные свойства через IMyCubeGrid:
grid.EntityId                // long
grid.DisplayName             // string
grid.PositionComp.GetPosition() // Vector3D
grid.GridSize                // float (размер ячейки)
grid.IsStatic                // bool
grid.BlocksCount             // int
grid.GridSystems             // Доступ к системам грида

// Получить все блоки на гриде
List<IMySlimBlock> blocks = new List<IMySlimBlock>();
grid.GetBlocks(blocks);

// Получить блоки определённого типа
List<IMyTerminalBlock> terminals = new List<IMyTerminalBlock>();
grid.GetBlocksOfType<IMyShipConnector>(terminals);
```

---

## 10. IMyGridTerminalSystem — Терминальная сеть

Доступ ко всем блокам, связанным через коннекторы, роторы, пистоны:

```csharp
IMyGridTerminalSystem terminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

// Все блоки в терминальной сети
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
terminalSys.GetBlocks(blocks);

// Блоки по типу
terminalSys.GetBlocksOfType<IMyShipConnector>(blocks, filter);

// Поиск по имени
terminalSys.SearchBlocksOfName("Connector", blocks);
```

---

## 11. Спавн из локального чертежа (рабочий алгоритм)

```csharp
// 1. Загрузить bp.sbc через DeserializeXML (Private namespace!)
MyObjectBuilder_Definitions definitions = null;
using (var stream = File.OpenRead(bpFilePath))
{
    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
        stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
    definitions = obj as MyObjectBuilder_Definitions;
}

// 2. Извлечь ShipBlueprint
var shipBp = definitions.ShipBlueprints[0];

// 3. Извлечь гриды
var grids = new List<MyObjectBuilder_CubeGrid>();
foreach (var grid in shipBp.CubeGrids)
{
    if (grid is MyObjectBuilder_CubeGrid cubeGrid)
        grids.Add(cubeGrid);
}

// 4. Ремап EntityId (публичный IMyEntities)
MyAPIGateway.Entities.RemapObjectBuilderCollection(grids);

// 5. Установить позицию спавна
MatrixD spawnMatrix = MatrixD.CreateWorld(spawnPos, Vector3D.Forward, Vector3D.Up);
gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(spawnMatrix);

// 6. Создать и добавить в мир (публичный IMyEntities)
// ⚠️ Это даёт фантомный грид — визуал есть, физики/инициализации нет
// Подробнее: см. Фантомный грид.md
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder);
```

---

## 12. Запуск игры с загрузкой мира

```batch
SpaceEngineersLauncher.exe -world "Имя Мира"
```

Имя мира должно совпадать с именем папки в:
`%APPDATA%\SpaceEngineers\Saves\<steamid>\<WorldName>\`

Например: `-world "Empty World 2026-07-14 21-40"`

---
