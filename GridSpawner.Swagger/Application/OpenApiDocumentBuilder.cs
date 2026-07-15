using System.Text.Json;
using Microsoft.OpenApi.Models;
using GridSpawner.Shared;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GridSpawner.Swagger.Application;

/// <summary>
/// Builds the OpenAPI document describing the game API.
/// Schemas are auto-generated from <see cref="Shared"/> DTOs via SchemaGenerator.
/// </summary>
public static class OpenApiDocumentBuilder
{
    public static OpenApiDocument Build(int gamePort)
    {
        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var schemaGen = new SchemaGenerator(
            new SchemaGeneratorOptions(),
            new JsonSerializerDataContractResolver(jsonOpts));
        var schemaRepo = new SchemaRepository();

        RegisterSchemas(schemaGen, schemaRepo);

        return new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "GridSpawner.Plugin API",
                Version = "1.0.0",
                Description = $"HTTP API for spawning Space Engineers grids from local blueprints. Game API runs on port {gamePort}."
            },
            Servers = [new OpenApiServer { Url = $"http://localhost:{gamePort}" }],
            Paths = BuildPaths(schemaRepo),
            Components = new OpenApiComponents { Schemas = schemaRepo.Schemas }
        };
    }

    private static void RegisterSchemas(SchemaGenerator generator, SchemaRepository repo)
    {
        generator.GenerateSchema(typeof(SpawnRequest), repo);
        generator.GenerateSchema(typeof(SpawnPosition), repo);
        generator.GenerateSchema(typeof(SpawnResponse), repo);
        generator.GenerateSchema(typeof(GridDto), repo);
        generator.GenerateSchema(typeof(BlockDto), repo);
        generator.GenerateSchema(typeof(Vector3Dto), repo);
        generator.GenerateSchema(typeof(Vector3IDto), repo);
        generator.GenerateSchema(typeof(BlueprintInfo), repo);
        generator.GenerateSchema(typeof(GridListItem), repo);
        generator.GenerateSchema(typeof(ErrorResponse), repo);
        generator.GenerateSchema(typeof(HealthResponse), repo);
    }

    private static OpenApiPaths BuildPaths(SchemaRepository repo)
    {
        return new OpenApiPaths
        {
            ["/api/v1/health"] = SimplePath(OperationType.Get,
                "Health check", "getHealth", Ref(nameof(HealthResponse))),

            ["/api/v1/blueprints"] = SimplePath(OperationType.Get,
                "List available blueprints", "listBlueprints",
                ArrayOf(nameof(BlueprintInfo))),

            ["/api/v1/grids"] = SimplePath(OperationType.Get,
                "List all spawned grids", "listGrids",
                ArrayOf(nameof(GridListItem))),

            ["/api/v1/grids/{id}"] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = GridById(OperationType.Get, "Get grid details", "getGrid", repo),
                    [OperationType.Delete] = GridById(OperationType.Delete, "Remove grid", "deleteGrid", repo)
                }
            },

            ["/api/v1/spawn"] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = SpawnOperation()
                }
            },

            ["/api/v1/spawn-tests"] = SimplePath(OperationType.Post,
                "Spawn all 3 test grids", "spawnTests", Ref(nameof(SpawnResponse)))
        };
    }

    // ── Path builders ─────────────────────────────────────────

    private static OpenApiPathItem SimplePath(OperationType op, string summary, string opId, OpenApiSchema responseSchema)
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [op] = new OpenApiOperation
                {
                    Summary = summary,
                    OperationId = opId,
                    Responses = new OpenApiResponses { ["200"] = Ok(responseSchema) }
                }
            }
        };
    }

    private static OpenApiOperation GridById(OperationType op, string summary, string opId, SchemaRepository repo)
    {
        var parameters = new[]
        {
            new OpenApiParameter
            {
                Name = "id", In = ParameterLocation.Path, Required = true,
                Schema = new OpenApiSchema { Type = "integer", Format = "int64" }
            }
        };

        if (op == OperationType.Get)
        {
            return new OpenApiOperation
            {
                Summary = summary, OperationId = opId, Parameters = parameters,
                Responses = new OpenApiResponses
                {
                    ["200"] = Ok(Ref(nameof(GridDto))),
                    ["404"] = Error("Not found")
                }
            };
        }

        // DELETE
        return new OpenApiOperation
        {
            Summary = summary, OperationId = opId, Parameters = parameters,
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Deleted" },
                ["404"] = Error("Not found")
            }
        };
    }

    private static OpenApiOperation SpawnOperation()
    {
        return new OpenApiOperation
        {
            Summary = "Spawn a grid from blueprint",
            OperationId = "spawnGrid",
            RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = { ["application/json"] = new OpenApiMediaType
                    { Schema = Ref(nameof(SpawnRequest)) } }
            },
            Responses = new OpenApiResponses
            {
                ["200"] = Ok(Ref(nameof(SpawnResponse))),
                ["400"] = Error("Bad request"),
                ["404"] = Error("Blueprint not found"),
                ["503"] = Error("Session not ready")
            }
        };
    }

    // ── Response helpers ──────────────────────────────────────

    private static OpenApiResponse Ok(OpenApiSchema schema) => new()
    {
        Description = "OK",
        Content = { ["application/json"] = new OpenApiMediaType { Schema = schema } }
    };

    private static OpenApiResponse Error(string desc) => new()
    {
        Description = desc,
        Content = { ["application/json"] = new OpenApiMediaType
            { Schema = Ref(nameof(ErrorResponse)) } }
    };

    private static OpenApiSchema Ref(string schemaName) => new()
    {
        Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = schemaName }
    };

    private static OpenApiSchema ArrayOf(string itemSchemaName) => new()
    {
        Type = "array", Items = Ref(itemSchemaName)
    };
}
