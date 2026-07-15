using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GridSpawner.Plugin.Infrastructure;
using GridSpawner.Shared.Models;
using Sandbox.Game.Entities;
using VRageMath;

namespace GridSpawner.Plugin.Application;

/// <summary>
/// Tracks spawned MyCubeGrid instances by entity ID.
/// Handles cleanup of dead/marked-for-close grids.
/// </summary>
internal sealed class GridTracker
{
    private readonly ConcurrentDictionary<long, MyCubeGrid> _grids = new();

    public void Track(MyCubeGrid grid)
    {
        if (grid == null) return;
        _grids[grid.EntityId] = grid;
    }

    public bool TryGet(long id, out MyCubeGrid grid) =>
        _grids.TryGetValue(id, out grid) && grid != null && !grid.MarkedForClose;

    public bool TryRemove(long id)
    {
        if (!_grids.TryRemove(id, out var grid))
            return false;
        try { grid?.Close(); }
        catch (Exception ex) { Logger.Warn($"GridTracker close {id}: {ex.Message}"); }
        return true;
    }

    /// <summary>Close and remove all tracked grids. Returns IDs that were successfully removed.</summary>
    public List<long> RemoveAll()
    {
        var removed = new List<long>();
        // Collect keys first — modifying during enumeration
        var keys = new List<long>(_grids.Keys);
        foreach (var id in keys)
        {
            if (_grids.TryRemove(id, out var grid))
            {
                try { grid?.Close(); removed.Add(id); }
                catch (Exception ex) { Logger.Warn($"GridTracker close {id}: {ex.Message}"); }
            }
        }
        return removed;
    }

    /// <summary>Purge entries whose grid is null or marked for close.</summary>
    public int CleanupDead()
    {
        int removed = 0;
        foreach (var kv in _grids)
        {
            if (kv.Value == null || kv.Value.MarkedForClose)
            {
                if (_grids.TryRemove(kv.Key, out _))
                    removed++;
            }
        }
        return removed;
    }

    public List<GridListItem> ListGrids()
    {
        var list = new List<GridListItem>();
        foreach (var kv in _grids)
        {
            try
            {
                var grid = kv.Value;
                if (grid == null || grid.MarkedForClose) continue;

                var pos = grid.PositionComp.GetPosition();
                list.Add(new GridListItem
                {
                    Id = grid.EntityId,
                    Name = grid.DisplayName,
                    Position = new Vector3Dto { X = pos.X, Y = pos.Y, Z = pos.Z }
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"GridTracker list: entity {kv.Key} — {ex.Message}");
            }
        }
        return list;
    }

    public GridDto GetGrid(long id)
    {
        if (!TryGet(id, out var grid))
            return null;
        return GridDtoMapper.ToDto(grid);
    }
}
