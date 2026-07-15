using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using GridSpawner.Api.Application;
using GridSpawner.Shared;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// HTTP API server. Routes requests to handlers and delegates
/// spawn logic to <see cref="SpawnOrchestrator"/>.
/// </summary>
public sealed class ApiServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly ISpawnService _spawnService;
    private readonly SpawnOrchestrator _orchestrator;
    private readonly AppConfig _config;
    private readonly Action<string> _log;
    private Thread _thread;
    private volatile bool _running;

    public ApiServer(AppConfig config, ISpawnService spawnService, Action<string> log = null)
    {
        _config = config;
        _spawnService = spawnService;
        _orchestrator = new SpawnOrchestrator(spawnService, config.BlueprintsFolder);
        _log = log ?? (_ => { });
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{config.ApiPort}/");
    }

    public void Start()
    {
        _listener.Start();
        _running = true;
        _thread = new Thread(Listen) { IsBackground = true, Name = "ApiServer" };
        _thread.Start();
        _log($"[ApiServer] Listening on port {_config.ApiPort}");
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

            if (method == "OPTIONS")
            {
                HttpResponseHelper.Text(ctx, 204, "", _config.SwaggerCorsOrigin);
                return;
            }

            switch (path)
            {
                case "api/v1/health" when method == "GET":
                    HttpResponseHelper.Json(ctx, 200, new HealthResponse { Ready = _spawnService.IsReady }, _config.SwaggerCorsOrigin);
                    return;

                case "api/v1/blueprints" when method == "GET":
                    var blueprintsFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SpaceEngineers", "Blueprints", "local");
                    HttpResponseHelper.Json(ctx, 200, _spawnService.ListBlueprints(blueprintsFolder), _config.SwaggerCorsOrigin);
                    return;

                case "api/v1/grids" when method == "GET":
                    HttpResponseHelper.Json(ctx, 200, _spawnService.ListGrids(), _config.SwaggerCorsOrigin);
                    return;

                case "api/v1/spawn" when method == "POST":
                    {
                        using var reader = new StreamReader(ctx.Request.InputStream, System.Text.Encoding.UTF8);
                        var result = _orchestrator.SpawnFromJson(reader.ReadToEnd());
                        HttpResponseHelper.Json(ctx, result.StatusCode, result.Body, _config.SwaggerCorsOrigin);
                    }
                    return;

                case "api/v1/spawn-tests" when method == "POST":
                    {
                        var result = _orchestrator.SpawnTestGrids();
                        HttpResponseHelper.Json(ctx, result.StatusCode, result.Body, _config.SwaggerCorsOrigin);
                    }
                    return;

                default:
                    {
                        var match = Regex.Match(path, @"^api/v1/grids/(\d+)$");
                        if (match.Success)
                        {
                            long id = long.Parse(match.Groups[1].Value);
                            HandleGridById(ctx, method, id);
                            return;
                        }
                    }
                    break;
            }

            HttpResponseHelper.Json(ctx, 405, new ErrorResponse { Error = "Method Not Allowed" }, _config.SwaggerCorsOrigin);
        }
        catch (Exception ex)
        {
            _log("[ApiServer] ERROR: " + ex);
            try { HttpResponseHelper.Json(ctx, 500, new ErrorResponse { Error = ex.Message }, _config.SwaggerCorsOrigin); } catch { }
        }
    }

    private void HandleGridById(HttpListenerContext ctx, string method, long id)
    {
        switch (method)
        {
            case "GET":
                var grid = _spawnService.GetGrid(id);
                if (grid == null)
                    HttpResponseHelper.Json(ctx, 404, new ErrorResponse { Error = $"Grid {id} not found" }, _config.SwaggerCorsOrigin);
                else
                    HttpResponseHelper.Json(ctx, 200, grid, _config.SwaggerCorsOrigin);
                return;

            case "DELETE":
                bool ok = _spawnService.DeleteGrid(id);
                if (!ok)
                    HttpResponseHelper.Json(ctx, 404, new ErrorResponse { Error = $"Grid {id} not found" }, _config.SwaggerCorsOrigin);
                else
                    HttpResponseHelper.Json(ctx, 200, new { deleted = id }, _config.SwaggerCorsOrigin);
                return;

            default:
                HttpResponseHelper.Text(ctx, 405, "Method Not Allowed", _config.SwaggerCorsOrigin);
                return;
        }
    }
}
