# MySpawnerController — Project State

## What is this

Space Engineers plugin that spawns grids from local blueprints via REST API.
Loaded via [PluginLoader](https://github.com/sepluginloader/PluginLoader) v1.12.8.
Game version: `1.209.024` (Bin64 at `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64`).

## Architecture (4 projects, Clean Architecture)

```
MySpawnerController.sln
├── MySpawnerController.Shared/        (netstandard2.0) — Pure DTOs, no game deps
│   └── 11 files: HealthResponse, ErrorResponse, SpawnRequest, SpawnPosition,
│       SpawnResponse, GridDto, BlockDto, Vector3Dto, Vector3IDto,
│       BlueprintInfo, GridListItem
├── MySpawnerController.Api/           (netstandard2.0) — HTTP API layer
│   ├── Application/
│   │   ├── ISpawnService.cs           — spawn contract (uses VRageMath)
│   │   └── SpawnOrchestrator.cs       — blueprint resolution, validation
│   └── Infrastructure/
│       ├── ApiServer.cs               — HttpListener routing
│       └── HttpResponseHelper.cs      — Json/Text/Respond helpers
├── MySpawnerController/               (.NET Framework 4.8, x64) — PluginLoader target
│   ├── Plugin.cs, Logger.cs
│   ├── SessionComponent.cs — creates ApiServer + SpawnService
│   └── SpawnService.cs — spawn/delete/track grids, main-thread queue
└── MySpawnerController.Swagger/       (.NET 8.0) — Swashbuckle Swagger UI
    ├── Application/
    │   ├── OpenApiDocumentBuilder.cs   — builds spec from Shared DTOs
    │   └── GameApiSwaggerProvider.cs   — ISwaggerProvider impl
    ├── Infrastructure/
    │   └── SwaggerHostExtensions.cs    — UseSwagger/UseSwaggerUI + connection JS
    └── Program.cs                      — composition root (~25 lines)
```

### Dependency direction
```
MySpawnerController.Shared  ← zero deps (pure DTOs)
    ↑
MySpawnerController.Api     ← Shared + game DLLs (ISpawnService, ApiServer)
    ↑              ↑
MySpawnerController        MySpawnerController.Swagger  ← Shared + Swashbuckle
```

### Data flow
```
Browser (9998/swagger) → fetch /api/v1/openapi.json → renders Swagger UI
Swagger "Try it out" → POST localhost:9997/api/v1/spawn → ApiServer → SpawnService
```

### Ports
| Port | Process | What |
|------|---------|------|
| 9997 | Game plugin | JSON API (health, blueprints, grids, spawn, spawn-tests) |
| 9998 | Separate .exe | Swagger UI |

## Key files

| File | Purpose |
|------|---------|
| `build.ps1` | Builds all 3 projects + copies to `Bin64/Plugins/` + updates config.xml |
| `build_and_run.ps1` | build.ps1 → launch game → launch swagger → wait → POST spawn-tests |
| `run_world.bat` | `SpaceEngineersLauncher.exe -world "Empty_World_In"` |
| `run_swagger.bat` | Starts `MySpawnerController.Swagger.exe` |
| `API_DOCS.md` | Reverse-engineered SE API docs from game DLLs |
| `Фантомный грид.md` | Phantom grid phenomenon + fix (CreatePhysics/Editable/PostSpawnFixup) |
| `README.md` | Usage guide |
| `TEST_PLAN.md` | Test plan |

## Critical details

### Spawn flow
1. `DeserializeXML` (NOT `DeserializePB` — returns null for `.sbc`) via `MyObjectBuilderSerializerKeen`
2. Extract `ShipBlueprints[0].CubeGrids`
3. `RemapObjectBuilderCollection` (block ID remapping for world uniqueness)
4. Set `CreatePhysics=true`, `Editable=true`, `DestructibleBlocks=true`
5. `CreateFromObjectBuilderAndAdd`
6. PostSpawnFixup: `OnAddedToScene` + `ActivatePhysics` + `RegisterCubeGrid` (all via reflection, all `internal`)

### Blueprint paths
- Source: `%APPDATA%\SpaceEngineers\Blueprints\local\<name>\bp.sbc`
- Target world: `Empty_World_In`
- Test grids: `TestGrid_MultiConnectorGrid`, `TestGrid_PBWithPanel`, `TestGrid_SingleConnector`

### build.ps1 quirks
- Must kill `SpaceEngineers.exe` + `SpaceEngineersLauncher.exe` before copy (file lock)
- Retry logic (5 attempts, 2s delay) for DLL copy
- Registers only `MySpawnerController.dll` in config.xml (other DLLs are deps, not plugins)
- Copies NuGet deps (System.Text.Json, System.Memory, etc.) to Plugins folder

### CORS
Game API adds `Access-Control-Allow-Origin: http://localhost:9998` for cross-port Swagger.

### Logging
`%APPDATA%\SpaceEngineers\MySpawnerController.log` — file logger with timestamps.

## API endpoints (port 9997)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/health` | `{"ready": bool}` |
| GET | `/api/v1/blueprints` | List blueprints in `%APPDATA%` |
| GET | `/api/v1/grids` | List tracked spawned grids |
| GET | `/api/v1/grids/{id}` | Grid details with blocks |
| DELETE | `/api/v1/grids/{id}` | Remove grid |
| POST | `/api/v1/spawn` | Spawn with `{"blueprint","position?","displayName?"}` |
| POST | `/api/v1/spawn-tests` | Spawn all 3 test grids |

## Quick test commands
```powershell
# Health
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/health | % Content

# Spawn tests
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn-tests | % Content

# Spawn single with custom position
Invoke-WebRequest -UseBasicParsing -Method POST -Body '{"blueprint":"TestGrid_SingleConnector","position":{"x":50,"z":100},"displayName":"MyGrid"}' -ContentType "application/json" http://localhost:9997/api/v1/spawn | % Content

# List spawned grids
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/grids | % Content

# Log tail
Get-Content "$env:APPDATA\SpaceEngineers\MySpawnerController.log" -Tail 30
```

## Git
- Branch: `feature/api`
- Last commit: `11c45e8`
- All 4 projects build successfully

---

# ✅ DONE: Clean Architecture + Shared project (4 projects)

**What changed:** `MySpawnerController.Swagger/` now uses **Swashbuckle.AspNetCore** (ASP.NET Core minimal API) with Clean Architecture.

## Project structure (Clean Architecture)
```
MySpawnerController.Swagger/
├── Domain/                              ← Entities (no deps)
│   ├── HealthResponseDto.cs
│   ├── ErrorResponseDto.cs
│   ├── SpawnRequestDto.cs
│   ├── SpawnPositionDto.cs
│   ├── SpawnResponseDto.cs
│   ├── GridDtoShape.cs
│   ├── BlockDtoShape.cs
│   ├── Vector3DtoShape.cs
│   ├── Vector3IDtoShape.cs
│   ├── BlueprintInfoDto.cs
│   └── GridListItemDto.cs
├── Application/                         ← Use cases (Domain + OpenApi)
│   ├── OpenApiDocumentBuilder.cs        ← builds OpenApiDocument from Domain DTOs
│   └── GameApiSwaggerProvider.cs        ← ISwaggerProvider impl
├── Infrastructure/                      ← Hosting (Application + ASP.NET Core)
│   └── SwaggerHostExtensions.cs         ← UseSwagger/UseSwaggerUI + connection JS
├── Program.cs                           ← Composition root (~25 lines)
└── MySpawnerController.Swagger.csproj
```

**Dependency direction:** `Program → Infrastructure → Application → Domain`

| File | Change |
|------|--------|
| `.csproj` | `Microsoft.NET.Sdk.Web`, Swashbuckle 6.9.0, ImplicitUsings |
| `Program.cs` | Thin: build doc, compose host, run (~25 lines) |
| `Domain/*.cs` | **11 files** — DTO shapes for schema generation (1 class = 1 file) |
| `Application/*.cs` | **2 files** — doc builder + swagger provider |
| `Infrastructure/*.cs` | **1 file** — host config + connection JS |

**How it works:**
- Schemas auto-generated from Domain DTOs via `SchemaGenerator`
- Paths built with typed `OpenApiPathItem`/`OpenApiOperation` (no JSON strings)
- Connection status dot via `InjectJavascript`
- OpenAPI `servers[0].url` = `http://localhost:9997` (game API)
- `build.ps1`, `build_and_run.ps1`, `run_swagger.bat` — no changes needed

**Adding a new endpoint:**
1. Add DTO shape in `Domain/`
2. Add path in `Application/OpenApiDocumentBuilder.BuildPaths()`
3. Spec auto-updates
