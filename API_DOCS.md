# Space Engineers Plugin API Documentation

Documentation is based on DLL analysis from `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\`
(game version `1209024`).

---

## 1. Game Architecture

### Assembly Stack (bottom to top)

| DLL | Purpose |
|-----|---------|
| `VRage.dll` | Core engine: serialization, identifiers, Plugins API |
| `VRage.Math.dll` | Math: Vector3, MatrixD, BoundingBox, etc. |
| `VRage.Game.dll` | Game framework: Entity, Components, ObjectBuilders, Definitions |
| `VRage.Render.dll` | Rendering |
| `Sandbox.Common.dll` | ModAPI interfaces (`IMyCubeGrid`, `IMyEntities`, `MyAPIGateway`) |
| `Sandbox.Game.dll` | Sandbox implementation: `MyEntities`, `MySession`, `MyPrefabManager` |
| `SpaceEngineers.Game.dll` | SE game logic |
| `SpaceEngineers.ObjectBuilders.dll` | SE-specific ObjectBuilders |
| `PluginLoader.dll` | Plugin loader (avaness.PluginLoader) |

### .NET Version

- **Target framework**: .NET Framework 4.6.1 (from `SpaceEngineers.exe.config`)
- **Actual**: Compatible with 4.8 (assemblies present: System.Memory 4.0.1.2, System.Runtime.CompilerServices.Unsafe 6.0.0.0)

---

## 2. PluginLoader API

### Plugin Interface (`VRage.Plugins`)

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

PluginLoader searches the assembly for a class implementing `IPlugin`, creates an instance, and calls:

1. `Init(gameInstance)` — on plugin load. `gameInstance` is a `SpaceEngineersGame` instance.
2. `Update()` — every frame.
3. `Dispose()` — on unload.

### Auto-Registration of SessionComponent

Classes with the `[MySessionComponentDescriptor]` attribute are auto-registered in `MySession`:

```csharp
[MySessionComponentDescriptor(MyUpdateOrder updateOrder, int priority)]
public class MyComponent : MySessionComponentBase
{
    public override void UpdateAfterSimulation() { }
    public override void UpdateBeforeSimulation() { }
    protected override void UnloadData() { }
}
```

**UpdateOrder** determines at which stage of the game loop the component is called:

| Value | When called |
|-------|-------------|
| `BeforeSimulation` | Before physics simulation |
| `AfterSimulation` | After physics simulation |
| `NoUpdate` | Event-driven only |

### PluginLoader Configuration

File: `Bin64\Plugins\config.xml`
```xml
<PluginConfig>
  <Plugins>
    <Id>D:\...\Bin64\Plugins\MyPlugin.dll</Id>
  </Plugins>
</PluginConfig>
```

---

## 3. MyAPIGateway — Main Entry Point

```csharp
Sandbox.ModAPI.MyAPIGateway
```

| Property | Type | Purpose |
|----------|------|---------|
| `Session` | `IMySession` | Current session |
| `Entities` | `IMyEntities` | All entities in the world |
| `Players` | — | Players |
| `PrefabManager` | `IMyPrefabManager` | Prefab spawning |
| `Utilities` | `IMyUtilities` | I/O, mods, notifications |
| `Multiplayer` | `IMyMultiplayer` | Networking |
| `Physics` | — | Physics |
| `Gui` | `IMyGui` | GUI |
| `CubeBuilder` | — | Building |
| `TerminalControls` | — | Block control |

---

## 4. Entity Spawning (IMyEntities)

### Creating from ObjectBuilder

```csharp
// 1. Create object only (without adding to world)
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilder(objectBuilder);

// 2. Create and immediately add to world
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(objectBuilder);

// 3. Add existing entity to world
MyAPIGateway.Entities.AddEntity(entity, insertIntoScene: true);
```

### EntityId Remapping

When spawning from a blueprint/prefab, entity IDs may conflict. Remapping is required:

```csharp
// Remap a collection of object builders
MyAPIGateway.Entities.RemapObjectBuilderCollection(builders);

// Remap a single object builder
MyAPIGateway.Entities.RemapObjectBuilder(builder);
```

### Finding Free Space

```csharp
Vector3D? pos = MyAPIGateway.Entities.FindFreePlace(
    position,         // Vector3D — desired position
    sphereRadius,     // float
    maxTestCount,     // int
    testsPerDistance, // int
    stepSize          // float
);
```

---

## 5. Prefab Spawning (IMyPrefabManager)

### SpawnPrefab (synchronized)

```csharp
MyAPIGateway.PrefabManager.SpawnPrefab(
    resultList,              // List<IMyCubeGrid> — spawned grids will be placed here
    prefabName,              // string — prefab name (SubtypeId)
    position,                // Vector3D
    forward,                 // Vector3
    up,                      // Vector3
    initialLinearVelocity,   // Vector3
    initialAngularVelocity,  // Vector3
    beaconName,              // string — beacon name (optional)
    spawningOptions,         // SpawningOptions
    ownerId,                 // long — owner ID
    spawnAtSync,             // bool — network-synced
    callback                 // Action — called after creation
);
```

### SpawningOptions (flags)

```csharp
[Flags]
public enum SpawningOptions
{
    None                        = 0,
    RotateFirstCockpitTowardsDirection = 1 << 0,
    SpawnRandomCargo            = 1 << 1,
    DisableDampeners            = 1 << 2,
    SetNeutralOwner             = 1 << 3,
    TurnOffReactors             = 1 << 4,
    DisableSave                 = 1 << 5,
    UseGridOrigin               = 1 << 6,
    SetAuthorship               = 1 << 7,
    ReplaceColor                = 1 << 8,
    UseOnlyWorldMatrix          = 1 << 9,
    RandomizeColor              = 1 << 10,
    SetNpcSpawnedGrid           = 1 << 11,
    SetOwnerNobody              = 1 << 12,
}
```

### MyPrefabManager (internal, non-synchronized)

```csharp
// Non-synchronized spawn
Sandbox.Game.World.MyPrefabManager.AddShipPrefab(
    prefabName,       // string
    position,         // MatrixD?
    ownerId,          // long
    spawnWithWelder   // bool
);

// Spawn at random position
Sandbox.Game.World.MyPrefabManager.AddShipPrefabRandomPosition(
    prefabName,       // string
    origin,           // Vector3D
    radius,           // float
    ownerId,          // long
    spawnWithWelder   // bool
);
```

---

## 6. Serialization / Deserialization

### MyObjectBuilderSerializer (public API)

```csharp
// ⚠️ DeserializePB does NOT work for .sbc files with xsi:type — returns null
// Below: what we tried and why it didn't work:

// Public DeserializePB — for .sbs (Protobuf), not .sbc (XML+xsi:type)
MyObjectBuilderSerializer.DeserializePB<T>(filePath, out result); // → null

// Private Keen DeserializePB — also null for SBC
VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializePB(...); // → null
```

### MyObjectBuilderSerializerKeen.DeserializeXML (working)

```csharp
// The only working method for deserializing bp.sbc:
using (var stream = File.OpenRead(bpFile))
{
    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
        stream,                                 // Stream (not path!)
        out MyObjectBuilder_Base obj,           // out — result
        typeof(MyObjectBuilder_Definitions));    // Type — root type
    var definitions = obj as MyObjectBuilder_Definitions;
}
```

> `MyObjectBuilderSerializerKeen` lives in `VRage.ObjectBuilders.Private`.
> If the class becomes `internal` in future game versions — use
> reflection or the public `MyObjectBuilderSerializer.DeserializeXML`
> (check the signature in new versions).

### MyObjectBuilder_Definitions

Class for deserializing SBC files. Contains all definition types:

```csharp
// VRage.Game.MyObjectBuilder_Definitions
public class MyObjectBuilder_Definitions
{
    public MyObjectBuilder_ShipBlueprintDefinition[] ShipBlueprints;
    public MyObjectBuilder_PrefabDefinition[] Prefabs;
    public MyObjectBuilder_BlueprintClassDefinition[] BlueprintClasses;
    // ... and others
}
```

### MyObjectBuilder_ShipBlueprintDefinition

Blueprint definition (bp.sbc):

```csharp
public class MyObjectBuilder_ShipBlueprintDefinition
{
    public SerializableDefinitionId Id;    // Type + Subtype
    public ulong OwnerSteamId;
    public ulong WorkshopId;
    public bool Enabled;
    public MyObjectBuilder_CubeGrid[] CubeGrids;  // Grids in the blueprint
}
```

### MyObjectBuilder_CubeGrid

Object builder for a grid:

```csharp
public class MyObjectBuilder_CubeGrid : MyObjectBuilder_EntityBase
{
    public string SubtypeName;
    public long EntityId;
    public MyPositionAndOrientation? PositionAndOrientation;
    public MyCubeSize GridSizeEnum;      // Small or Large
    public MyObjectBuilder_CubeBlock[] CubeBlocks;
    // ...
}
```

---

## 7. MySession — Game Session

```csharp
// Current session
MySession.Static           // MySession
MySession.Static.Ready     // bool — world fully loaded
MySession.Static.Name      // string — world name
```

---

## 8. MyEntityIdentifier — Entity ID Management

```csharp
// Remap a collection
MyEntityIdentifier.RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase>)

// Allocate a new ID
MyEntityIdentifier.AllocateId(ID_OBJECT_TYPE, ID_ALLOCATION_METHOD)

// Register an ID as used
MyEntityIdentifier.MarkIdUsed(long id)
```

---

## 9. MyCubeGrid — Grid Interface

```csharp
// Key properties via IMyCubeGrid:
grid.EntityId                // long
grid.DisplayName             // string
grid.PositionComp.GetPosition() // Vector3D
grid.GridSize                // float (cell size)
grid.IsStatic                // bool
grid.BlocksCount             // int
grid.GridSystems             // Access to grid systems

// Get all blocks on the grid
List<IMySlimBlock> blocks = new List<IMySlimBlock>();
grid.GetBlocks(blocks);

// Get blocks of a specific type
List<IMyTerminalBlock> terminals = new List<IMyTerminalBlock>();
grid.GetBlocksOfType<IMyShipConnector>(terminals);
```

---

## 10. IMyGridTerminalSystem — Terminal Network

Access to all blocks connected via connectors, rotors, pistons:

```csharp
IMyGridTerminalSystem terminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

// All blocks in the terminal network
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
terminalSys.GetBlocks(blocks);

// Blocks by type
terminalSys.GetBlocksOfType<IMyShipConnector>(blocks, filter);

// Search by name
terminalSys.SearchBlocksOfName("Connector", blocks);
```

---

## 11. Spawning from a Local Blueprint (working algorithm)

```csharp
// 1. Load bp.sbc via DeserializeXML (Private namespace!)
MyObjectBuilder_Definitions definitions = null;
using (var stream = File.OpenRead(bpFilePath))
{
    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
        stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
    definitions = obj as MyObjectBuilder_Definitions;
}

// 2. Extract ShipBlueprint
var shipBp = definitions.ShipBlueprints[0];

// 3. Extract grids
var grids = new List<MyObjectBuilder_CubeGrid>();
foreach (var grid in shipBp.CubeGrids)
{
    if (grid is MyObjectBuilder_CubeGrid cubeGrid)
        grids.Add(cubeGrid);
}

// 4. Remap EntityId (public IMyEntities)
MyAPIGateway.Entities.RemapObjectBuilderCollection(grids);

// 5. Set spawn position
MatrixD spawnMatrix = MatrixD.CreateWorld(spawnPos, Vector3D.Forward, Vector3D.Up);
gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(spawnMatrix);

// 6. Create and add to world (public IMyEntities)
// ⚠️ This produces a phantom grid — visuals are there, physics/initialization is not
// See: Phantom Grid.md for details
IMyEntity entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder);
```

---

## 12. Launching the Game with a World Load

```batch
SpaceEngineersLauncher.exe -world "World Name"
```

The world name must match the folder name in:
`%APPDATA%\SpaceEngineers\Saves\<steamid>\<WorldName>\`

Example: `-world "Empty World 2026-07-14 21-40"`

---
