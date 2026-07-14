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
| 2.1 | `Init(mySessionComponent)` → `StartHttpListener()` | HTTP server starts on `localhost:9998` |
| 2.2 | Browser/curl → `GET /spawn` | Queue spawn items, process in `UpdateAfterSimulation` |
| 2.3 | `MySession.Static.Ready == false` | `GET /spawn` returns `503 Session not ready` |
| **LOG** | | `"Listening on http://localhost:9998/"` |

---

## 3. Blueprint Spawn Per Grid

### 3.1 TestGrid_MultiConnectorGrid @ (0, 0, 0)

| Step | API Call | Args | Expected Result |
|------|----------|------|----------------|
| 3.1.1 | `File.Exists(bpFile)` | `%APPDATA%/SpaceEngineers/Blueprints/local/TestGrid_MultiConnectorGrid/bp.sbc` | `true` |
| 3.1.2 | `MyObjectBuilderSerializerKeen.DeserializeXML(stream, out obj, typeof(MyObjectBuilder_Definitions))` | Stream of bpFile | `definitions != null` |
| 3.1.3 | `definitions.ShipBlueprints.Length > 0` | — | `true` |
| 3.1.4 | `definitions.ShipBlueprints[0]` → shipBp | — | `shipBp.CubeGrids.Length >= 1` |
| 3.1.5 | `foreach (grid in shipBp.CubeGrids)` → gridBuilders | — | List of `MyObjectBuilder_CubeGrid` |
| 3.1.6 | `MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders)` | gridBuilders | New EntityId assigned to each grid |
| 3.1.7 | `gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(spawnMatrix)` | Matrix at (0,0,0) | Position set |
| 3.1.8 | `MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridBuilder)` | gridBuilder | Returns `IMyEntity`, cast to `MyCubeGrid` (phantom!) |
| **LOG** | | | `"  OK: <DisplayName> Id=<EntityId>"` |

### 3.2 TestGrid_PBWithPanel @ (0, 0, 10)

| Step | Same as 3.1.x except: |
|------|----------------------|
| 3.2.1 | bpFile → `.../TestGrid_PBWithPanel/bp.sbc` |
| 3.2.7 | Position = originalPos + `(0, 0, 10)` |
| **LOG** | | `"  OK: <DisplayName> Id=<EntityId>"` |

### 3.3 TestGrid_SingleConnector @ (0, 0, -10)

| Step | Same as 3.1.x except: |
|------|----------------------|
| 3.3.1 | bpFile → `.../TestGrid_SingleConnector/bp.sbc` |
| 3.3.7 | Position = originalPos + `(0, 0, -10)` |
| **LOG** | | `"  OK: <DisplayName> Id=<EntityId>"` |

---

## 4. Expected Log Output (Success)

```
[HH:MM:SS] [INFO] Plugin loaded. Send GET http://localhost:9998/spawn after world loads.
[HH:MM:SS] [INFO] Listening on http://localhost:9998/
[HH:MM:SS] [INFO] HTTP GET /spawn
[HH:MM:SS] [INFO] Spawn: TestGrid_MultiConnectorGrid @ {X:0 Y:0 Z:0}
[HH:MM:SS] [INFO]   OK: TestGrid_MultiConnectorGrid Id=131055962750989521
[HH:MM:SS] [INFO]   OK: Small Grid 2481 Id=110475759332787369
[HH:MM:SS] [INFO] Spawn: TestGrid_PBWithPanel @ {X:0 Y:0 Z:10}
[HH:MM:SS] [INFO]   OK: TestGrid_PBWithPanel Id=98424645662834437
[HH:MM:SS] [INFO] Spawn: TestGrid_SingleConnector @ {X:0 Y:0 Z:-10}
[HH:MM:SS] [INFO]   OK: TestGrid_SingleConnector Id=116730841757601590
```

---

## 5. Expected Log Output (Blueprint Missing)

```
[HH:MM:SS] [INFO] HTTP GET /spawn
[HH:MM:SS] [INFO] Spawn: TestGrid_MultiConnectorGrid @ {X:0 Y:0 Z:0}
[HH:MM:SS] [ERROR] No ShipBlueprints: TestGrid_MultiConnectorGrid
[HH:MM:SS] [INFO] Spawn: TestGrid_PBWithPanel @ {X:0 Y:0 Z:10}
[HH:MM:SS] [INFO]   OK: TestGrid_PBWithPanel Id=...
[HH:MM:SS] [INFO] Spawn: TestGrid_SingleConnector @ {X:0 Y:0 Z:-10}
[HH:MM:SS] [INFO]   OK: TestGrid_SingleConnector Id=...
```

---

## 6. Expected Log Output (Deserialization Failure)

```
[HH:MM:SS] [INFO] Spawn: TestGrid_MultiConnectorGrid @ {X:0 Y:0 Z:0}
[HH:MM:SS] [ERROR] DeserializeXML: TestGrid_MultiConnectorGrid: System.Exception...
```

---

## 7. Expected Log Output (No CubeGrids)

```
[HH:MM:SS] [INFO] Spawn: TestGrid_MultiConnectorGrid @ {X:0 Y:0 Z:0}
[HH:MM:SS] [ERROR] No CubeGrids: TestGrid_MultiConnectorGrid
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
- [ ] After world loads: `Invoke-WebRequest http://localhost:9998/spawn` returns 200
- [ ] `MySpawnerController.log` contains `"OK: TestGrid_..."` for all 3 grids
- [ ] Grid positions: `TestGrid_MultiConnectorGrid` at (0, 0, 0), others ±10m on Z
- [ ] Grids visible in world (phantom — no physics/collision, see Фантомный грид.md)
