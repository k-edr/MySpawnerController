# GridSpawner — HTTP API for spawning grids

Плагин поднимает JSON API на `localhost:9997`.
Сваггер запускается **отдельным процессом** на `localhost:9998`.

## Быстрый старт (всё одной командой)

```
.\run.bat
```

Что делает:
1. Билдит все 4 проекта
2. Копирует DLL в `Bin64/Plugins/`
3. Запускает игру (AutoWorldLoader загружает мир)
4. Запускает сваггер в отдельном окне
5. Ждёт загрузки мира
6. Спавнит 3 тестовых грида

## Настройка путей

Перед первым билдом скопируй шаблон и укажи свой путь к `Bin64`:

```powershell
copy build-config.example.json build-config.json
# отредактируй build-config.json
```

```json
{
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64"
}
```

| Поле | Описание | Пример (Linux/Proton) |
|------|----------|----------------------|
| `seBin64` | Путь к `Bin64` игры | `"Z:/home/user/.steam/.../Bin64"` |

Если файла нет — скрипты используют путь по умолчанию.

## Runtime-конфиг (`%APPDATA%\SpaceEngineers\GridSpawner.json`)

Создаётся автоматически при первом запуске плагина:

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

| Поле | Описание |
|------|----------|
| `apiPort` | Порт JSON API |
| `swaggerPort` | Порт Swagger UI |
| `swaggerCorsOrigin` | CORS origin (менять если хост не localhost) |
| `blueprintsFolder` | Переопределить папку чертежей (`null` = `%APPDATA%\SpaceEngineers\Blueprints\local`) |
| `apiKey` | Ключ для защищённых эндпоинтов (`null` = без авторизации) |
| `maxBlueprintFileSizeBytes` | Макс. размер `.sbc` файла (по умолчанию 50 МБ) |
| `maxGridsPerBlueprint` | Макс. число гридов в одном чертеже |

## Только сборка

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

## Порты

| Порт | Что | Кто |
|------|-----|-----|
| `9997` | JSON API (спавн, гриды, хелс) | Игровой модуль |
| `9998` | Swagger UI | Отдельный процесс |

## API Endpoints (все на порту 9997)

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/api/v1/health` | `{"ready": true/false}` |
| `GET` | `/api/v1/blueprints` | Список чертежей |
| `GET` | `/api/v1/grids` | Список заспавненных гридов |
| `GET` | `/api/v1/grids/{id}` | Детали грида |
| `DELETE` | `/api/v1/grids/{id}` | Удалить грид |
| `POST` | `/api/v1/spawn` | Спавн грида `{"blueprint":"...", "position":{"x":0,"y":0,"z":0}}` |
| `POST` | `/api/v1/spawn-tests` | Спавн 3 тестовых грида |

## Примеры PowerShell

```powershell
# Спавн всех тестовых гридов
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn-tests

# Спавн с кастомной позицией
$body = '{"blueprint":"TestGrid_SingleConnector","displayName":"MyGrid","position":{"x":50,"y":0,"z":100}}'
Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn -Body $body -ContentType "application/json"

# Проверить здоровье
Invoke-WebRequest -UseBasicParsing http://localhost:9997/api/v1/health
```

## Архитектура

```
GridSpawner.Shared/        — netstandard2.0: Configuration/ + Models/ (DTOs)
GridSpawner.Api/           — netstandard2.0: Application/ + Infrastructure/
GridSpawner.Plugin/        — .NET 4.8: Application/ + Infrastructure/
GridSpawner.Swagger/       — .NET 8.0: Application/ + Infrastructure/ (Swashbuckle)
```

## Логи

`%APPDATA%\SpaceEngineers\GridSpawner.log`
