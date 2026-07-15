# GridSpawner — HTTP API for Spawning Grids in Space Engineers

A Space Engineers plugin that starts a JSON API on `localhost:9997` for spawning,
listing, and deleting grids from local blueprints. Swagger UI runs as a
**separate process** on `localhost:9998`.

## Requirements & Dependencies

Before setting up the project, make sure you have:
1. **Windows OS**: The build and run scripts (`build.ps1`, `run.ps1`, `run.bat`) are adapted for Windows. On Linux/Proton, they may require modifications.
2. **Space Engineers**: Installed on your machine.
   * *Default Steam Path (example):* `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64`
3. **Space Engineers Plugin Loader**: Recommended. Having it installed and run at least once generates `Bin64/Plugins/config.xml` which allows the build script to automatically register the plugin. If missing, the build script will issue a warning and skip registration.
4. **MSBuild**: Installed via Visual Studio (2019 or 2022) or Build Tools.

## Quick Start (one command)

```
.\run.bat
```

This does:
1. Builds all 4 projects
2. Copies DLLs to `Bin64/Plugins/`
3. Launches the game (AutoWorldLoader loads the configured world)
4. Launches Swagger UI in a separate window
5. Waits for world load
6. Spawns 3 test grids

---

## Path Configuration

Before the first build, copy the template and set your `Bin64` path:

```powershell
copy build-config.example.json build-config.json
# edit build-config.json
```

```json
{
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64"
}
```

| Field | Description | Example (Linux/Proton) |
|-------|-------------|------------------------|
| `seBin64` | Path to the game's `Bin64` directory | `"Z:/home/user/.steam/.../Bin64"` |

**All paths that may need changing per environment:**

| File | Variable | What |
|------|----------|------|
| `build-config.json` | `seBin64` | Game `Bin64` directory |
| `build.ps1` | (reads from `build-config.json`) | Build + deploy script |
| `run.ps1` | (reads from `build-config.json`) | Build + run + test script |
| `%APPDATA%\SpaceEngineers\GridSpawner.json` | `blueprintsFolder` | Override blueprints directory |
| `%APPDATA%\SpaceEngineers\GridSpawner.json` | `apiPort` / `swaggerPort` | Port overrides |

> `build-config.json` is gitignored. Each developer keeps their own local copy.
> `build-config.example.json` is the committed template.

---

## Runtime Config (`%APPDATA%\SpaceEngineers\GridSpawner.json`)

Created automatically on first plugin start:

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

| Field | Description |
|-------|-------------|
| `apiScheme` | `http` or `https` |
| `apiHost` | Listener host (`localhost` for local, `+` for all interfaces — requires admin) |
| `apiPort` | JSON API port |
| `swaggerPort` | Swagger UI port |
| `swaggerCorsOrigin` | CORS origin for browser requests |
| `blueprintsFolder` | Override blueprints folder |
| `maxBlueprintFileSizeBytes` | Max `.sbc` file size (default 50 MB) |
| `maxGridsPerBlueprint` | Max grids allowed in a single blueprint |

---

## Build Only

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

---

## Ports

| Port | What | Who |
|------|------|-----|
| `9997` | JSON API (spawn, grids, health) | In-game plugin |
| `9998` | Swagger UI | Separate process |

---

## API Endpoints (port 9997)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/health` | `{"ready": true/false}` |
| `GET` | `/api/v1/blueprints` | List blueprints |
| `GET` | `/api/v1/grids` | List spawned grids |
| `GET` | `/api/v1/grids/{id}` | Grid details |
| `DELETE` | `/api/v1/grids/{id}` | Delete grid |
| `POST` | `/api/v1/spawn` | Spawn grid `{"blueprint":"...", "position":{"x":0,"y":0,"z":0}}` |
| `POST` | `/api/v1/spawn-tests` | Spawn 3 test grids |

### Terminal Block Interaction

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/grids/{id}/blocks` | List all functional blocks on a grid |
| `GET` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}` | Get block details (actions + properties) |
| `POST` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/action` | Execute a block action `{"actionId":"OnOff_Off"}` |
| `GET` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}` | Get property value |
| `PUT` | `/api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}` | Set property `{"value":"true"}` |

---

## PowerShell Examples

```powershell
# Spawn all test grids
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn-tests

# Spawn with custom position
$body = '{"blueprint":"TestGrid_SingleConnector","displayName":"MyGrid","position":{"x":50,"y":0,"z":100}}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn -Body $body -ContentType "application/json"

# Health check
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/health

# List terminal blocks on a grid
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/grids/12345/blocks

# Get block details (actions + properties)
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/grids/12345/blocks/0/0/0

# Execute block action (e.g. toggle power)
$actionBody = '{"actionId":"OnOff_Off"}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/grids/12345/blocks/0/0/0/action -Body $actionBody -ContentType "application/json"

# Get property
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/grids/12345/blocks/0/0/0/properties/OnOff

# Set property
$propBody = '{"value":"true"}'
Invoke-WebRequest -UseBasicParsing -Method PUT http://localhost:9997/api/v1/grids/12345/blocks/0/0/0/properties/OnOff -Body $propBody -ContentType "application/json"
```

---

## Architecture

```
GridSpawner.Shared/    — netstandard2.0: Configuration/ + Models/ (DTOs)
GridSpawner.Api/       — netstandard2.0: Application/ + Infrastructure/
GridSpawner.Plugin/    — .NET 4.8:      Application/ + Infrastructure/
GridSpawner.Swagger/   — .NET 8.0:      Application/ + Infrastructure/ (Swashbuckle)
```

Clean architecture with clear dependency direction: `Infrastructure → Application → Shared`.

---

## Logs

`%APPDATA%\SpaceEngineers\GridSpawner.log`
