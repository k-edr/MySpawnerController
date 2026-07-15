# GridSpawner.Plugin

Game plugin for Space Engineers — spawns grids from local blueprints via REST API.
Loaded by [PluginLoader](https://github.com/sepluginloader/PluginLoader) v1.12.8.

## Architecture (Clean Architecture)

```
GridSpawner.Plugin/
├── Application/
│   ├── SpawnService.cs              ← use case orchestrator (ISpawnService impl)
│   ├── SpawnTask.cs / DeleteTask.cs ← task DTOs for main-thread queue
│   ├── BlueprintDeserializer.cs     ← .sbc XML → object builders
│   ├── PostSpawnFixup.cs            ← reflection-based grid activation
│   ├── GridDtoMapper.cs             ← MyCubeGrid entity → GridDto
│   └── IBlockInfoExtractor.cs       ← strategy: fat vs slim block info
│       FatBlockInfoExtractor.cs
│       SlimBlockInfoExtractor.cs
├── Infrastructure/
│   ├── Plugin.cs                    ← IPlugin entry point
│   ├── SessionComponent.cs          ← session lifecycle, starts ApiServer
│   └── Logger.cs                    ← file + game log
└── Properties/
```

## Dependency direction

```
Infrastructure → Application → GridSpawner.Api + GridSpawner.Shared
```

## Build

Part of `GridSpawner.sln`. Built by `build.ps1`:

```powershell
.\build.ps1   # builds all 4 projects, copies to Bin64/Plugins/
```

- **Framework:** .NET Framework 4.8 x64
- **Output:** `GridSpawner.Plugin.dll` → `Bin64/Plugins/`
- **Dependencies:** `GridSpawner.Api.dll`, `GridSpawner.Shared.dll` (auto-copied)

## API ports

| Port | What |
|------|------|
| 9997 | Game JSON API |
| 9998 | Swagger UI (separate .exe) |

See [Swagger README](../GridSpawner.Swagger/README.md) for UI setup.
