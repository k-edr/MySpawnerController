using System.Net;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Configuration;

namespace GridSpawner.Api.Infrastructure;

public static class BlueprintHandler
{
    public static void Register(Router router, ISpawnService spawn)
    {
        router.Map("api/v1/blueprints", "GET", (ctx, m) =>
        {
            HttpResponseHelper.Json(ctx, 200,
                spawn.ListBlueprints(AppDefaults.DefaultBlueprintsFolder), null);
        });
    }
}
