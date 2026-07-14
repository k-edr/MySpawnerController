using System.Collections.Generic;
using MySpawnerController.Shared;
using VRageMath;

namespace MySpawnerController.Api.Application;

/// <summary>
/// Service contract for grid spawning and management.
/// Implemented by the game plugin on the main thread.
/// </summary>
public interface ISpawnService
{
    List<GridDto> Spawn(string blueprintName, string blueprintPath,
        Vector3D position, string displayName);

    List<BlueprintInfo> ListBlueprints(string blueprintsFolder);

    List<GridListItem> ListGrids();

    GridDto GetGrid(long id);

    bool DeleteGrid(long id);

    bool IsReady { get; }
}
