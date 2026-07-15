using System.IO;
using System.Text.Json;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using VRageMath;

namespace GridSpawner.Api.Application;

public sealed class SpawnOrchestrator
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ISpawnService _spawnService;
    private readonly string _blueprintsFolder;

    public SpawnOrchestrator(ISpawnService spawnService, string blueprintsFolderOverride = null)
    {
        _spawnService = spawnService;
        _blueprintsFolder = blueprintsFolderOverride ?? AppDefaults.DefaultBlueprintsFolder;
    }

    public SpawnResult SpawnFromJson(string json)
    {
        var req = DeserializeRequest(json);
        if (req == null) return SpawnResult.BadRequest("Missing required field: blueprint");

        if (!_spawnService.IsReady)
            return SpawnResult.NotReady();

        string bpPath = ResolveBlueprintPath(req.Blueprint);
        if (!File.Exists(bpPath))
            return SpawnResult.NotFound($"Blueprint not found: {req.Blueprint}");

        return DoSpawn(req, bpPath);
    }

    public SpawnResult SpawnTestGrids()
    {
        if (!_spawnService.IsReady)
            return SpawnResult.NotReady();

        var spawner = new TestGridSpawner(_spawnService, _blueprintsFolder);
        var grids = spawner.SpawnAll();
        return SpawnResult.Ok(new SpawnResponse { Grids = grids });
    }

    // ── Private helpers ──

    private SpawnRequest DeserializeRequest(string json)
    {
        try { return JsonSerializer.Deserialize<SpawnRequest>(json, JsonOpts); }
        catch (JsonException) { return null; }
    }

    private string ResolveBlueprintPath(string blueprintName) =>
        Path.Combine(_blueprintsFolder, blueprintName, AppDefaults.BlueprintExtension);

    private SpawnResult DoSpawn(SpawnRequest req, string bpPath)
    {
        var pos = req.Position ?? new SpawnPosition();
        var grids = _spawnService.Spawn(req.Blueprint, bpPath,
            new Vector3D(pos.X, pos.Y, pos.Z), req.DisplayName);

        if (grids == null)
            return SpawnResult.Error($"Failed to spawn: {req.Blueprint}");

        return SpawnResult.Ok(new SpawnResponse { Grids = grids });
    }
}
