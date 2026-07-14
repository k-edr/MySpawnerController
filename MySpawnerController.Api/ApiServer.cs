using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using VRageMath;

namespace MySpawnerController.Api
{
    public sealed class ApiServer : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly HttpListener _listener;
        private readonly ISpawnService _spawnService;
        private readonly Action<string> _log;
        private readonly int _port;
        private Thread _thread;
        private volatile bool _running;

        public ApiServer(int port, ISpawnService spawnService, Action<string> log = null)
        {
            _spawnService = spawnService;
            _log = log ?? (_ => { });
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            _listener.Start();
            _running = true;
            _thread = new Thread(Listen) { IsBackground = true, Name = "ApiServer" };
            _thread.Start();
            _log("[ApiServer] Listening on http://localhost:" + _port);
        }

        public void Dispose()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            _listener?.Close();
        }

        private void Listen()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Route(ctx));
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex) when (_running) { _log("[ApiServer] " + ex.Message); }
            }
        }

        private void Route(HttpListenerContext ctx)
        {
            try
            {
                string method = ctx.Request.HttpMethod;
                string path = ctx.Request.Url.AbsolutePath.Trim('/');
                _log($"[ApiServer] {method} /{path}");

                // CORS preflight
                if (method == "OPTIONS")
                {
                    Text(ctx, 204, "");
                    return;
                }

                // Health
                if (path == "api/v1/health")
                {
                    if (method != "GET") { Text(ctx, 405, "Method Not Allowed"); return; }
                    Json(ctx, 200, new HealthResponse { Ready = _spawnService.IsReady });
                    return;
                }

                // Blueprints
                if (path == "api/v1/blueprints")
                {
                    if (method != "GET") { Text(ctx, 405, "Method Not Allowed"); return; }
                    var list = _spawnService.ListBlueprints(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "SpaceEngineers", "Blueprints", "local"));
                    Json(ctx, 200, list);
                    return;
                }

                // Grids collection
                if (path == "api/v1/grids")
                {
                    if (method != "GET") { Text(ctx, 405, "Method Not Allowed"); return; }
                    var list = _spawnService.ListGrids();
                    Json(ctx, 200, list);
                    return;
                }

                // Grid by id
                var match = Regex.Match(path, @"^api/v1/grids/(\d+)$");
                if (match.Success)
                {
                    long id = long.Parse(match.Groups[1].Value);
                    if (method == "GET")
                    {
                        var grid = _spawnService.GetGrid(id);
                        if (grid == null) Json(ctx, 404, new ErrorResponse { Error = $"Grid {id} not found" });
                        else Json(ctx, 200, grid);
                    }
                    else if (method == "DELETE")
                    {
                        bool ok = _spawnService.DeleteGrid(id);
                        if (!ok) Json(ctx, 404, new ErrorResponse { Error = $"Grid {id} not found" });
                        else Json(ctx, 200, new { deleted = id });
                    }
                    else Text(ctx, 405, "Method Not Allowed");
                    return;
                }

                // Spawn
                if (path == "api/v1/spawn")
                {
                    if (method != "POST") { Text(ctx, 405, "Method Not Allowed"); return; }
                    HandleSpawn(ctx);
                    return;
                }

                // Spawn tests
                if (path == "api/v1/spawn-tests")
                {
                    if (method != "POST") { Text(ctx, 405, "Method Not Allowed"); return; }
                    HandleSpawnTests(ctx);
                    return;
                }

                // 404
                Json(ctx, 404, new ErrorResponse { Error = "Not Found", Hint = "See /swagger on port 9998 for API docs" });
            }
            catch (Exception ex)
            {
                _log("[ApiServer] ERROR: " + ex);
                try { Json(ctx, 500, new ErrorResponse { Error = ex.Message }); } catch { }
            }
        }

        private void HandleSpawn(HttpListenerContext ctx)
        {
            if (!_spawnService.IsReady)
            {
                Json(ctx, 503, new ErrorResponse { Error = "Session not ready" });
                return;
            }

            SpawnRequest req;
            try
            {
                using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    req = JsonSerializer.Deserialize<SpawnRequest>(reader.ReadToEnd(), JsonOpts);
                }
            }
            catch (JsonException ex)
            {
                Json(ctx, 400, new ErrorResponse { Error = $"Invalid JSON: {ex.Message}" });
                return;
            }

            if (req == null || string.IsNullOrWhiteSpace(req.Blueprint))
            {
                Json(ctx, 400, new ErrorResponse { Error = "Missing required field: blueprint" });
                return;
            }

            string blueprintsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "Blueprints", "local");
            string bpPath = Path.Combine(blueprintsFolder, req.Blueprint, "bp.sbc");

            if (!File.Exists(bpPath))
            {
                Json(ctx, 404, new ErrorResponse { Error = $"Blueprint not found: {req.Blueprint}" });
                return;
            }

            var pos = req.Position ?? new SpawnPosition();
            var grids = _spawnService.Spawn(req.Blueprint, bpPath,
                new Vector3D(pos.X, pos.Y, pos.Z), req.DisplayName);

            if (grids == null)
                Json(ctx, 500, new ErrorResponse { Error = $"Failed to spawn: {req.Blueprint}" });
            else
                Json(ctx, 200, new SpawnResponse { Grids = grids });
        }

        private void HandleSpawnTests(HttpListenerContext ctx)
        {
            if (!_spawnService.IsReady)
            {
                Json(ctx, 503, new ErrorResponse { Error = "Session not ready" });
                return;
            }

            string blueprintsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "Blueprints", "local");

            var tests = new (string name, Vector3D offset)[]
            {
                ("TestGrid_MultiConnectorGrid", new Vector3D(0, 0, 0)),
                ("TestGrid_PBWithPanel",        new Vector3D(0, 0, 10)),
                ("TestGrid_SingleConnector",    new Vector3D(0, 0, -10)),
            };

            var allGrids = new SpawnResponse();
            foreach (var (name, offset) in tests)
            {
                string bpPath = Path.Combine(blueprintsFolder, name, "bp.sbc");
                if (!File.Exists(bpPath)) continue;
                var grids = _spawnService.Spawn(name, bpPath, offset, null);
                if (grids != null) allGrids.Grids.AddRange(grids);
            }

            Json(ctx, 200, allGrids);
        }

        // ── Response helpers ───────────────────────────────────────

        private static void Json<T>(HttpListenerContext ctx, int code, T obj)
        {
            var json = JsonSerializer.Serialize(obj, JsonOpts);
            Respond(ctx, code, json, "application/json");
        }

        private static void Text(HttpListenerContext ctx, int code, string body)
        {
            Respond(ctx, code, body, "text/plain; charset=utf-8");
        }

        private static void Respond(HttpListenerContext ctx, int code, string body, string contentType)
        {
            try
            {
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "http://localhost:9998");
                ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
                ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                byte[] data = Encoding.UTF8.GetBytes(body);
                ctx.Response.StatusCode = code;
                ctx.Response.ContentType = contentType;
                ctx.Response.ContentLength64 = data.Length;
                ctx.Response.OutputStream.Write(data, 0, data.Length);
                ctx.Response.OutputStream.Close();
            }
            catch { }
        }
    }
}
