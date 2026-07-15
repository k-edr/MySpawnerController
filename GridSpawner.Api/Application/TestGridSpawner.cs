using System.Collections.Generic;
using System.IO;
using GridSpawner.Shared.Models;
using VRageMath;

namespace GridSpawner.Api.Application;

/// <summary>
/// Spawns predefined test grids. Decoupled from <see cref="SpawnOrchestrator"/>.
/// Blueprints must exist in the configured folder.
/// </summary>
public sealed class TestGridSpawner
{
    private readonly ISpawnService _spawn;
    private readonly string _blueprintsFolder;

    /// <summary>
    /// Test grid definitions: blueprint name → world offset.
    /// Keep in sync with the blueprints stored under /TestBlueprints/.
    /// </summary>
    public static readonly (string Name, Vector3D Offset)[] TestGrids =
    {
        ("TestGrid_MultiConnectorGrid", new Vector3D(0, 0, 0)),
        ("TestGrid_PBWithPanel",        new Vector3D(0, 0, 10)),
        ("TestGrid_SingleConnector",    new Vector3D(0, 0, -10)),
    };

    public TestGridSpawner(ISpawnService spawn, string blueprintsFolder)
    {
        _spawn = spawn;
        _blueprintsFolder = blueprintsFolder;
    }

    /// <summary>
    /// Spawn all test grids. Missing blueprints are silently skipped.
    /// </summary>
    public IReadOnlyList<GridDto> SpawnAll()
    {
        var all = new List<GridDto>();

        foreach (var (name, offset) in TestGrids)
        {
            string bpPath = Path.Combine(_blueprintsFolder, name, "bp.sbc");
            if (!File.Exists(bpPath))
                continue;

            var grids = _spawn.Spawn(name, bpPath, offset, null);
            if (grids != null)
                all.AddRange(grids);
        }

        return all;
    }
}
