using System.Net;
using GridSpawner.Api.Application;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Registers health check endpoint: GET /api/v1/health
/// </summary>
public static class HealthHandler
{
    public static void Register(Router router, ISpawnService spawn)
    {
        router.Map("api/v1/health", "GET", (ctx, m) =>
        {
            var ready = spawn.IsReady;
            HttpResponseHelper.Json(ctx, 200, new { ready }, null);
        });
    }
}
