using System.Text.Json;
using Microsoft.OpenApi.Models;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GridSpawner.Swagger.Application;

/// <summary>
/// Builds the OpenAPI document describing the game API.
/// Schemas are auto-generated from <see cref="Shared"/> DTOs via SchemaGenerator.
/// </summary>
public static class OpenApiDocumentBuilder
{
    public static OpenApiDocument Build(AppConfig config)
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
                Description = $"HTTP API for spawning Space Engineers grids from local blueprints. Game API runs on port {config.ApiPort}."
            },
            Servers = [new OpenApiServer { Url = $"{config.ApiScheme}://{config.DisplayHost}:{config.ApiPort}" }],
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
        generator.GenerateSchema(typeof(TerminalBlockDto), repo);
        generator.GenerateSchema(typeof(BlockActionDto), repo);
        generator.GenerateSchema(typeof(BlockPropertyDto), repo);
        generator.GenerateSchema(typeof(BlockActionRequest), repo);
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
                "Spawn all 3 test grids", "spawnTests", Ref(nameof(SpawnResponse))),

            // ── Terminal block interaction ──

            ["/api/v1/grids/{id}/blocks"] = SimplePath(OperationType.Get,
                "List functional blocks on a grid", "listGridBlocks",
                ArrayOf(nameof(TerminalBlockDto))),

            ["/api/v1/grids/{id}/blocks/{x}/{y}/{z}"] = BlockDetail(repo),

            ["/api/v1/grids/{id}/blocks/{x}/{y}/{z}/action"] = BlockActionOp(repo),

            ["/api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}"] = BlockPropertyOp(repo)
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

    // ── Block operation builders ──────────────────────────

    private static OpenApiPathItem BlockDetail(SchemaRepository repo)
    {
        var parameters = GridBlockPathParams();
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Summary = "Get block details with actions and properties",
                    OperationId = "getBlockDetail",
                    Parameters = parameters,
                    Responses = new OpenApiResponses
                    {
                        ["200"] = Ok(Ref(nameof(TerminalBlockDto))),
                        ["404"] = Error("Block not found")
                    }
                }
            }
        };
    }

    private static OpenApiPathItem BlockActionOp(SchemaRepository repo)
    {
        var parameters = GridBlockPathParams();
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Summary = "Execute a terminal action on a block",
                    OperationId = "executeBlockAction",
                    Parameters = parameters,
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = { ["application/json"] = new OpenApiMediaType
                            { Schema = Ref("BlockActionRequest") } }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Action executed" },
                        ["404"] = Error("Block or action not found")
                    }
                }
            }
        };
    }

    private static OpenApiPathItem BlockPropertyOp(SchemaRepository repo)
    {
        var parameters = new List<OpenApiParameter>(GridBlockPathParams())
        {
            new OpenApiParameter
            {
                Name = "propId", In = ParameterLocation.Path, Required = true,
                Schema = new OpenApiSchema { Type = "string" }
            }
        };
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Summary = "Get a property value from a block",
                    OperationId = "getBlockProperty",
                    Parameters = parameters,
                    Responses = new OpenApiResponses
                    {
                        ["200"] = Ok(new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["propertyId"] = new() { Type = "string" },
                                ["value"] = new() { Type = "string" }
                            }
                        }),
                        ["404"] = Error("Property not found")
                    }
                },
                [OperationType.Put] = new OpenApiOperation
                {
                    Summary = "Set a property value on a block",
                    OperationId = "setBlockProperty",
                    Parameters = parameters,
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = { ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["value"] = new() { Type = "string" }
                                }
                            }
                        }}
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Property set" },
                        ["404"] = Error("Block or property not found")
                    }
                }
            }
        };
    }

    private static List<OpenApiParameter> GridBlockPathParams() => new()
    {
        new OpenApiParameter
        {
            Name = "id", In = ParameterLocation.Path, Required = true,
            Schema = new OpenApiSchema { Type = "integer", Format = "int64" }
        },
        new OpenApiParameter
        {
            Name = "x", In = ParameterLocation.Path, Required = true,
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        },
        new OpenApiParameter
        {
            Name = "y", In = ParameterLocation.Path, Required = true,
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        },
        new OpenApiParameter
        {
            Name = "z", In = ParameterLocation.Path, Required = true,
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        }
    };
}
