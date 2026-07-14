# MySpawnerController — Test Plan

## 1. Build & Deploy

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

| Check | Expected |
|-------|----------|
| API Build OK | ✅ |
| Main Build OK | ✅ |
| DLLs copied to Plugins/ | `MySpawnerController.dll`, `MySpawnerController.Api.dll`, `System.Text.Json.dll` |
| config.xml updated | Contains `<Id>...MySpawnerController.dll</Id>` |

## 2. Game Launch

```batch
run_world.bat
```

| Check | Expected |
|-------|----------|
| Game starts, loads world directly | ✅ No main menu |
| `MySpawnerController.log` created | `"Plugin loaded. Swagger UI at http://localhost:9998/swagger"` |

## 3. API Tests

### 3.1 Health Check

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/health
```

| Expected | Status |
|----------|--------|
| `200` `{"ready":true}` | After world loads ✅ |
| `200` `{"ready":false}` | Before world loads |

### 3.2 List Blueprints

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/blueprints
```

| Expected | Status |
|----------|--------|
| `200` JSON array with `["name","available"]` | ✅ |

### 3.3 Spawn Tests (all 3 grids)

```powershell
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn-tests
```

| Expected | Status |
|----------|--------|
| `200` JSON `{"grids":[...]}` with 4+ grids | ✅ |
| Grids at (0,0,0), (0,0,10), (0,0,-10) | ✅ |
| Each grid has `id`, `name`, `position`, `velocity`, `blocks[]` | ✅ |
| Blocks have `name`, `type`, `gridPosition` | ✅ |

### 3.4 Spawn Single Grid (custom pos + name)

```powershell
$body = '{"blueprint":"TestGrid_SingleConnector","displayName":"MyCustomGrid","position":{"x":100,"y":0,"z":200}}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn -Body $body -ContentType "application/json"
```

| Expected | Status |
|----------|--------|
| `200` JSON with 1 grid | ✅ |
| Grid name = "MyCustomGrid" | ✅ |
| Position = (100, 0, 200) | ✅ |

### 3.5 List Grids

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/grids
```

| Expected | Status |
|----------|--------|
| `200` JSON array with all spawned grids | ✅ |

### 3.6 Get Grid by ID

```powershell
# Use ID from response
Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/grids/123456789
```

| Expected | Status |
|----------|--------|
| `200` Grid details | Grid exists ✅ |
| `404` `{"error":"Grid ... not found"}` | Grid doesn't exist |

### 3.7 Delete Grid

```powershell
Invoke-WebRequest -UseBasicParsing -Method DELETE http://localhost:9998/api/v1/grids/123456789
```

| Expected | Status |
|----------|--------|
| `200` `{"deleted":123456789}` | Grid exists ✅ |
| `404` `{"error":"Grid ... not found"}` | Grid already gone |

### 3.8 Swagger UI

```powershell
# Open in browser:
Start-Process http://localhost:9998/swagger
```

| Expected | Status |
|----------|--------|
| Swagger UI loads with API docs | ✅ |
| "Try it out" buttons work | ✅ |

### 3.9 Spawn with missing blueprint

```powershell
$body = '{"blueprint":"NonExistent"}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn -Body $body -ContentType "application/json"
```

| Expected | Status |
|----------|--------|
| `404` `{"error":"Blueprint not found: NonExistent"}` | ✅ |

## 4. In-Game Validation

| Check | Expected |
|-------|----------|
| Grids visible at correct positions | ✅ |
| Physics/collision working (not phantom) | ✅ |
| Blocks clickable, terminal works | ✅ |
| Grids persist after save/load | ✅ |

## 5. Log File

`%APPDATA%\SpaceEngineers\MySpawnerController.log`

Look for:
```
[ApiServer] Listening on ...
[ApiServer] POST /api/v1/spawn
  Deserializing SBC via DeserializeXML...
  DeserializeXML: ok=True, definitions=True
  OnAddedToScene OK
  ActivatePhysics OK
  RegisterCubeGrid OK
  OK: TestGrid_MultiConnectorGrid Id=...
```
