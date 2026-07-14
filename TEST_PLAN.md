# MySpawnerController - Pipeline & Expected Results

## 1. Plugin Load (PluginLoader Stage)

| Step | API Call | Expected |
|------|----------|----------|
| 1.1 | PluginLoader finds `MySpawnerController.dll` in `config.xml` | DLL loaded, `IPlugin` type found |
| 1.2 | `new Plugin()` | Instance created, no errors |
| 1.3 | `plugin.Init(gameInstance)` | `Logger.Info("Plugin.Init called")` in log |
| 1.4 | `[MySessionComponentDescriptor]` scan | `SessionComponent` registered in `MySession` |
| **LOG** | | `%APPDATA%\SpaceEngineers\MySpawnerController.log` created |

---

## 2. Session Load (World Enter Stage)

| Step | Condition | Expected |
|------|-----------|----------|
| 2.1 | `UpdateAfterSimulation()` tick < 60 | Skip, wait for session init |
| 2.2 | `UpdateAfterSimulation()` tick >= 60, `MySession.Static.Ready == true` | `_spawned = true`, enter `SpawnAllGrids()` |
| 2.3 | `MySession.Static.Ready == false` after 60 ticks | Wait next frame |
| **LOG** | | `"Session ready. Starting grid spawn sequence..."` |

---

## 3. Blueprint Spawn Per Grid

### 3.1 TestGrid_MultiConnectorGrid @ (0, 0, 0)

| Step | API Call | Args | Expected Result |
|------|----------|------|----------------|
| 3.1.1 | `File.Exists(bpFile)` | `%APPDATA%/SpaceEngineers/Blueprints/local/TestGrid_MultiConnectorGrid/bp.sbc` | `true` |
| 3.1.2 | `MyObjectBuilderSerializer.DeserializePB<MyObjectBuilder_Definitions>(bpFile, out definitions)` | bpFile | `definitions != null` |
| 3.1.3 | `definitions.ShipBlueprints.Length > 0` | — | `true` |
| 3.1.4 | `definitions.ShipBlueprints[0]` → shipBp | — | `shipBp.CubeGrids.Length >= 1` |
| 3.1.5 | `foreach (grid in shipBp.CubeGrids)` → gridBuilders | — | List of `MyObjectBuilder_CubeGrid` |
| 3.1.6 | `MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders)` | gridBuilders | New EntityId assigned to each grid |
| 3.1.7 | `gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(spawnMatrix)` | Matrix at (0,0,0) | Position set |
| 3.1.8 | `MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder)` | gridBuilder | Returns `IMyEntity`, cast to `MyCubeGrid` |
| **LOG** | | | `"  OK: <DisplayName> -> {0, 0, 0}, Id=<EntityId>"` |

### 3.2 TestGrid_PBWithPanel @ (0, 0, 10)

| Step | Same as 3.1.x except: |
|------|----------------------|
| 3.2.1 | bpFile → `.../TestGrid_PBWithPanel/bp.sbc` |
| 3.2.7 | Position = originalPos + `(0, 0, 10)` |
| **LOG** | | `"  OK: <DisplayName> -> {0, 0, 10}, Id=<EntityId>"` |

### 3.3 TestGrid_SingleConnector @ (0, 0, -10)

| Step | Same as 3.1.x except: |
|------|----------------------|
| 3.3.1 | bpFile → `.../TestGrid_SingleConnector/bp.sbc` |
| 3.3.7 | Position = originalPos + `(0, 0, -10)` |
| **LOG** | | `"  OK: <DisplayName> -> {0, 0, -10}, Id=<EntityId>"` |

---

## 4. Expected Log Output (Success)

```
[HH:MM:SS] [INFO] Plugin.Init called
[HH:MM:SS] [INFO] Game instance type: SpaceEngineers.Game.SpaceEngineersGame
[HH:MM:SS] [INFO] Session ready. Starting grid spawn sequence...
[HH:MM:SS] [INFO] --- Spawning: TestGrid_MultiConnectorGrid ---
[HH:MM:SS] [INFO]   Blueprint: TestGrid_MultiConnectorGrid, Grids: 1
[HH:MM:SS] [INFO]   OK: <grid_name> -> {X:0 Y:0 Z:0}, Id=<long>
[HH:MM:SS] [INFO] --- Spawning: TestGrid_PBWithPanel ---
[HH:MM:SS] [INFO]   Blueprint: TestGrid_PBWithPanel, Grids: 1
[HH:MM:SS] [INFO]   OK: <grid_name> -> {X:0 Y:0 Z:10}, Id=<long>
[HH:MM:SS] [INFO] --- Spawning: TestGrid_SingleConnector ---
[HH:MM:SS] [INFO]   Blueprint: TestGrid_SingleConnector, Grids: 1
[HH:MM:SS] [INFO]   OK: <grid_name> -> {X:0 Y:0 Z:-10}, Id=<long>
[HH:MM:SS] [INFO] Spawn complete: 3/3 grids spawned.
```

---

## 5. Expected Log Output (Blueprint Missing)

```
[HH:MM:SS] [INFO] Session ready. Starting grid spawn sequence...
[HH:MM:SS] [INFO] --- Spawning: TestGrid_MultiConnectorGrid ---
[HH:MM:SS] [WARN] Blueprint file not found: C:\Users\...\TestGrid_MultiConnectorGrid\bp.sbc
[HH:MM:SS] [INFO] --- Spawning: TestGrid_PBWithPanel ---
[HH:MM:SS] [INFO]   Blueprint: TestGrid_PBWithPanel, Grids: 1
[HH:MM:SS] [INFO]   OK: ... -> {X:0 Y:0 Z:10}, Id=...
[HH:MM:SS] [INFO] Spawn complete: 1/3 grids spawned.
```

---

## 6. Expected Log Output (Deserialization Failure)

```
[HH:MM:SS] [INFO] --- Spawning: TestGrid_MultiConnectorGrid ---
[HH:MM:SS] [ERROR] Deserialization returned null for: C:\Users\...\bp.sbc
```

---

## 7. Expected Log Output (No CubeGrids)

```
[HH:MM:SS] [INFO] --- Spawning: TestGrid_MultiConnectorGrid ---
[HH:MM:SS] [ERROR] No CubeGrids in blueprint: TestGrid_MultiConnectorGrid
```

---

## 8. World Command-Line Launch

| Command | Expected |
|---------|----------|
| `SpaceEngineersLauncher.exe -world "Empty World 2026-07-14 21-40"` | Game starts, loads the world directly, skips main menu |
| No `-world` arg | Normal launch with main menu |

---

## 9. Config Validation

| File | Check | Expected |
|------|-------|----------|
| `Bin64\Plugins\config.xml` | `<GameVersion>1209024</GameVersion>` | Matches current game version |
| `Bin64\Plugins\config.xml` | Contains `<Id>...MySpawnerController.dll</Id>` | Plugin registered |
| `Bin64\Plugins\MySpawnerController.dll` | File exists after deploy | DLL deployed |

---

## 10. Debug Checklist

- [ ] `build.ps1` completes without errors (BUILD OK)
- [ ] `MySpawnerController.dll` present in `Bin64\Plugins\`
- [ ] `config.xml` contains plugin `<Id>` entry
- [ ] Game launches via `run_world.bat`
- [ ] After world loads (~2 sec): 3 grids visible near origin
- [ ] `MySpawnerController.log` contains `"Spawn complete: 3/3"`
- [ ] Grid positions: `TestGrid_MultiConnectorGrid` at (0, 0, 0), others ±10m on Z
