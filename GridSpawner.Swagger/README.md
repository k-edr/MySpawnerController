# GridSpawner.Swagger

Swagger UI for the [GridSpawner.Plugin](../README.md) game plugin API.
Built with **ASP.NET Core minimal API** + **Swashbuckle**.

## Quick start

```powershell
# Build + run (from repo root)
.\build.ps1
.\run_swagger.bat

# Or run directly
dotnet run --project GridSpawner.Swagger
```

- **Swagger UI:** http://localhost:9998/swagger
- **OpenAPI JSON:** http://localhost:9998/swagger/v1/swagger.json
- **Game API:** http://localhost:9997

## Project structure

```
GridSpawner.Swagger/
├── Application/                         ← Use cases (Shared DTOs + OpenApi)
│   ├── OpenApiDocumentBuilder.cs        ← builds OpenApiDocument from Shared DTOs
│   └── GameApiSwaggerProvider.cs        ← ISwaggerProvider impl
├── Infrastructure/                      ← Hosting (Application + ASP.NET Core)
│   └── SwaggerHostExtensions.cs         ← UseSwagger/UseSwaggerUI + connection JS
├── Program.cs                           ← Composition root (20 lines)
├── README.md
└── GridSpawner.Swagger.csproj
```

**Dependency direction:** `Program → Infrastructure → Application → Shared`

DTOs live in `GridSpawner.Shared/` (netstandard2.0) and are shared
across all projects: Api, Swagger, and the game plugin.

## How it works

1. **Shared** — DTOs (`SpawnRequest`, `GridDto`, `BlockDto`, etc.) with
   `[JsonPropertyName]` attributes for SnakeCase JSON serialization.
2. **Application** — `OpenApiDocumentBuilder` generates schemas from Shared DTOs
   via Swashbuckle's `SchemaGenerator` and builds the full `OpenApiDocument`.
   `GameApiSwaggerProvider` serves it to the Swashbuckle middleware.
3. **Infrastructure** — Wires up Swagger UI, injects the connection status dot
   (polls `GET /api/v1/health`), maps redirect `/` → `/swagger`.
4. **Program.cs** — Thin composition root: build doc, register services, run.

The OpenAPI spec's `servers[0].url` points to `http://localhost:9997` (the
game), so "Try it out" sends requests directly to the game API.

## Adding a new endpoint

1. Add/update DTO in `GridSpawner.Shared/`
2. Add path entry in `OpenApiDocumentBuilder.BuildPaths()`
3. Implement endpoint in `ApiServer.cs` (game plugin)
4. Spec auto-updates — no JSON strings to edit

## Build output

`build.ps1` copies the exe + DLLs + JSONs to `Bin64/Plugins/Swagger/`.
The project uses `Microsoft.NET.Sdk.Web` with framework-dependent deployment —
.NET 8 runtime must be installed on the target machine.

## Dependencies

| Package                | Version |
|------------------------|---------|
| Swashbuckle.AspNetCore | 6.9.0   |
| Microsoft.OpenApi      | 1.6.14 (transitive) |
