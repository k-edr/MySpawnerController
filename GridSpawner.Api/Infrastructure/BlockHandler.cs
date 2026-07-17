using System;
using System.Net;
using System.Text.RegularExpressions;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Models;

namespace GridSpawner.Api.Infrastructure;

/// <summary>
/// Registers terminal block interaction endpoints under /api/v1/grids/{id}/blocks.
///   GET  .../blocks                                 — list functional blocks
///   GET  .../blocks/{x}/{y}/{z}                     — block detail
///   POST .../blocks/{x}/{y}/{z}/action              — execute terminal action
///   PUT  .../blocks/{x}/{y}/{z}/program             — set PB code
///   PUT  .../blocks/{x}/{y}/{z}/text                — write text to LCD
///   POST .../blocks/{x}/{y}/{z}/run                 — run PB with argument
///   GET  .../blocks/{x}/{y}/{z}/properties/{propId} — get property
///   PUT  .../blocks/{x}/{y}/{z}/properties/{propId} — set property
/// </summary>
public static class BlockHandler
{
    public static void Register(Router router, ISpawnService spawn)
    {
        // ── No-body endpoints ─────────────────────────

        router.Map("api/v1/grids/{id}/blocks", "GET", (ctx, m) =>
        {
            if (!long.TryParse(m.Groups["id"].Value, out long id))
            {
                HttpResponseHelper.Json(ctx, 400,
                    new { error = "Invalid grid ID" }, null);
                return;
            }
            HttpResponseHelper.Json(ctx, 200, spawn.GetGridBlocks(id), null);
        });

        router.Map("api/v1/grids/{id}/blocks/{x}/{y}/{z}", "GET", (ctx, m) =>
        {
            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
            {
                HttpResponseHelper.Json(ctx, 400,
                    new { error = "Invalid block coordinates" }, null);
                return;
            }
            var block = spawn.GetBlockDetail(gridId, x, y, z);
            if (block == null)
                HttpResponseHelper.Json(ctx, 404,
                    new { error = $"Block not found at ({x},{y},{z}) on grid {gridId}" }, null);
            else
                HttpResponseHelper.Json(ctx, 200, block, null);
        });

        // GET /api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}
        router.Map("api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}", "GET",
            (ctx, m) =>
        {
            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
            {
                HttpResponseHelper.Json(ctx, 400,
                    new { error = "Invalid block coordinates" }, null);
                return;
            }
            string propId = Uri.UnescapeDataString(m.Groups["propId"].Value);
            var val = spawn.GetBlockProperty(gridId, x, y, z, propId);
            if (val == null)
                HttpResponseHelper.Json(ctx, 404,
                    new { error = $"Property '{propId}' not found" }, null);
            else
                HttpResponseHelper.Json(ctx, 200,
                    new { propertyId = propId, value = val }, null);
        });

        // ── Body-aware endpoints (typed) ──────────────

        // POST action
        router.Map<BlockActionRequest>(
            "api/v1/grids/{id}/blocks/{x}/{y}/{z}/action", "POST",
            (m, req) =>
        {
            if (req?.ActionId == null)
                return RouteResult.BadRequest("Missing required field: actionId");

            var (gridId, x, y, z) = ParseBlockCoords(m);
            bool ok = spawn.ExecuteBlockAction(gridId, x, y, z, req.ActionId);
            return ok
                ? RouteResult.Ok(new { success = true, action = req.ActionId })
                : RouteResult.NotFound($"Action '{req.ActionId}' failed");
        });

        // PUT program
        router.Map<UploadCodeRequest>(
            "api/v1/grids/{id}/blocks/{x}/{y}/{z}/program", "PUT",
            (m, body) =>
        {
            if (body?.Code == null)
                return RouteResult.BadRequest("Missing required field: code");

            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
                return RouteResult.BadRequest("Invalid block coordinates");
            bool ok = spawn.SetProgramCode(gridId, x, y, z, body.Code);
            return ok
                ? RouteResult.Ok(new { success = true, length = body.Code.Length })
                : RouteResult.NotFound("Block is not a programmable block");
        });

        // PUT text
        router.Map<WriteTextRequest>(
            "api/v1/grids/{id}/blocks/{x}/{y}/{z}/text", "PUT",
            (m, body) =>
        {
            if (body?.Text == null)
                return RouteResult.BadRequest("Missing required field: text");

            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
                return RouteResult.BadRequest("Invalid block coordinates");
            bool ok = spawn.WriteTextPanel(gridId, x, y, z, body.Text);
            return ok
                ? RouteResult.Ok(new { success = true, length = body.Text.Length })
                : RouteResult.NotFound("Block is not a text panel");
        });

        // POST run
        router.Map<RunScriptRequest>(
            "api/v1/grids/{id}/blocks/{x}/{y}/{z}/run", "POST",
            (m, body) =>
        {
            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
                return RouteResult.BadRequest("Invalid block coordinates");
            string arg = body?.Argument ?? "";
            bool ok = spawn.RunProgram(gridId, x, y, z, arg);
            return ok
                ? RouteResult.Ok(new { success = true, argument = arg })
                : RouteResult.NotFound("Block is not a programmable block");
        });

        // PUT property
        router.Map<SetPropertyRequest>(
            "api/v1/grids/{id}/blocks/{x}/{y}/{z}/properties/{propId}", "PUT",
            (m, body) =>
        {
            if (body?.Value == null)
                return RouteResult.BadRequest("Missing required field: value");

            if (!TryParseBlockCoords(m, out var gridId, out var x, out var y, out var z))
                return RouteResult.BadRequest("Invalid block coordinates");
            string propId = Uri.UnescapeDataString(m.Groups["propId"].Value);
            bool ok = spawn.SetBlockProperty(gridId, x, y, z, propId, body.Value);
            return ok
                ? RouteResult.Ok(new { propertyId = propId, value = body.Value, success = true })
                : RouteResult.NotFound($"Failed to set property '{propId}'");
        });
    }

    private static bool TryParseBlockCoords(Match m, out long gridId, out int x, out int y, out int z)
    {
        gridId = 0; x = 0; y = 0; z = 0;
        return long.TryParse(m.Groups["id"].Value, out gridId)
            && int.TryParse(m.Groups["x"].Value, out x)
            && int.TryParse(m.Groups["y"].Value, out y)
            && int.TryParse(m.Groups["z"].Value, out z);
    }

    private static (long gridId, int x, int y, int z) ParseBlockCoords(Match m) =>
        (
            long.Parse(m.Groups["id"].Value),
            int.Parse(m.Groups["x"].Value),
            int.Parse(m.Groups["y"].Value),
            int.Parse(m.Groups["z"].Value)
        );
}
