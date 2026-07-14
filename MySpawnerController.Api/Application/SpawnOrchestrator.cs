using System.IO;
using System.Text.Json;
using MySpawnerController.Shared;
using VRageMath;

namespace MySpawnerController.Api.Application;

/// <summary>
/// Orchestrates spawn requests: validates input, resolves blueprint paths,
/// delegates to <see cref="ISpawnService"/>.
/// </summary>
public sealed class SpawnOrchestrator
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ISpawnService _spawnService;
    private readonly string _blueprintsFolder;

    public SpawnOrchestrator(ISpawnService spawnService)
    {
        _spawnService = spawnService;
        _blueprintsFolder = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "SpaceEngineers", "Blueprints", "local");
    }

    public SpawnResult SpawnFromJson(string json)
    {
        SpawnRequest req;
        try { req = JsonSerializer.Deserialize<SpawnRequest>(json, JsonOpts); }
        catch (JsonException ex) { return SpawnResult.BadRequest($"Invalid JSON: {ex.Message}"); }

        if (req == null || string.IsNullOrWhiteSpace(req.Blueprint))
            return SpawnResult.BadRequest("Missing required field: blueprint");

        if (!_spawnService.IsReady)
            return SpawnResult.NotReady();

        string bpPath = Path.Combine(_blueprintsFolder, req.Blueprint, "bp.sbc");
        if (!File.Exists(bpPath))
            return SpawnResult.NotFound($"Blueprint not found: {req.Blueprint}");

        var pos = req.Position ?? new SpawnPosition();
        var grids = _spawnService.Spawn(req.Blueprint, bpPath,
            new Vector3D(pos.X, pos.Y, pos.Z), req.DisplayName);

        if (grids == null)
            return SpawnResult.Error($"Failed to spawn: {req.Blueprint}");

        return SpawnResult.Ok(new SpawnResponse { Grids = grids });
    }

    public SpawnResult SpawnTestGrids()
    {
        if (!_spawnService.IsReady)
            return SpawnResult.NotReady();

        var tests = new (string name, Vector3D offset)[]
        {
            ("TestGrid_MultiConnectorGrid", new Vector3D(0, 0, 0)),
            ("TestGrid_PBWithPanel",        new Vector3D(0, 0, 10)),
            ("TestGrid_SingleConnector",    new Vector3D(0, 0, -10)),
        };

        var allGrids = new SpawnResponse();
        foreach (var (name, offset) in tests)
        {
            string bpPath = Path.Combine(_blueprintsFolder, name, "bp.sbc");
            if (!File.Exists(bpPath)) continue;
            var grids = _spawnService.Spawn(name, bpPath, offset, null);
            if (grids != null) allGrids.Grids.AddRange(grids);
        }

        return SpawnResult.Ok(allGrids);
    }
}

