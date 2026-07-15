# Отчёт по рефакторингу — 2026-07-15

## Обзор

Полная чистка всех четырёх проектов. Вынес константы, разбил длинные методы, заменил молчаливые `catch` на логирование, убрал хардкод строк — всё в конфиг.

---

## 1. Константы — `AppDefaults.cs` (новый)

**Файл**: `GridSpawner.Shared/Configuration/AppDefaults.cs`

Все магические строки/пути/порты в одном месте:

| Константа | Значение |
|---|---|
| `AppDataRoot` | `%APPDATA%/SpaceEngineers` |
| `DefaultBlueprintsFolder` | `%APPDATA%/SpaceEngineers/Blueprints/local` |
| `DefaultConfigFile` | `%APPDATA%/SpaceEngineers/GridSpawner.json` |
| `DefaultLogFile` | `%APPDATA%/SpaceEngineers/GridSpawner.log` |
| `BlueprintExtension` | `bp.sbc` |
| `DefaultHost` | `+` (все интерфейсы) |
| `DefaultApiPort` | `9997` |
| `DefaultSwaggerPort` | `9998` |

---

## 2. Конфиг — `AppConfig.cs`

- Убран `ApiKey` (пока не нужен)
- `BlueprintsFolder` больше не nullable — по умолчанию `AppDefaults.DefaultBlueprintsFolder`
- Добавлено поле `ApiHost` (по умолчанию `+`), используется везде вместо хардкодного `localhost`
- Все дефолтные значения берутся из `AppDefaults`

---

## 3. Файлы, переведённые на константы

| Файл | Что поменялось |
|---|---|
| `AppConfigLoader.cs` | `ConfigPath` → `AppDefaults.DefaultConfigFile` |
| `Logger.cs` | `LogFilePath` → `AppDefaults.DefaultLogFile` |
| `BlueprintHandler.cs` | Папка блюпринтов → `AppDefaults.DefaultBlueprintsFolder` |
| `SpawnOrchestrator.cs` | Фолбэк папки, расширение `bp.sbc` → `AppDefaults` |
| `ApiServer.cs` | `localhost` → `config.ApiHost` |
| `Plugin.cs` | Убран хардкод портов из лог-сообщения |
| `Program.cs` (Swagger) | Путь к конфигу, хост → `AppDefaults` + `config.ApiHost` |
| `OpenApiDocumentBuilder.cs` | Принимает `AppConfig` вместо `int gamePort`, хост из конфига |
| `SwaggerHostExtensions.cs` | `ConfigureSwagger` принимает `AppConfig`, хост в баннере + JS |

---

## 4. SpawnOrchestrator — разбит на подметоды

**Файл**: `GridSpawner.Api/Application/SpawnOrchestrator.cs`

`SpawnFromJson` разделён на:
- `DeserializeRequest(json)` — разбор JSON
- `ResolveBlueprintPath(name)` — сборка пути
- `DoSpawn(req, bpPath)` — сам спавн

---

## 5. BlueprintDeserializer — разбит на подметоды

**Файл**: `GridSpawner.Plugin/Application/BlueprintDeserializer.cs`

`Deserialize` разделён на:
- `ValidateFile(bpFile, config, out error)` — проверка существования + размера
- `DeserializeXml(bpFile)` — разбор XML
- `ExtractGridBuilders(definitions, config, out error)` — проверка количества гридов + извлечение

---

## 6. Молчаливые catch → логирование

| Файл | Метод | Исправление |
|---|---|---|
| `HttpResponseHelper.cs` | `ReadBody<T>` | Логирует `Exception.Message` |
| `HttpResponseHelper.cs` | `Respond` | Логирует `Exception.Message` |
| `FatBlockInfoExtractor.cs` | `GetTypeName` | `Logger.Warn` |
| `GridDtoMapper.cs` | `ToDto` (позиция) | `Logger.Warn` |
| `GridDtoMapper.cs` | `ToDto` (скорость) | `Logger.Warn` |

---

## 7. SpawnService — очистка по игровому времени

**Файл**: `GridSpawner.Plugin/Application/SpawnService.cs`

- `_frameCounter % 600` заменён на `_cleanupAccumulator`, накапливающий `MySession.Static.ElapsedGameTime.TotalSeconds`
- Очистка мёртвых гридов — каждые **10 секунд симуляции**, независимо от FPS

---

## 8. TerminalBlockDto — `List<T>` → `IReadOnlyList<T>`

**Файл**: `GridSpawner.Shared/Models/TerminalBlockDto.cs`

- `Actions`: `List<BlockActionDto>` → `IReadOnlyList<BlockActionDto>`
- `Properties`: `List<BlockPropertyDto>` → `IReadOnlyList<BlockPropertyDto>`

---

## Билд

Все 4 проекта собираются без ошибок:
```
Shared Build OK
API Build OK
Main Build OK
Swagger Build OK
```

---

## Ветка

`feature/terminal-block-interaction` (не запушена)
