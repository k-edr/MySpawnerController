# Отчет по техническому аудиту проекта MySpawnerController

В ходе аудита исходного кода решения `GridSpawner.sln` были проанализированы архитектура, управление ресурсами, многопоточность, взаимодействие с API Space Engineers (VRage) и система сборки. Были обнаружены как критические проблемы с утечкой ресурсов, так и архитектурные недочеты.

---

## 1. Критические проблемы и баги

### 1.1. Утечка системных дескрипторов (Resource Leak) в `SpawnService.cs`
**Файл:** [SpawnService.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/Application/SpawnService.cs)
* **Суть проблемы:** Каждый метод взаимодействия (например, `Spawn`, `ListGrids`, `GetGridBlocks` и др.) создает задачу `var task = new MainThreadTask<T>();` и ожидает её завершения через `task.Done.Wait(_timeout)`. Класс `MainThreadTask` наследует `IDisposable` и инициализирует `ManualResetEventSlim` для синхронизации. Однако **метод `Dispose()` у `task` никогда не вызывается** после завершения ожидания.
* **Последствия:** При каждом входящем HTTP-запросе в операционной системе навсегда утекает один дескриптор события (`Event Handle`). При длительной работе сервера это приведет к исчерпанию системных ресурсов и нестабильности игры.
* **Решение:** Обернуть вызовы с использованием `task` в конструкцию `using`:
  ```csharp
  using (var task = new MainThreadTask<List<GridListItem>>())
  {
      task.Process = () => task.Result = _tracker.ListGrids();
      _queue.Enqueue(task);
      if (!task.Done.Wait(_timeout)) { ... }
      return task.Result;
  }
  ```

### 1.2. Потенциальные падения игры (Race Condition) в `BlueprintDeserializer.cs`
**Файл:** [BlueprintDeserializer.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/Application/BlueprintDeserializer.cs#L62-L68)
* **Суть проблемы:** Метод десериализации XML `MyObjectBuilderSerializerKeen.DeserializeXML` вызывается на потоке API (в пуле потоков `HttpListenerContext`), а не в главном потоке игры. Сериализаторы Space Engineers/VRage используют глобальные статические состояния и буферы, из-за чего они **не являются потокобезопасными**.
* **Последствия:** Если плагин обрабатывает параллельные запросы на спавн или если игра в этот же момент выполняет внутреннюю сериализацию/десериализацию, это может привести к непредсказуемому поведению или моментальному вылету (Crash) игры на рабочий стол.
* **Решение:** Перенести десериализацию XML в очередь выполнения на главном игровом потоке (`MainThreadTask`), либо защитить вызовы десериализатора Keen критической секцией (`lock`).

### 1.3. Мертвый неиспользуемый код в `PostSpawnFixup.cs`
**Файл:** [PostSpawnFixup.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/Application/PostSpawnFixup.cs)
* **Суть проблемы:** Класс `PostSpawnFixup` содержит методы для инициализации физики сетки и регистрации в сессии (`OnAddedToScene`, `ActivatePhysics`, `RegisterCubeGrid`), которые крайне важны при спавне объектов через рефлексию. Однако **этот класс и его методы не вызываются ни в одной части проекта**.
* **Последствия:** Сетки, созданные через `MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd`, могут спавниться без надлежащей инициализации физики или регистрации в мире, что приводит к багам отображения или зависанию объектов в пространстве до первой интеракции.
* **Решение:** Вызывать `PostSpawnFixup.Apply(grid)` внутри `DoSpawn` в [SpawnService.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/Application/SpawnService.cs#L323).

---

## 2. Замечания по качеству кода и архитектуре

### 2.1. Перезапись файлов лога при каждом запуске
**Файл:** [Logger.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/Infrastructure/Logger.cs#L24-L25)
* **Суть проблемы:** В методе `Logger.Init()` используется `File.WriteAllText(LogFilePath, ...)`, что полностью затирает лог-файл от прошлых сессий игры.
* **Последствия:** В случае сбоя или падения игры при предыдущем запуске, разработчик потеряет важные логи отладки сразу после перезапуска игры.
* **Решение:** Использовать `File.AppendAllText` вместо `WriteAllText`.

### 2.2. Неполное освобождение ресурсов HttpListener
**Файл:** [HttpResponseHelper.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Api/Infrastructure/HttpResponseHelper.cs#L75-L76)
* **Суть проблемы:** В методе `Respond` закрывается только поток вывода `ctx.Response.OutputStream.Close()`, но сам `ctx.Response.Close()` или `ctx.Response.Dispose()` не вызываются.
* **Последствия:** В некоторых версиях .NET Framework / .NET Core это может приводить к задержке освобождения TCP-соединений под высокой нагрузкой.
* **Решение:** Добавить вызов `ctx.Response.Close();` после закрытия стрима.

---

## 3. Сборка и конфигурация

### 3.1. Жесткие ссылки на Release-сборки в `GridSpawner.Plugin.csproj`
**Файл:** [GridSpawner.Plugin.csproj](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Plugin/GridSpawner.Plugin.csproj#L78-L87)
* **Суть проблемы:** Вместо стандартных проектных связей (`ProjectReference`), проект плагина ссылается непосредственно на DLL-файлы из папок `Release` других проектов:
  `..\GridSpawner.Shared\bin\Release\netstandard2.0\GridSpawner.Shared.dll`
* **Последствия:** При сборке в режиме `Debug` проект плагина будет использовать старые артефакты `Release`-сборки (или сборка упадет, если `Release` еще не собирался).
* **Решение:** Заменить `<Reference Include="GridSpawner.Shared">` на стандартные ссылки на проекты:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\GridSpawner.Shared\GridSpawner.Shared.csproj" />
    <ProjectReference Include="..\GridSpawner.Api\GridSpawner.Api.csproj" />
  </ItemGroup>
  ```

### 3.2. Расхождение документации и кода в `AppConfig.cs`
**Файл:** [AppConfig.cs](file:///d:/repos/SE.Plugins/MySpawnerController/GridSpawner.Shared/Configuration/AppConfig.cs#L13-L15)
* **Суть проблемы:** В XML-комментарии указано: `Default + (all interfaces)`, но значение свойства по умолчанию инициализируется как `AppDefaults.DefaultHost` (которое равно `"localhost"`). 
* **Дополнительно:** Запуск на хосте `+` требует прав администратора в Windows. Если пользователь изменит конфиг на `+` без запуска от имени администратора, плагин не запустится. Стоит добавить проверку или информативное логирование на этот случай.

---

### Резюме аудита
Проект спроектирован с хорошей изоляцией слоев (`Infrastructure -> Application -> Shared`). Однако критически важно устранить утечку `ManualResetEventSlim` в `SpawnService.cs` и перевести десериализацию на игровой поток, чтобы гарантировать стабильность плагина при длительных игровых сессиях.
