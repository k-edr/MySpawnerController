using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Result returned by a route handler.
/// </summary>
public sealed class RouteResult
{
    public int StatusCode { get; set; }
    public object Body { get; set; }

    public static RouteResult Ok(object body = null) =>
        new() { StatusCode = 200, Body = body };

    public static RouteResult NotFound(string message = null) =>
        new() { StatusCode = 404, Body = new { error = message ?? "Not found" } };

    public static RouteResult BadRequest(string message) =>
        new() { StatusCode = 400, Body = new { error = message } };

    public static RouteResult Error(string message) =>
        new() { StatusCode = 500, Body = new { error = message } };
}

/// <summary>
/// Delegate for route handlers. Receives the HTTP context and matched route parameters.
/// </summary>
public delegate void RouteHandler(HttpListenerContext ctx, Match routeMatch);

/// <summary>
/// Delegate for typed route handlers. The body is deserialized automatically.
/// Handler returns <see cref="RouteResult"/>; router writes the response.
/// </summary>
public delegate RouteResult TypedHandler<in TBody>(Match routeMatch, TBody body);

/// <summary>
/// Unified HTTP request router. Supports:
/// - Exact path matching with named parameters ({id}, {x}, etc.)
/// - Multiple HTTP methods per route
/// - Route grouping by prefix for extensibility
/// </summary>
public sealed class Router
{
    private readonly List<RouteEntry> _entries = new();

    /// <summary>
    /// Register a single route.
    /// </summary>
    /// <param name="path">Path with optional parameters: "grids/{id}/blocks/{x}/{y}/{z}"</param>
    /// <param name="methods">Comma-separated HTTP methods: "GET,POST"</param>
    /// <param name="handler">Handler delegate</param>
    public void Map(string path, string methods, RouteHandler handler)
    {
        _entries.Add(new RouteEntry
        {
            Regex = BuildRegex(path),
            Methods = new HashSet<string>(methods.Split(','), StringComparer.OrdinalIgnoreCase),
            Handler = handler
        });
    }

    /// <summary>
    /// Register a typed route: body is auto-deserialized from JSON,
    /// handler returns <see cref="RouteResult"/>, response is auto-written.
    /// </summary>
    public void Map<TBody>(string path, string methods, TypedHandler<TBody> handler)
        where TBody : class
    {
        Map(path, methods, (ctx, m) =>
        {
            var body = HttpResponseHelper.ReadBody<TBody>(ctx);
            var result = handler(m, body);
            HttpResponseHelper.Json(ctx, result.StatusCode, result.Body, HttpResponseHelper.CorsOrigin);
        });
    }

    /// <summary>
    /// Create a route group builder with a common path prefix.
    /// </summary>
    public RouteGroupBuilder MapGroup(string prefix)
    {
        return new RouteGroupBuilder(this, prefix);
    }

    /// <summary>
    /// Try to dispatch an incoming request. Returns false if no route matched.
    /// </summary>
    public bool TryDispatch(HttpListenerContext ctx, string path)
    {
        var method = ctx.Request.HttpMethod;
        foreach (var entry in _entries)
        {
            if (!entry.Methods.Contains(method))
                continue;

            var match = entry.Regex.Match(path);
            if (!match.Success)
                continue;

            entry.Handler(ctx, match);
            return true;
        }

        return false;
    }

    private static Regex BuildRegex(string path)
    {
        // "grids/{id}/blocks/{x}/{y}/{z}" → ^grids/(?<id>[^/]+)/blocks/(?<x>[^/]+)/(?<y>[^/]+)/(?<z>[^/]+)$
        // Single-regex replace: {name} → (?<name>[^/]+)
        // Avoids Regex.Escape which doesn't escape } in .NET Framework, causing invalid group syntax.
        var pattern = "^" + Regex.Replace(
            Regex.Escape(path.Trim('/')),
            @"\\\{(.+?)\}",
            @"(?<${1}>[^/]+)") + "$";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private sealed class RouteEntry
    {
        public Regex Regex;
        public HashSet<string> Methods;
        public RouteHandler Handler;
    }
}

/// <summary>
/// Builder for routes sharing a common path prefix.
/// </summary>
public sealed class RouteGroupBuilder
{
    private readonly Router _router;
    private readonly string _prefix;

    internal RouteGroupBuilder(Router router, string prefix)
    {
        _router = router;
        _prefix = prefix.Trim('/');
    }

    /// <summary>
    /// Register a GET route under the group's prefix.
    /// </summary>
    public RouteGroupBuilder Get(string path, RouteHandler handler) =>
        Map("GET", path, handler);

    /// <summary>
    /// Register a POST route under the group's prefix.
    /// </summary>
    public RouteGroupBuilder Post(string path, RouteHandler handler) =>
        Map("POST", path, handler);

    /// <summary>
    /// Register a route accepting multiple methods under the group's prefix.
    /// </summary>
    public RouteGroupBuilder Map(string methods, string path, RouteHandler handler)
    {
        var fullPath = string.IsNullOrEmpty(path)
            ? _prefix
            : _prefix + "/" + path.Trim('/');
        _router.Map(fullPath, methods, handler);
        return this;
    }

    /// <summary>
    /// Register a typed POST route under the group's prefix.
    /// Body is auto-deserialized from JSON.
    /// </summary>
    public RouteGroupBuilder Post<TBody>(string path, TypedHandler<TBody> handler)
        where TBody : class
    {
        var fullPath = string.IsNullOrEmpty(path)
            ? _prefix
            : _prefix + "/" + path.Trim('/');
        _router.Map(fullPath, "POST", handler);
        return this;
    }

    /// <summary>
    /// Register a typed route under the group's prefix.
    /// Body is auto-deserialized from JSON.
    /// </summary>
    public RouteGroupBuilder Map<TBody>(string methods, string path, TypedHandler<TBody> handler)
        where TBody : class
    {
        var fullPath = string.IsNullOrEmpty(path)
            ? _prefix
            : _prefix + "/" + path.Trim('/');
        _router.Map(fullPath, methods, handler);
        return this;
    }
}
