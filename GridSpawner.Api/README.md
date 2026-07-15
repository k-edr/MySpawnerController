# GridSpawner.Api

HTTP API layer for the [GridSpawner.Plugin](../GridSpawner.Plugin/README.md).
Routes requests, validates input, delegates to the game plugin.

## Architecture (Clean Architecture)

```
GridSpawner.Api/                      (netstandard2.0)
├── Application/
│   ├── ISpawnService.cs              ← service contract (spawn, list, delete)
│   ├── SpawnOrchestrator.cs          ← blueprint resolution + validation
│   └── SpawnResult.cs                ← typed result (status + body)
├── Infrastructure/
│   ├── ApiServer.cs                  ← HttpListener routing
│   ├── AppConfigLoader.cs            ← %APPDATA% config load/save
│   └── HttpResponseHelper.cs         ← JSON/Text/CORS response helpers
└── GridSpawner.Api.csproj
```

## Dependency direction

```
Infrastructure → Application → GridSpawner.Shared
```

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

## Config

`%APPDATA%\SpaceEngineers\GridSpawner.json`:

```json
{
  "apiPort": 9997,
  "swaggerPort": 9998,
  "swaggerCorsOrigin": "http://localhost:9998",
  "blueprintsFolder": null,
  "apiKey": null,
  "maxBlueprintFileSizeBytes": 52428800,
  "maxGridsPerBlueprint": 50
}
```

## Build

```powershell
dotnet build GridSpawner.Api -c Release
```

References game DLLs from `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\`.
