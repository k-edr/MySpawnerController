using System.Net;
using System.Text.RegularExpressions;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Models;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Registers PB/LCD convenience endpoints for integration testing.
///   PUT  /api/v1/grids/{id}/script  — upload PB code
///   POST /api/v1/grids/{id}/run     — run PB with argument
///   POST /api/v1/grids/{id}/runlcd  — run PB, wait 100 ms, read LCD surface
///   GET  /api/v1/grids/{id}/lcd     — read LCD content
/// </summary>
public static class PbTestHandler
{
    public static void Register(Router router, ISpawnService spawn)
    {
        // PUT /api/v1/grids/{id}/script
        router.Map<UploadCodeRequest>(
            "api/v1/grids/{id}/script", "PUT",
            (m, body) =>
        {
            if (body?.Code == null)
                return RouteResult.BadRequest("Missing required field: code");

            if (!long.TryParse(m.Groups["id"].Value, out long gridId))
                return RouteResult.BadRequest("Invalid grid ID");
            bool ok = spawn.UploadScript(gridId, body.Code);
            return ok
                ? RouteResult.Ok(new { success = true, length = body.Code.Length })
                : RouteResult.NotFound("No programmable block found on grid");
        });

        // POST /api/v1/grids/{id}/run
        router.Map<RunScriptRequest>(
            "api/v1/grids/{id}/run", "POST",
            (m, body) =>
        {
            if (!long.TryParse(m.Groups["id"].Value, out long gridId))
                return RouteResult.BadRequest("Invalid grid ID");
            string arg = body?.Argument ?? "";
            var result = spawn.RunScript(gridId, arg);
            return result.Success
                ? RouteResult.Ok(new { echo = result.Echo, success = true, argument = arg })
                : RouteResult.NotFound("No programmable block found or run failed");
        });

        // POST /api/v1/grids/{id}/runlcd
        router.Map<RunScriptRequest>(
            "api/v1/grids/{id}/runlcd", "POST",
            (m, body) =>
        {
            if (!long.TryParse(m.Groups["id"].Value, out long gridId))
                return RouteResult.BadRequest("Invalid grid ID");
            string arg = body?.Argument ?? "";
            var result = spawn.RunScriptLcd(gridId, arg);
            return result.Success
                ? RouteResult.Ok(new { echo = result.Echo, output = result.Output, success = true, argument = arg })
                : RouteResult.NotFound("No programmable block found or run failed");
        });

        // GET /api/v1/grids/{id}/lcd
        router.Map("api/v1/grids/{id}/lcd", "GET", (ctx, m) =>
        {
            if (!long.TryParse(m.Groups["id"].Value, out long gridId))
            {
                HttpResponseHelper.Json(ctx, 400, new { error = "Invalid grid ID" }, null);
                return;
            }
            var content = spawn.GetLcdContent(gridId);
            HttpResponseHelper.Json(ctx, 200, new { content }, null);
        });
    }
}
