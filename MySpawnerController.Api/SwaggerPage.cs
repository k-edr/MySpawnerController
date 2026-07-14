namespace MySpawnerController.Api
{
    /// <summary>
    /// Embedded Swagger UI page. Served at GET /swagger.
    /// Uses Swagger UI from CDN with an inline OpenAPI 3.0 spec.
    /// </summary>
    public static class SwaggerPage
    {
        public static readonly string Html = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>MySpawnerController API</title>
  <link rel=""stylesheet"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"">
</head>
<body>
  <div id=""swagger-ui""></div>
  <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
  <script>
    SwaggerUIBundle({
      url: ""/api/v1/openapi.json"",
      dom_id: ""#swagger-ui"",
      deepLinking: true,
      defaultModelsExpandDepth: 1,
    });
  </script>
</body>
</html>";

        public static readonly string OpenApiSpec = @"{
  ""openapi"": ""3.0.3"",
  ""info"": {
    ""title"": ""MySpawnerController API"",
    ""version"": ""1.0.0"",
    ""description"": ""HTTP API for spawning Space Engineers grids from local blueprints.""
  },
  ""servers"": [{ ""url"": ""http://localhost:9998"" }],
  ""paths"": {
    ""/api/v1/health"": {
      ""get"": {
        ""summary"": ""Health check"",
        ""responses"": { ""200"": { ""description"": ""Session status"" } }
      }
    },
    ""/api/v1/blueprints"": {
      ""get"": {
        ""summary"": ""List available blueprints"",
        ""responses"": { ""200"": { ""description"": ""Blueprint list"" } }
      }
    },
    ""/api/v1/grids"": {
      ""get"": {
        ""summary"": ""List all spawned grids"",
        ""responses"": { ""200"": { ""description"": ""Grid list"" } }
      }
    },
    ""/api/v1/grids/{id}"": {
      ""get"": {
        ""summary"": ""Get grid details"",
        ""parameters"": [{ ""name"": ""id"", ""in"": ""path"", ""required"": true, ""schema"": { ""type"": ""integer"" } }],
        ""responses"": { ""200"": { ""description"": ""Grid details"" }, ""404"": { ""description"": ""Not found"" } }
      },
      ""delete"": {
        ""summary"": ""Remove grid"",
        ""parameters"": [{ ""name"": ""id"", ""in"": ""path"", ""required"": true, ""schema"": { ""type"": ""integer"" } }],
        ""responses"": { ""200"": { ""description"": ""Deleted"" }, ""404"": { ""description"": ""Not found"" } }
      }
    },
    ""/api/v1/spawn"": {
      ""post"": {
        ""summary"": ""Spawn a grid from blueprint"",
        ""requestBody"": {
          ""required"": true,
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""type"": ""object"",
                ""required"": [""blueprint""],
                ""properties"": {
                  ""blueprint"": { ""type"": ""string"", ""description"": ""Blueprint folder name"" },
                  ""displayName"": { ""type"": ""string"", ""description"": ""Custom grid display name"" },
                  ""static"": { ""type"": ""boolean"", ""description"": ""Static grid"" },
                  ""position"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""x"": { ""type"": ""number"" },
                      ""y"": { ""type"": ""number"" },
                      ""z"": { ""type"": ""number"" }
                    }
                  }
                }
              }
            }
          }
        },
        ""responses"": {
          ""200"": { ""description"": ""Spawn result with grid details"" },
          ""400"": { ""description"": ""Missing blueprint"" },
          ""404"": { ""description"": ""Blueprint not found"" },
          ""503"": { ""description"": ""Session not ready"" }
        }
      }
    },
    ""/api/v1/spawn-tests"": {
      ""post"": {
        ""summary"": ""Spawn all 3 test grids"",
        ""responses"": { ""200"": { ""description"": ""Spawn results"" } }
      }
    }
  }
}";
    }
}
