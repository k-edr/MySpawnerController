using System;
using System.Net;
using System.Threading;
using GridSpawner.Shared.Configuration;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// HTTP API server listening on <see cref="AppConfig.ApiPort"/>.
/// Generic — knows nothing about specific routes.
/// </summary>
public sealed class ApiServer : IDisposable
{
    private HttpListener _listener;
    private readonly AppConfig _config;
    private readonly Action<string> _log;
    private readonly Router _router;
    private Thread _thread;
    private volatile bool _running;

    public ApiServer(AppConfig config, Action<string> log = null)
    {
        _config = config;
        _log = log ?? (_ => { });
        _listener = new HttpListener();
        _listener.Prefixes.Add($"{config.ApiScheme}://{config.ApiHost}:{config.ApiPort}/");
        _router = new Router();
    }

    /// <summary>
    /// Expose the router so the composition root can register handlers.
    /// </summary>
    public Router Router => _router;

    public void Start()
    {
        HttpResponseHelper.Log = _log;

        try
        {
            _listener.Start();
            _log($"[ApiServer] Listening on port {_config.ApiPort}");
        }
        catch (HttpListenerException)
        {
            _log($"[ApiServer] Port {_config.ApiPort} already registered, retrying...");

            try
            {
                _listener.Close();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"{_config.ApiScheme}://{_config.ApiHost}:{_config.ApiPort}/");
                _listener.Start();
                _log($"[ApiServer] Listening on port {_config.ApiPort}");
            }
            catch (Exception ex)
            {
                _log($"[ApiServer] Cannot start listener: {ex.Message}. API disabled.");
                return;
            }
        }

        _running = true;
        _thread = new Thread(Listen) { IsBackground = true, Name = "ApiServer" };
        _thread.Start();
    }

    public void Dispose()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
    }

    private void Listen()
    {
        while (_running)
        {
            try
            {
                var ctx = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
            }
            catch (HttpListenerException) { break; }
            catch (Exception ex) when (_running) { _log("[ApiServer] " + ex.Message); }
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        try
        {
            string method = ctx.Request.HttpMethod;
            string path = ctx.Request.Url.AbsolutePath.Trim('/');
            _log($"[ApiServer] {method} /{path}");

            // CORS preflight
            if (method == "OPTIONS")
            {
                HttpResponseHelper.Text(ctx, 204, "", _config.SwaggerCorsOrigin);
                return;
            }

            // Dispatch
            if (_router.TryDispatch(ctx, path))
                return;

            // 405
            HttpResponseHelper.Json(ctx, 405,
                new { error = "Method Not Allowed" }, _config.SwaggerCorsOrigin);
        }
        catch (Exception ex)
        {
            _log("[ApiServer] ERROR: " + ex);
            try { HttpResponseHelper.Json(ctx, 500, new { error = ex.Message }, _config.SwaggerCorsOrigin); }
            catch { }
        }
    }
}
