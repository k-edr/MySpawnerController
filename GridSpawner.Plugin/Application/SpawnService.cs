using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GridSpawner.Api.Application;
using GridSpawner.Plugin.Infrastructure;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace GridSpawner.Plugin.Application;

/// <summary>
/// Implements <see cref="ISpawnService"/> for the API layer.
/// All game-thread work is enqueued as <see cref="MainThreadTask"/> closures
/// and processed by <see cref="ProcessQueue"/>, called from SessionComponent.UpdateAfterSimulation.
/// </summary>
public sealed class SpawnService : ISpawnService, IDisposable
{
    private readonly ConcurrentQueue<MainThreadTask> _queue = new();
    private readonly GridTracker _tracker = new();
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private readonly AppConfig _config;
    private double _cleanupAccumulator; // seconds of game time
    private const double CleanupIntervalSeconds = 10.0;

    public SpawnService(AppConfig config)
    {
        _config = config;
    }

    public bool IsReady => MySession.Static?.Ready == true;

    // ── ISpawnService (called from API thread) ───────────────

    public IReadOnlyList<GridDto> Spawn(string blueprintName, string blueprintPath,
        Vector3D position, string displayName)
    {
        // ── Deserialize on API thread (CPU + I/O, no game state) ──
        var gridBuilders = BlueprintDeserializer.Deserialize(blueprintPath,
            _config, out string error);
        if (gridBuilders == null)
        {
            Logger.Error($"Spawn: {blueprintName} — {error}");
            return null;
        }

        // ── World creation on main thread ──
        using var task = new MainThreadTask<List<GridDto>>();
        string bpName = blueprintName;
        string dispName = displayName;
        Vector3D off = position;
        task.Process = () => task.Result = DoSpawn(gridBuilders, off, bpName, dispName);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error($"Spawn timeout: {bpName}");
            return null;
        }
        return task.Result;
    }

    public IReadOnlyList<BlueprintInfo> ListBlueprints(string blueprintsFolder)
    {
        var list = new List<BlueprintInfo>();
        if (!Directory.Exists(blueprintsFolder)) return list;
        foreach (var dir in Directory.GetDirectories(blueprintsFolder))
        {
            string name = Path.GetFileName(dir);
            list.Add(new BlueprintInfo
            {
                Name = name,
                Available = File.Exists(Path.Combine(dir, "bp.sbc"))
            });
        }
        return list;
    }

    public IReadOnlyList<GridListItem> ListGrids()
    {
        using var task = new MainThreadTask<List<GridListItem>>();
        task.Process = () => task.Result = _tracker.ListGrids();
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error("ListGrids timeout");
            return new List<GridListItem>();
        }
        return task.Result;
    }

    public GridDto GetGrid(long id)
    {
        using var task = new MainThreadTask<GridDto>();
        long entityId = id;
        task.Process = () => task.Result = _tracker.GetGrid(entityId);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error($"GetGrid timeout: {entityId}");
            return null;
        }
        return task.Result;
    }

    public bool DeleteGrid(long id)
    {
        using var task = new MainThreadTask<bool>();
        long entityId = id;
        task.Process = () => task.Result = _tracker.TryRemove(entityId);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public IReadOnlyList<long> DeleteAllGrids()
    {
        using var task = new MainThreadTask<List<long>>();
        task.Process = () => task.Result = _tracker.RemoveAll();
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error("DeleteAllGrids timeout");
            return new List<long>();
        }
        return task.Result;
    }

    // ── Terminal block interaction ───────────────────────────

    public IReadOnlyList<TerminalBlockDto> GetGridBlocks(long gridId)
    {
        using var task = new MainThreadTask<List<TerminalBlockDto>>();
        long gid = gridId;
        task.Process = () => task.Result = DoGetBlocks(gid);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error($"GetGridBlocks timeout: grid {gid}");
            return new List<TerminalBlockDto>();
        }
        return task.Result;
    }

    public TerminalBlockDto GetBlockDetail(long gridId, int x, int y, int z)
    {
        using var task = new MainThreadTask<TerminalBlockDto>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        task.Process = () => task.Result = DoGetBlockDetail(gid, pos);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
        {
            Logger.Error($"GetBlockDetail timeout: grid {gid} at ({x},{y},{z})");
            return null;
        }
        return task.Result;
    }

    public bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string act = actionId;
        task.Process = () => task.Result = DoExecuteAction(gid, pos, act);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public string GetBlockProperty(long gridId, int x, int y, int z, string propertyId)
    {
        using var task = new MainThreadTask<string>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string prop = propertyId;
        task.Process = () => task.Result = DoGetProperty(gid, pos, prop);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return null;
        return task.Result;
    }

    public bool SetBlockProperty(long gridId, int x, int y, int z,
        string propertyId, string value)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string prop = propertyId;
        string val = value;
        task.Process = () => task.Result = DoSetProperty(gid, pos, prop, val);
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public bool SetProgramCode(long gridId, int x, int y, int z, string code)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string c = code;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid)) { task.Result = false; return; }
            task.Result = TerminalBlockService.SetProgramCode(grid, pos, c);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public bool WriteTextPanel(long gridId, int x, int y, int z, string text)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string t = text;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid)) { task.Result = false; return; }
            task.Result = TerminalBlockService.WriteTextPanel(grid, pos, t);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public bool RunProgram(long gridId, int x, int y, int z, string argument)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        Vector3I pos = new(x, y, z);
        string arg = argument;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid)) { task.Result = false; return; }
            task.Result = TerminalBlockService.RunProgram(grid, pos, arg);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    // ── PB/LCD convenience (by-type) ─────────────────────────

    public bool UploadScript(long gridId, string code)
    {
        using var task = new MainThreadTask<bool>();
        long gid = gridId;
        string c = code;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid)) { task.Result = false; return; }
            task.Result = TerminalBlockService.UploadScript(grid, c);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return false;
        return task.Result;
    }

    public ScriptRunResult RunScript(long gridId, string argument)
    {
        using var task = new MainThreadTask<ScriptRunResult>();
        long gid = gridId;
        string arg = argument;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid))
            {
                task.Result = new ScriptRunResult { Echo = "", Success = false };
                return;
            }
            task.Result = TerminalBlockService.RunScript(grid, arg);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout))
            return new ScriptRunResult { Echo = "", Success = false };
        return task.Result;
    }

    public string GetLcdContent(long gridId)
    {
        using var task = new MainThreadTask<string>();
        long gid = gridId;
        task.Process = () =>
        {
            if (!_tracker.TryGet(gid, out var grid)) { task.Result = ""; return; }
            task.Result = TerminalBlockService.GetLcdContent(grid);
        };
        _queue.Enqueue(task);
        if (!task.Done.Wait(_timeout)) return "";
        return task.Result;
    }

    // ── Main-thread processing
    // ── Main-thread processing ───────────────────────────────

    /// <summary>Must be called on the game main thread (UpdateAfterSimulation).</summary>
    public void ProcessQueue()
    {
        while (_queue.TryDequeue(out var task))
        {
            try
            {
                task.Process?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error("ProcessQueue", ex);
                task.Error = ex;
            }
            finally
            {
                task.Done.Set();
            }
        }

        // Periodic dead-grid cleanup (every 10 seconds of sim time, regardless of FPS)
        _cleanupAccumulator += MySession.Static.ElapsedGameTime.TotalSeconds;
        if (_cleanupAccumulator >= CleanupIntervalSeconds)
        {
            _cleanupAccumulator = 0;
            int removed = _tracker.CleanupDead();
            if (removed > 0)
                Logger.Info($"CleanupDead: removed {removed} stale grids");
        }
    }

    // ── Dispose ──────────────────────────────────────────────

    public void Dispose()
    {
        // Wake all waiting threads before disposing — avoids ObjectDisposedException
        var tasks = new List<MainThreadTask>();
        while (_queue.TryDequeue(out var t))
            tasks.Add(t);

        foreach (var t in tasks)
        {
            try { t.Done.Set(); } catch { }
        }
        foreach (var t in tasks)
        {
            try { t.Dispose(); } catch { }
        }
    }

    // ── Private: game-thread implementations ─────────────────

    private List<GridDto> DoSpawn(List<MyObjectBuilder_CubeGrid> gridBuilders,
        Vector3D offset, string bpName, string dispName)
    {
        Logger.Info($"Spawning: {bpName} @ X:{offset.X} Y:{offset.Y} Z:{offset.Z}");

        MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

        var result = new List<GridDto>();
        string displayName = dispName;

        foreach (var gb in gridBuilders)
        {
            ConfigureBuilder(gb, offset);
            var ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);

            if (ent is MyCubeGrid grid)
            {
                if (!string.IsNullOrEmpty(displayName))
                {
                    grid.DisplayName = displayName;
                    displayName = null;
                }

                result.Add(GridDtoMapper.ToDto(grid));
                _tracker.Track(grid);
                Logger.Info($"  OK: {grid.DisplayName} Id={grid.EntityId}");
            }
        }

        return result;
    }

    private List<TerminalBlockDto> DoGetBlocks(long gridId)
    {
        if (!_tracker.TryGet(gridId, out var grid))
            return new List<TerminalBlockDto>();
        return TerminalBlockService.GetGridBlocks(grid);
    }

    private TerminalBlockDto DoGetBlockDetail(long gridId, Vector3I pos)
    {
        if (!_tracker.TryGet(gridId, out var grid))
            return null;
        return TerminalBlockService.GetBlockDetail(grid, pos);
    }

    private bool DoExecuteAction(long gridId, Vector3I pos, string actionId)
    {
        if (!_tracker.TryGet(gridId, out var grid))
            return false;
        return TerminalBlockService.ExecuteAction(grid, pos, actionId);
    }

    private string DoGetProperty(long gridId, Vector3I pos, string propertyId)
    {
        if (!_tracker.TryGet(gridId, out var grid))
            return null;
        return TerminalBlockService.GetProperty(grid, pos, propertyId);
    }

    private bool DoSetProperty(long gridId, Vector3I pos, string propertyId, string value)
    {
        if (!_tracker.TryGet(gridId, out var grid))
            return false;
        return TerminalBlockService.SetProperty(grid, pos, propertyId, value);
    }

    private static void ConfigureBuilder(MyObjectBuilder_CubeGrid gb, Vector3D offset)
    {
        gb.CreatePhysics = true;
        gb.Editable = true;
        gb.DestructibleBlocks = true;

        var po = gb.PositionAndOrientation;
        MatrixD m = po.HasValue
            ? MatrixD.CreateWorld(po.Value.Position + offset, po.Value.Forward, po.Value.Up)
            : MatrixD.CreateWorld(offset, Vector3D.Forward, Vector3D.Up);
        gb.PositionAndOrientation = new MyPositionAndOrientation(m);
    }
}
