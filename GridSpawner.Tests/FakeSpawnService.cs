using System.Collections.Generic;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Models;
using VRageMath;

namespace GridSpawner.Tests
{
    /// <summary>
    /// Fake implementation of <see cref="ISpawnService"/> for unit-testing
    /// <see cref="SpawnOrchestrator"/> without the game engine.
    /// </summary>
    internal sealed class FakeSpawnService : ISpawnService
    {
        public bool Ready { get; set; } = true;
        public bool ReturnNull { get; set; }

        public string LastBlueprintName { get; private set; }
        public string LastBlueprintPath { get; private set; }
        public double LastX { get; private set; }
        public double LastY { get; private set; }
        public double LastZ { get; private set; }
        public string LastDisplayName { get; private set; }

        public bool IsReady => Ready;

        public IReadOnlyList<GridDto> Spawn(string blueprintName, string blueprintPath,
            Vector3D position, string displayName)
        {
            LastBlueprintName = blueprintName;
            LastBlueprintPath = blueprintPath;
            LastX = position.X;
            LastY = position.Y;
            LastZ = position.Z;
            LastDisplayName = displayName;
            return ReturnNull ? null : new List<GridDto>();
        }

        // ── Unused stubs ──

        public IReadOnlyList<BlueprintInfo> ListBlueprints(string blueprintsFolder) =>
            new List<BlueprintInfo>();

        public IReadOnlyList<GridListItem> ListGrids() =>
            new List<GridListItem>();

        public GridDto GetGrid(long id) => null;
        public bool DeleteGrid(long id) => false;
        public IReadOnlyList<long> DeleteAllGrids() => new List<long>();

        public IReadOnlyList<TerminalBlockDto> GetGridBlocks(long gridId) =>
            new List<TerminalBlockDto>();

        public TerminalBlockDto GetBlockDetail(long gridId, int x, int y, int z) => null;

        public bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId) =>
            false;

        public string GetBlockProperty(long gridId, int x, int y, int z, string propertyId) =>
            null;

        public bool SetBlockProperty(long gridId, int x, int y, int z,
            string propertyId, string value) => false;

        public bool SetProgramCode(long gridId, int x, int y, int z, string code) => false;
        public bool WriteTextPanel(long gridId, int x, int y, int z, string text) => false;
        public bool RunProgram(long gridId, int x, int y, int z, string argument) => false;
        public bool UploadScript(long gridId, string code) => false;
        public ScriptRunResult RunScript(long gridId, string argument) => null;
        public string GetLcdContent(long gridId) => "";
    }
}
