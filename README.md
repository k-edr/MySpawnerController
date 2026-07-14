# MySpawnerController — HTTP-управляемый спавнер гридов

Плагин поднимает HTTP-сервер на `localhost:9998`. Отправляешь GET-запрос — гриды спавнятся.

## Использование

### 1. Запустить игру
```batch
run_world.bat
```
Или вручную: `SpaceEngineersLauncher.exe -world "Empty World 2026-07-14 21-40"`

### 2. Дождаться загрузки мира (~5 сек)

### 3. Отправить команду

**Спавн всех трёх гридов (позиции по умолчанию):**
```powershell
Invoke-WebRequest http://localhost:9998/spawn
```
Или в браузере: `http://localhost:9998/spawn`

**Спавн конкретного грида в произвольную точку:**
```
http://localhost:9998/spawn?name=TestGrid_MultiConnectorGrid&x=100&y=0&z=-200
```

**Проверить, жив ли плагин:**
```
http://localhost:9998/status
```

**Список доступных чертежей:**
```
http://localhost:9998/list
```

## Позиции по умолчанию (GET /spawn без параметров)

| Грид | Позиция |
|------|---------|
| `TestGrid_MultiConnectorGrid` | (0, 0, 0) |
| `TestGrid_PBWithPanel` | (0, 0, 10) |
| `TestGrid_SingleConnector` | (0, 0, –10) |

## Ответы сервера

| Код | Значение |
|-----|----------|
| 200 | Успех. Тело содержит имена/позиции заспавненных гридов |
| 404 | Чертеж не найден или неверный endpoint |
| 500 | Ошибка спавна (см. `MySpawnerController.log`) |
| 503 | Сессия ещё не готова — мир загружается |

## Сборка и деплой

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

## Логи

`%APPDATA%\SpaceEngineers\MySpawnerController.log`

## Порты

- `9998` — MySpawnerController (этот плагин)
- `9999` — SEBlueprintPlugin (если установлен)
