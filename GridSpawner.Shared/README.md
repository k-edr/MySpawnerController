# GridSpawner.Shared

Shared DTOs and configuration — referenced by all other projects.

## Architecture (Clean Architecture)

```
GridSpawner.Shared/                      (netstandard2.0)
├── Configuration/
│   └── AppConfig.cs                     ← ports, limits, keys
├── Models/
│   ├── BlockDto.cs                      ← block info
│   ├── BlueprintInfo.cs                 ← blueprint listing
│   ├── ErrorResponse.cs                 ← error payload
│   ├── GridDto.cs                       ← full grid details
│   ├── GridListItem.cs                  ← grid summary
│   ├── HealthResponse.cs                ← ready status
│   ├── SpawnPosition.cs                 ← x/y/z position
│   ├── SpawnRequest.cs                  ← spawn input
│   ├── SpawnResponse.cs                 ← spawn output
│   ├── Vector3Dto.cs                    ← double-precision vector
│   └── Vector3IDto.cs                   ← integer vector (grid coords)
├── GridSpawner.json                     ← config template
└── GridSpawner.Shared.csproj
```

## Namespaces

| Namespace | Contents |
|-----------|----------|
| `GridSpawner.Shared.Configuration` | `AppConfig` |
| `GridSpawner.Shared.Models` | All DTOs |

## Build

```powershell
dotnet build GridSpawner.Shared -c Release
```

Targets `netstandard2.0` — compatible with both .NET Framework 4.8 (game plugin) and .NET 8.0 (Swagger).
