namespace MySpawnerController.Api
{
    /// <summary>
    /// Self-contained API docs page. Served at GET /swagger.
    /// No CDN dependencies — works fully offline.
    /// </summary>
    public static class SwaggerPage
    {
        public static readonly string Html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>MySpawnerController API</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: system-ui, -apple-system, sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 20px; }
  h1 { color: #4fc3f7; margin-bottom: 8px; }
  .sub { color: #888; margin-bottom: 24px; }
  .card { background: #16213e; border-radius: 8px; padding: 16px; margin-bottom: 12px; border-left: 4px solid #4fc3f7; }
  .card:hover { background: #1c2a4a; }
  .method { display: inline-block; padding: 2px 8px; border-radius: 4px; font-weight: bold; font-size: 12px; margin-right: 8px; min-width: 52px; text-align: center; }
  .get    { background: #2e7d32; color: #fff; }
  .post   { background: #1565c0; color: #fff; }
  .delete { background: #c62828; color: #fff; }
  .path   { font-family: monospace; font-size: 15px; }
  .desc   { margin-top: 6px; color: #aaa; font-size: 13px; }
  .body-example { margin-top: 8px; }
  .body-example summary { cursor: pointer; color: #81c784; font-size: 13px; }
  .body-example pre { background: #0d1117; color: #7ee787; padding: 10px; border-radius: 4px; overflow-x: auto; font-size: 12px; margin-top: 4px; }
  a { color: #64b5f6; }
</style>
</head>
<body>

<h1>MySpawnerController API</h1>
<p class=""sub"">REST API for spawning Space Engineers grids from local blueprints. Port 9998.</p>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/health</span>
  <div class=""desc"">Session readiness. Returns {""ready"": true} when world is loaded.</div>
</div>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/blueprints</span>
  <div class=""desc"">List blueprints in %APPDATA%/SpaceEngineers/Blueprints/local/</div>
</div>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/grids</span>
  <div class=""desc"">List all spawned grids (id, name, position).</div>
</div>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/grids/{id}</span>
  <div class=""desc"">Get full grid details: name, position, velocity, blocks[] with type and gridPosition.</div>
</div>

<div class=""card"">
  <span class=""method delete"">DELETE</span>
  <span class=""path"">/api/v1/grids/{id}</span>
  <div class=""desc"">Remove a spawned grid from the world.</div>
</div>

<div class=""card"">
  <span class=""method post"">POST</span>
  <span class=""path"">/api/v1/spawn</span>
  <div class=""desc"">Spawn a grid from a local blueprint.</div>
  <details class=""body-example"">
    <summary>Request body example</summary>
    <pre>{
  ""blueprint"": ""TestGrid_SingleConnector"",
  ""displayName"": ""MyGrid"",
  ""position"": { ""x"": 100, ""y"": 0, ""z"": -200 }
}</pre>
  </details>
</div>

<div class=""card"">
  <span class=""method post"">POST</span>
  <span class=""path"">/api/v1/spawn-tests</span>
  <div class=""desc"">Spawn all 3 test grids at default positions:
  <br>TestGrid_MultiConnectorGrid @ (0,0,0)
  <br>TestGrid_PBWithPanel @ (0,0,10)
  <br>TestGrid_SingleConnector @ (0,0,-10)</div>
</div>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/openapi.json</span>
  <div class=""desc""><a href=""/api/v1/openapi.json"">OpenAPI 3.0 spec</a> (for Swagger UI / Postman import).</div>
</div>

<p style=""margin-top:24px;color:#666;font-size:12px;"">
  PowerShell quick test:
  <code style=""color:#81c784;"">Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn-tests</code>
</p>

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
