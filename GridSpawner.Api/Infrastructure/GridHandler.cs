using System.Net;
using System.Text.RegularExpressions;
using GridSpawner.Api.Application;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Registers grid-level endpoints under /api/v1/grids.
///   GET  /api/v1/grids        — list all spawned grids
///   GET  /api/v1/grids/{id}   — get grid details
///   DELETE /api/v1/grids/{id} — delete a grid
/// </summary>
public static class GridHandler
{
    public static void Register(Router router, ISpawnService spawn)
    {
        // GET /api/v1/grids
        router.Map("api/v1/grids", "GET", (ctx, m) =>
        {
            HttpResponseHelper.Json(ctx, 200, spawn.ListGrids(), null);
        });

        // DELETE /api/v1/grids — delete all
        router.Map("api/v1/grids", "DELETE", (ctx, m) =>
        {
            var deleted = spawn.DeleteAllGrids();
            HttpResponseHelper.Json(ctx, 200, new { deleted }, null);
        });

        // GET + DELETE /api/v1/grids/{id}
        router.Map("api/v1/grids/{id}", "GET,DELETE", (ctx, m) =>
        {
            long id = long.Parse(m.Groups["id"].Value);
            string method = ctx.Request.HttpMethod;

            if (method == "GET")
            {
                var grid = spawn.GetGrid(id);
                if (grid == null)
                    HttpResponseHelper.Json(ctx, 404, new { error = $"Grid {id} not found" }, null);
                else
                    HttpResponseHelper.Json(ctx, 200, grid, null);
            }
            else // DELETE
            {
                bool ok = spawn.DeleteGrid(id);
                HttpResponseHelper.Json(ctx, ok ? 200 : 404,
                    ok ? new { deleted = id } : new { error = $"Grid {id} not found" }, null);
            }
        });
    }
}
