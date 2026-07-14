# MySpawnerController — HTTP API for spawning grids

Плагин поднимает REST API сервер на `localhost:9998` со Swagger UI.
Управляет спавном гридов из локальных чертежей через HTTP-запросы.

## Быстрый старт

### 1. Запустить игру
```batch
run_world.bat
```
Или: `SpaceEngineersLauncher.exe -world "Empty_World_In"`

### 2. Дождаться загрузки мира

### 3. Открыть Swagger UI
```
http://localhost:9998/swagger
```

### 4. Спавн всех тестовых гридов
```powershell
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn-tests
```

## API Endpoints

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/swagger` | Swagger UI |
| `GET` | `/api/v1/openapi.json` | OpenAPI 3.0 спецификация |
| `GET` | `/api/v1/health` | Статус сессии (`{ "ready": true }`) |
| `GET` | `/api/v1/blueprints` | Список чертежей в `%APPDATA%/SpaceEngineers/Blueprints/local/` |
| `GET` | `/api/v1/grids` | Список заспавненных гридов |
| `GET` | `/api/v1/grids/{id}` | Детали конкретного грида |
| `DELETE` | `/api/v1/grids/{id}` | Удалить грид |
| `POST` | `/api/v1/spawn` | Заспавнить грид из чертежа |
| `POST` | `/api/v1/spawn-tests` | Заспавнить все 3 тестовых грида |

### POST /api/v1/spawn — тело запроса

```json
{
  "blueprint": "TestGrid_SingleConnector",
  "displayName": "MyGrid",
  "position": { "x": 100, "y": 0, "z": -200 }
}
```

| Поле | Тип | Обязательно | Описание |
|------|-----|-------------|----------|
| `blueprint` | string | Да | Имя папки чертежа |
| `displayName` | string | Нет | Кастомное имя грида |
| `position` | object | Нет | `{ x, y, z }` — позиция спавна |

### POST /api/v1/spawn-tests

Спавнит 3 грида на позициях:
- `TestGrid_MultiConnectorGrid` @ (0, 0, 0)
- `TestGrid_PBWithPanel` @ (0, 0, 10)
- `TestGrid_SingleConnector` @ (0, 0, -10)

## Примеры PowerShell

```powershell
# Спавн всех тестовых гридов
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn-tests

# Спавн конкретного грида с кастомным именем
$body = '{"blueprint":"TestGrid_SingleConnector","displayName":"MyGrid","position":{"x":50,"y":0,"z":100}}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn -Body $body -ContentType "application/json"

# Проверить здоровье
(Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/health).Content

# Список чертежей
(Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/blueprints).Content

# Список гридов
(Invoke-WebRequest -UseBasicParsing http://localhost:9998/api/v1/grids).Content
```

## Сборка и деплой

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Скрипт:
1. Убивает игру если запущена
2. Билдит `MySpawnerController.Api` (netstandard2.0)
3. Билдит `MySpawnerController` (.NET Framework 4.8)
4. Копирует DLL и NuGet-зависимости в `Bin64/Plugins/`
5. Регистрирует плагин в `config.xml`

## Архитектура

```
MySpawnerController.Api/     — netstandard2.0, REST API + Swagger
  ├── Models.cs              — DTOs + ISpawnService
  ├── ApiServer.cs           — HttpListener routing, JSON
  └── SwaggerPage.cs         — Swagger UI HTML + OpenAPI spec

MySpawnerController/         — .NET Framework 4.8, PluginLoader target
  ├── Plugin.cs              — IPlugin entry point
  ├── Logger.cs              — file logger
  ├── SessionComponent.cs    — init ApiServer + SpawnService
  └── SpawnService.cs        — ISpawnService impl (main-thread spawn)
```

## Логи

`%APPDATA%\SpaceEngineers\MySpawnerController.log`

## Порты

- `9998` — MySpawnerController (этот плагин)
