using System.Collections.Generic;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using VRageMath;

namespace GridSpawner.Api.Application;

/// <summary>
/// Service contract for grid spawning, management, and terminal block interaction.
/// Implemented by the game plugin on the main thread.
/// </summary>
public interface ISpawnService
{
    IReadOnlyList<GridDto> Spawn(string blueprintName, string blueprintPath,
        Vector3D position, string displayName);

    IReadOnlyList<BlueprintInfo> ListBlueprints(string blueprintsFolder);

    IReadOnlyList<GridListItem> ListGrids();

    GridDto GetGrid(long id);

    bool DeleteGrid(long id);

    /// <summary>Delete all tracked grids. Returns IDs that were successfully removed.</summary>
    IReadOnlyList<long> DeleteAllGrids();

    // ── Terminal block interaction ──

    /// <summary>Get all functional (FatBlock) blocks on a grid.</summary>
    IReadOnlyList<TerminalBlockDto> GetGridBlocks(long gridId);

    /// <summary>Get detailed info for a specific block including actions and properties.</summary>
    TerminalBlockDto GetBlockDetail(long gridId, int x, int y, int z);

    /// <summary>Execute a terminal action on a block.</summary>
    bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId);

    /// <summary>Get a specific property value from a block.</summary>
    string GetBlockProperty(long gridId, int x, int y, int z, string propertyId);

    /// <summary>Set a property value on a block.</summary>
    bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value);

    /// <summary>Set the program code of a programmable block.</summary>
    bool SetProgramCode(long gridId, int x, int y, int z, string code);

    /// <summary>Write text to a text panel / LCD.</summary>
    bool WriteTextPanel(long gridId, int x, int y, int z, string text);

    /// <summary>Run a programmable block with an argument.</summary>
    bool RunProgram(long gridId, int x, int y, int z, string argument);

    /// <summary>Read text from a ProgrammableBlock's built-in LCD surface (GetSurface(0).GetText()).</summary>
    string GetPbSurfaceText(long gridId, int x, int y, int z);

    // ── PB/LCD convenience (by-type, no position needed) ──

    /// <summary>Upload script code to the first PB found on the grid.</summary>
    bool UploadScript(long gridId, string code);

    /// <summary>Run the first PB on the grid with an argument. Returns echo output.</summary>
    ScriptRunResult RunScript(long gridId, string argument);

    /// <summary>Read text content from the first LCD/TextPanel on the grid.</summary>
    string GetLcdContent(long gridId);

    bool IsReady { get; }
}
