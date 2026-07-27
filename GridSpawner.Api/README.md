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
│   ├── HttpResponseHelper.cs         ← JSON/Text/CORS response helpers
│   ├── BlueprintHandler.cs           ← GET /api/v1/blueprints
│   ├── GridHandler.cs                ← GET/DELETE /api/v1/grids
│   ├── BlockHandler.cs               ← Terminal block interaction
│   ├── PbTestHandler.cs              ← PB/LCD convenience endpoints
│   ├── SpawnHandler.cs               ← POST /api/v1/spawn, /spawn-tests
│   ├── HealthHandler.cs              ← GET /api/v1/health
│   ├── SwaggerHandler.cs             ← Swagger UI on separate port
│   └── Router.cs                     ← Regex-based routing
└── GridSpawner.Api.csproj
```

## Dependency direction

```
Infrastructure → Application → GridSpawner.Shared
```

---

## API Endpoints (port 9997)

### Health

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `GET` | `/api/v1/health` | Session readiness | `{"ready": bool}` |

### Blueprints

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `GET` | `/api/v1/blueprints` | List local blueprints | `[{ "name", "available" }]` |

### Spawn

| Method | Path | Description | Body |
|--------|------|-------------|------|
| `POST` | `/api/v1/spawn` | Spawn a single grid | `{"blueprint": "name", "position?": {"x","y","z"}, "displayName?": "name"}` |
| `POST` | `/api/v1/spawn-tests` | Spawn all 3 test grids | — |

### Grid management

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `GET` | `/api/v1/grids` | List tracked grids | `[{ "id", "name", "position", "velocity", "forward", ... }]` |
| `GET` | `/api/v1/grids/{id}` | Grid detail + blocks + orientation | `GridDto` (see below) |
| `DELETE` | `/api/v1/grids/{id}` | Remove a single grid | `{"deleted": id}` or `{"error": "..."}` |
| `DELETE` | `/api/v1/grids` | Remove all grids | `{"deleted": count}` |

### Grid orientation fields (GridDto)

`GET /api/v1/grids/{id}` now returns full 6-axis orientation from `grid.WorldMatrix`:

```json
{
  "id": 123,
  "name": "Missile",
  "position":  { "x": 100, "y": 200, "z": 300 },
  "velocity":  { "x": 50, "y": 0, "z": -10 },
  "forward":   { "x": 0.7, "y": 0.2, "z": 0.7 },
  "backward":  { "x": -0.7, "y": -0.2, "z": -0.7 },
  "up":        { "x": 0, "y": 1, "z": 0 },
  "down":      { "x": 0, "y": -1, "z": 0 },
  "left":      { "x": -0.7, "y": 0, "z": 0.7 },
  "right":     { "x": 0.7, "y": 0, "z": -0.7 },
  "blocks": [ ... ]
}
```

- `forward` / `backward` — from `WorldMatrix.Forward`
- `up` / `down` — from `WorldMatrix.Up`
- `right` / `left` — from `WorldMatrix.Right`

### Terminal block interaction (by position)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/grids/{id}/blocks` | List all functional blocks |
| `GET` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}` | Block detail (actions + properties) |
| `GET` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}` | Single property value |
| `PUT` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}` | Set property value |
| `POST` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/action` | Execute terminal action |
| `PUT` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/program` | Upload code to PB |
| `POST` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/run` | Run PB with argument |
| `PUT` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/text` | Write text to panel/LCD |
| `GET` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/surface-text` | Read PB built-in LCD surface |

### PB/LCD convenience (by grid — finds first PB)

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `PUT` | `/api/v1/grids/{id}/script` | Upload code to first PB | `{"success": true, "length": N}` |
| `POST` | `/api/v1/grids/{id}/run` | Run first PB, return echo | `{"echo": "...", "output": "...", "success": true}` |
| `POST` | `/api/v1/grids/{id}/runlcd` | Run PB, wait 100 ms, read LCD surface | `{"echo": "...", "output": "LCD text", "success": true}` |
| `GET` | `/api/v1/grids/{id}/lcd` | Read first LCD/PB surface text | `{"content": "..."}` |

`ScriptRunResult` fields:
- `echo` — text from PB's Echo property (reflection-based)
- `output` — PB echo (for `/run`) or LCD surface text (for `/runlcd`)
- `success` — whether `TryRun()` succeeded

**`/runlcd` vs `/run`:** `/runlcd` runs the script, waits 100 ms (`Thread.Sleep` on game thread) for the LCD to update, then reads the PB's built-in display surface (`GetSurface(0).GetText()`). This is often more convenient than Echo, as PB scripts commonly write formatted output to `Me.GetSurface(0)`.

---

## Config

`%APPDATA%\SpaceEngineers\GridSpawner.json`:

```json
{
  "apiScheme": "http",
  "apiHost": "localhost",
  "apiPort": 9997,
  "swaggerPort": 9998,
  "swaggerCorsOrigin": "http://localhost:9998",
  "blueprintsFolder": "%APPDATA%\\SpaceEngineers\\Blueprints\\local",
  "maxBlueprintFileSizeBytes": 52428800,
  "maxGridsPerBlueprint": 50
}
```

## Build

```powershell
dotnet build GridSpawner.Api -c Release
```

References game DLLs from `D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\`.
