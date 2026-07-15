using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using GridSpawner.Shared.Configuration;
using GridSpawner.Swagger.Application;
using Swashbuckle.AspNetCore.Swagger;

namespace GridSpawner.Swagger.Infrastructure;

public static class SwaggerHostExtensions
{
    public static void AddSwaggerDocument(this IServiceCollection services, OpenApiDocument doc)
    {
        services.AddSingleton<ISwaggerProvider>(new GameApiSwaggerProvider(doc));
    }

    public static void ConfigureSwagger(this WebApplication app, AppConfig config)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.DocumentTitle = "GridSpawner.Plugin API";
            c.InjectJavascript("/swagger-ui/connection-status.js");
        });

        app.MapGet("/", () => Results.Redirect("/swagger"));
        app.MapGet("/favicon.ico", () => Results.StatusCode(204));
        app.MapGet("/swagger-ui/connection-status.js", () =>
            Results.Content(ConnectionStatusScript(config),
                "application/javascript; charset=utf-8"));
    }

    public static void PrintBanner(AppConfig config)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("============================================");
        Console.WriteLine(" GridSpawner.Plugin Swagger UI");
        Console.WriteLine("============================================");
        Console.ResetColor();
        Console.WriteLine($"  Swagger : {config.ApiScheme}://{config.DisplayHost}:{config.SwaggerPort}/swagger");
        Console.WriteLine($"  Game API : {config.ApiScheme}://{config.DisplayHost}:{config.ApiPort}");
        Console.WriteLine();
        Console.WriteLine("  Press Ctrl+C to stop (or close this window).");
        Console.WriteLine();
    }

    private static string ConnectionStatusScript(AppConfig config) => $@"(function() {{
  var style = document.createElement('style');
  style.textContent = `
    .connection-status {{
      position: fixed; top: 10px; right: 20px; z-index: 9999;
      padding: 6px 14px; border-radius: 20px; font-family: sans-serif;
      font-size: 13px; font-weight: 600; color: #fff;
    }}
    .connected {{ background: #2e7d32; }}
    .loading {{ background: #e65100; }}
    .disconnected {{ background: #c62828; }}
  `;
  document.head.appendChild(style);

  var dot = document.createElement('div');
  dot.id = 'connection-status';
  dot.className = 'connection-status loading';
  dot.textContent = 'Game: checking...';
  document.body.insertBefore(dot, document.body.firstChild);

  fetch('{config.ApiScheme}://{config.DisplayHost}:{config.ApiPort}/api/v1/health')
    .then(r => r.json())
    .then(d => {{
      dot.textContent = d.ready ? 'Game: connected' : 'Game: loading...';
      dot.className = 'connection-status ' + (d.ready ? 'connected' : 'loading');
    }})
    .catch(() => {{
      dot.textContent = 'Game: not connected';
      dot.className = 'connection-status disconnected';
    }});
}})();";
}
