using System;
using System.Net;
using System.Text;
using System.Threading;

var port = 9998;
var gamePort = 9997;

using var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
listener.Start();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("============================================");
Console.WriteLine(" MySpawnerController Swagger UI");
Console.WriteLine("============================================");
Console.ResetColor();
Console.WriteLine($"  Swagger : http://localhost:{port}/swagger");
Console.WriteLine($"  Game API : http://localhost:{gamePort}");
Console.WriteLine();
Console.WriteLine("  Press Ctrl+C to stop.");
Console.WriteLine();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        var ctx = listener.GetContext();
        ThreadPool.QueueUserWorkItem(_ => Handle(ctx, gamePort));
    }
    catch (HttpListenerException) when (cts.Token.IsCancellationRequested) { break; }
    catch (Exception ex) { Console.WriteLine($"[ERR] {ex.Message}"); }
}

Console.WriteLine("Stopped.");

static void Handle(HttpListenerContext ctx, int gamePort)
{
    try
    {
        var path = ctx.Request.Url.AbsolutePath.Trim('/');
        Console.WriteLine($"[{ctx.Request.HttpMethod}] /{path}");

        if (path is "" or "swagger")
        {
            Html(ctx, SwaggerPage.GetHtml(gamePort));
            return;
        }

        if (path == "favicon.ico")
        {
            Text(ctx, 204, "");
            return;
        }

        if (path == "api/v1/openapi.json")
        {
            Json(ctx, SwaggerPage.GetOpenApiSpec(gamePort));
            return;
        }

        Text(ctx, 404, "Not Found. Try /swagger");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERR] {ex.Message}");
        try { Text(ctx, 500, ex.Message); } catch { }
    }
}

static void Html(HttpListenerContext ctx, string body)
    => Respond(ctx, 200, body, "text/html; charset=utf-8");

static void Json(HttpListenerContext ctx, string body)
    => Respond(ctx, 200, body, "application/json");

static void Text(HttpListenerContext ctx, int code, string body)
    => Respond(ctx, code, body, "text/plain; charset=utf-8");

static void Respond(HttpListenerContext ctx, int code, string body, string ct)
{
    try
    {
        var data = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = ct;
        ctx.Response.ContentLength64 = data.Length;
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.OutputStream.Close();
    }
    catch { }
}

/// <summary>Embedded Swagger UI page and OpenAPI spec.</summary>
static class SwaggerPage
{
    public static string GetHtml(int gamePort) => $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>MySpawnerController API</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; }}
  body {{ font-family: system-ui, -apple-system, sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 20px; }}
  h1 {{ color: #4fc3f7; margin-bottom: 4px; }}
  .sub {{ color: #888; margin-bottom: 8px; font-size:13px; }}
  .status {{ font-size: 13px; margin-bottom: 20px; }}
  .status .dot {{ display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-right: 6px; }}
  .ok {{ color: #4caf50; }} .err {{ color: #f44336; }} .pending {{ color: #ff9800; }}
  .card {{ background: #16213e; border-radius: 8px; padding: 16px; margin-bottom: 12px; border-left: 4px solid #4fc3f7; }}
  .card:hover {{ background: #1c2a4a; }}
  .method {{ display: inline-block; padding: 2px 8px; border-radius: 4px; font-weight: bold; font-size: 12px; margin-right: 8px; min-width: 54px; text-align: center; }}
  .get    {{ background: #2e7d32; color: #fff; }}
  .post   {{ background: #1565c0; color: #fff; }}
  .delete {{ background: #c62828; color: #fff; }}
  .path   {{ font-family: monospace; font-size: 15px; }}
  .desc   {{ margin-top: 6px; color: #aaa; font-size: 13px; }}
  .body-example {{ margin-top: 8px; }}
  .body-example summary {{ cursor: pointer; color: #81c784; font-size: 13px; }}
  .body-example pre {{ background: #0d1117; color: #7ee787; padding: 10px; border-radius: 4px; overflow-x: auto; font-size: 12px; margin-top: 4px; }}
  a {{ color: #64b5f6; }}
  button {{ background: #1565c0; color: #fff; border: none; padding: 6px 14px; border-radius: 4px; cursor: pointer; font-size: 13px; }}
  button:hover {{ background: #1976d2; }}
  .footer {{ margin-top: 24px; color: #666; font-size: 12px; }}
  .footer code {{ color: #81c784; }}
</style>
</head>
<body>

<h1>MySpawnerController API</h1>
<p class=""sub"">REST API for spawning Space Engineers grids from local blueprints.</p>

<div class=""status"" id=""status"">
  <span class=""dot pending"" style=""display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px;background:#ff9800;""></span>
  Game: checking...
</div>

<script>
  fetch('http://localhost:{gamePort}/api/v1/health')
    .then(r => r.json())
    .then(d => {{
      var s = document.getElementById('status');
      if (d.ready) s.innerHTML = '<span style=""display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px;background:#4caf50;""></span> Game: connected (world loaded)';
      else s.innerHTML = '<span style=""display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px;background:#ff9800;""></span> Game: connected (loading)';
    }})
    .catch(() => {{
      document.getElementById('status').innerHTML = '<span style=""display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px;background:#f44336;""></span> Game: not connected (start Space Engineers first)';
    }});
</script>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/health</span>
  <div class=""desc"">Session readiness (need for spawn). Returns ready: true/false.</div>
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
  <span class=""path"">/api/v1/grids/{{id}}</span>
  <div class=""desc"">Full grid details: name, position, velocity, blocks[] with type and gridPosition.</div>
</div>

<div class=""card"">
  <span class=""method delete"">DELETE</span>
  <span class=""path"">/api/v1/grids/{{id}}</span>
  <div class=""desc"">Remove a spawned grid from the world.</div>
</div>

<div class=""card"">
  <span class=""method post"">POST</span>
  <span class=""path"">/api/v1/spawn</span>
  <div class=""desc"">Spawn a grid from a local blueprint.</div>
  <details class=""body-example"">
    <summary>Request body example</summary>
    <pre>{{
  ""blueprint"": ""TestGrid_SingleConnector"",
  ""displayName"": ""MyGrid"",
  ""position"": {{ ""x"": 100, ""y"": 0, ""z"": -200 }}
}}</pre>
  </details>
</div>

<div class=""card"">
  <span class=""method post"">POST</span>
  <span class=""path"">/api/v1/spawn-tests</span>
  <div class=""desc"">Spawn all 3 test grids at default positions:
  <br>TestGrid_MultiConnectorGrid @ (0,0,0)
  <br>TestGrid_PBWithPanel @ (0,0,10)
  <br>TestGrid_SingleConnector @ (0,0,-10)</div>
  <div style=""margin-top:8px;"">
    <button onclick=""fetch('http://localhost:{gamePort}/api/v1/spawn-tests',{{method:'POST'}}).then(r=>r.json()).then(d=>alert('Spawned '+d.grids.length+' grid(s)')).catch(e=>alert('Error: '+e))"">Spawn All</button>
  </div>
</div>

<div class=""card"">
  <span class=""method get"">GET</span>
  <span class=""path"">/api/v1/openapi.json</span>
  <div class=""desc""><a href=""/api/v1/openapi.json"">OpenAPI 3.0 spec</a> (import into Postman / Swagger Editor).</div>
</div>

<div class=""footer"">
  Game API port: <code>{gamePort}</code> &nbsp;|&nbsp; Swagger port: <code>9998</code>
  <br>PowerShell: <code>Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:{gamePort}/api/v1/spawn-tests</code>
</div>

</body>
</html>";

    public static string GetOpenApiSpec(int gamePort) => $@"{{
  ""openapi"": ""3.0.3"",
  ""info"": {{
    ""title"": ""MySpawnerController API"",
    ""version"": ""1.0.0"",
    ""description"": ""HTTP API for spawning Space Engineers grids from local blueprints.""
  }},
  ""servers"": [{{ ""url"": ""http://localhost:{gamePort}"" }}],
  ""paths"": {{
    ""/api/v1/health"": {{
      ""get"": {{
        ""summary"": ""Health check"",
        ""responses"": {{ ""200"": {{ ""description"": ""Session status"" }} }}
      }}
    }},
    ""/api/v1/blueprints"": {{
      ""get"": {{
        ""summary"": ""List available blueprints"",
        ""responses"": {{ ""200"": {{ ""description"": ""Blueprint list"" }} }}
      }}
    }},
    ""/api/v1/grids"": {{
      ""get"": {{
        ""summary"": ""List all spawned grids"",
        ""responses"": {{ ""200"": {{ ""description"": ""Grid list"" }} }}
      }}
    }},
    ""/api/v1/grids/{{id}}"": {{
      ""get"": {{
        ""summary"": ""Get grid details"",
        ""parameters"": [{{ ""name"": ""id"", ""in"": ""path"", ""required"": true, ""schema"": {{ ""type"": ""integer"" }} }}],
        ""responses"": {{ ""200"": {{ ""description"": ""Grid details"" }}, ""404"": {{ ""description"": ""Not found"" }} }}
      }},
      ""delete"": {{
        ""summary"": ""Remove grid"",
        ""parameters"": [{{ ""name"": ""id"", ""in"": ""path"", ""required"": true, ""schema"": {{ ""type"": ""integer"" }} }}],
        ""responses"": {{ ""200"": {{ ""description"": ""Deleted"" }}, ""404"": {{ ""description"": ""Not found"" }} }}
      }}
    }},
    ""/api/v1/spawn"": {{
      ""post"": {{
        ""summary"": ""Spawn a grid from blueprint"",
        ""requestBody"": {{
          ""required"": true,
          ""content"": {{
            ""application/json"": {{
              ""schema"": {{
                ""type"": ""object"",
                ""required"": [""blueprint""],
                ""properties"": {{
                  ""blueprint"": {{ ""type"": ""string"", ""description"": ""Blueprint folder name"" }},
                  ""displayName"": {{ ""type"": ""string"", ""description"": ""Custom grid display name"" }},
                  ""static"": {{ ""type"": ""boolean"", ""description"": ""Static grid"" }},
                  ""position"": {{
                    ""type"": ""object"",
                    ""properties"": {{
                      ""x"": {{ ""type"": ""number"" }},
                      ""y"": {{ ""type"": ""number"" }},
                      ""z"": {{ ""type"": ""number"" }}
                    }}
                  }}
                }}
              }}
            }}
          }}
        }},
        ""responses"": {{
          ""200"": {{ ""description"": ""Spawn result with grid details"" }},
          ""400"": {{ ""description"": ""Missing blueprint"" }},
          ""404"": {{ ""description"": ""Blueprint not found"" }},
          ""503"": {{ ""description"": ""Session not ready"" }}
        }}
      }}
    }},
    ""/api/v1/spawn-tests"": {{
      ""post"": {{
        ""summary"": ""Spawn all 3 test grids"",
        ""responses"": {{ ""200"": {{ ""description"": ""Spawn results"" }} }}
      }}
    }}
  }}
}}";
}
