using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GridSpawner.Plugin.Infrastructure;
using GridSpawner.Shared.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
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

        // Close subgrids (piston tops, rotor heads, wheels) before closing parent
        foreach (var sub in FindConnectedSubgrids(grid))
        {
            _grids.TryRemove(sub.EntityId, out _);
            try { sub.Close(); }
            catch (Exception ex) { Logger.Warn($"GridTracker close subgrid {sub.EntityId}: {ex.Message}"); }
        }

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
                // Close subgrids first
                foreach (var sub in FindConnectedSubgrids(grid))
                {
                    _grids.TryRemove(sub.EntityId, out _);
                    try { sub.Close(); removed.Add(sub.EntityId); }
                    catch (Exception ex) { Logger.Warn($"GridTracker close subgrid {sub.EntityId}: {ex.Message}"); }
                }

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
        // Collect keys first to avoid mutation during enumeration
        var keys = new List<long>(_grids.Keys);
        foreach (var key in keys)
        {
            if (_grids.TryGetValue(key, out var grid))
            {
                if (grid == null || grid.MarkedForClose)
                {
                    if (_grids.TryRemove(key, out _))
                        removed++;
                }
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

    // ── Subgrid discovery ─────────────────────────────────────

    /// <summary>
    /// Recursively finds all subgrids connected to <paramref name="root"/>
    /// through mechanical blocks (rotors, pistons, wheels).
    /// Uses MyAPIGateway.GridGroups (Physical link) for reliable discovery.
    /// </summary>
    private static HashSet<MyCubeGrid> FindConnectedSubgrids(MyCubeGrid root)
    {
        var found = new HashSet<MyCubeGrid>();

        var group = new List<IMyCubeGrid>();
        MyAPIGateway.GridGroups.GetGroup(root, GridLinkTypeEnum.Physical, group);

        foreach (var g in group)
        {
            if (g.EntityId == root.EntityId) continue;
            if (g is MyCubeGrid sub && !sub.MarkedForClose)
            {
                found.Add(sub);
                Logger.Info($"GridTracker: subgrid found {sub.EntityId} ({sub.DisplayName}) under {root.EntityId}");
            }
        }

        return found;
    }
}
