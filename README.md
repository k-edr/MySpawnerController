# MySpawnerController — HTTP API for spawning grids

Плагин поднимает JSON API на `localhost:9997`.
Сваггер запускается **отдельным процессом** на `localhost:9998`.

## Быстрый старт (всё одной командой)

```powershell
powershell -ExecutionPolicy Bypass -File build_and_run.ps1
```

Или двойным кликом: `build_and_run.bat`

Что делает:
1. Билдит все 3 проекта
2. Копирует DLL в `Bin64/Plugins/`
3. Запускает игру с миром `Empty_World_In`
4. Запускает сваггер в отдельном окне
5. Ждёт загрузки мира
6. Спавнит 3 тестовых грида

## Ручной запуск

### Собрать и задеплоить
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

### Запустить игру
```batch
run_world.bat
```

### Запустить сваггер (отдельный терминал)
```batch
run_swagger.bat
```
→ Открыть `http://localhost:9998/swagger`

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
MySpawnerController.Api/          — netstandard2.0 library: DTOs, ISpawnService, ApiServer
MySpawnerController/              — .NET Framework 4.8: PluginLoader plugin, SpawnService, game API
MySpawnerController.Swagger/      — .NET 8.0 console app: Swagger UI server (separate process)
```

## Логи

`%APPDATA%\SpaceEngineers\MySpawnerController.log`
