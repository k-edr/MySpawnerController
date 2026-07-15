using System.IO;
using System.Net;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Configuration;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Registers spawn endpoints:
///   POST /api/v1/spawn       — spawn a grid from blueprint
///   POST /api/v1/spawn-tests — spawn all test grids
/// </summary>
public static class SpawnHandler
{
    public static void Register(Router router, ISpawnService spawn, AppConfig config)
    {
        var orchestrator = new SpawnOrchestrator(spawn, config.BlueprintsFolder);

        router.Map("api/v1/spawn", "POST", (ctx, m) =>
        {
            using var reader = new StreamReader(ctx.Request.InputStream, System.Text.Encoding.UTF8);
            var result = orchestrator.SpawnFromJson(reader.ReadToEnd());
            HttpResponseHelper.Json(ctx, result.StatusCode, result.Body, null);
        });

        router.Map("api/v1/spawn-tests", "POST", (ctx, m) =>
        {
            var result = orchestrator.SpawnTestGrids();
            HttpResponseHelper.Json(ctx, result.StatusCode, result.Body, null);
        });
    }
}
