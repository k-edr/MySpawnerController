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

namespace GridSpawner.Plugin.Application
{
    /// <summary>
    /// Implements <see cref="ISpawnService"/> for the API layer.
    /// Spawn/delete operations are enqueued and processed on the main thread
    /// via <see cref="ProcessQueue"/>, called from SessionComponent.UpdateAfterSimulation.
    /// </summary>
    public sealed class SpawnService : ISpawnService
    {
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly ConcurrentDictionary<long, MyCubeGrid> _trackedGrids =
            new ConcurrentDictionary<long, MyCubeGrid>();
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private readonly AppConfig _config;

        public SpawnService(AppConfig config)
        {
            _config = config;
        }

        public bool IsReady => MySession.Static?.Ready == true;

        // ── ISpawnService (called from API thread) ───────────

        public List<GridDto> Spawn(string blueprintName, string blueprintPath,
            Vector3D position, string displayName)
        {
            var task = new SpawnTask
            {
                BpFile = blueprintPath,
                Offset = position,
                BlueprintName = blueprintName,
                DisplayName = displayName
            };
            _queue.Enqueue(task);
            if (!task.Done.Wait(_timeout))
            {
                Logger.Error($"Spawn timeout: {blueprintName}");
                return null;
            }
            return task.Result;
        }

        public List<BlueprintInfo> ListBlueprints(string blueprintsFolder)
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

        public List<GridListItem> ListGrids()
        {
            var list = new List<GridListItem>();
            foreach (var kv in _trackedGrids)
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
                catch { }
            }
            return list;
        }

        public GridDto GetGrid(long id)
        {
            if (!_trackedGrids.TryGetValue(id, out var grid) ||
                grid == null || grid.MarkedForClose)
                return null;
            return GridDtoMapper.ToDto(grid);
        }

        public bool DeleteGrid(long id)
        {
            if (!_trackedGrids.TryGetValue(id, out var grid) || grid == null)
                return false;

            var task = new DeleteTask { EntityId = id };
            _queue.Enqueue(task);
            if (!task.Done.Wait(_timeout)) return false;
            return task.Result;
        }

        // ── Main-thread processing ───────────────────────────

        /// <summary>Must be called on the game main thread (UpdateAfterSimulation).</summary>
        public void ProcessQueue()
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    switch (item)
                    {
                        case SpawnTask st: ProcessSpawn(st); break;
                        case DeleteTask dt: ProcessDelete(dt); break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("ProcessQueue", ex);
                }
            }
        }

        // ── Spawn ────────────────────────────────────────────

        private void ProcessSpawn(SpawnTask task)
        {
            try
            {
                Logger.Info($"Spawning: {task.BlueprintName} @ X:{task.Offset.X} Y:{task.Offset.Y} Z:{task.Offset.Z}");

                var gridBuilders = BlueprintDeserializer.Deserialize(task.BpFile, _config, out string error);
                if (gridBuilders == null)
                {
                    Logger.Error($"Deserialize failed: {task.BlueprintName} — {error}");
                    task.Result = null;
                    return;
                }

                MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

                var result = new List<GridDto>();
                string displayName = task.DisplayName;

                foreach (var gb in gridBuilders)
                {
                    ConfigureBuilder(gb, task.Offset);
                    var ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);

                    if (ent is MyCubeGrid grid)
                    {
                        PostSpawnFixup.Apply(grid);

                        if (!string.IsNullOrEmpty(displayName))
                        {
                            grid.DisplayName = displayName;
                            displayName = null;
                        }

                        result.Add(GridDtoMapper.ToDto(grid));
                        _trackedGrids[grid.EntityId] = grid;
                        Logger.Info($"  OK: {grid.DisplayName} Id={grid.EntityId}");
                    }
                }

                task.Result = result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Spawn failed: {task.BlueprintName}", ex);
                task.Result = null;
            }
            finally
            {
                task.Done.Set();
            }
        }

        // ── Delete ────────────────────────────────────────────

        private void ProcessDelete(DeleteTask task)
        {
            try
            {
                if (_trackedGrids.TryRemove(task.EntityId, out var grid))
                {
                    grid.Close();
                    task.Result = true;
                }
                else
                {
                    task.Result = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Delete grid {task.EntityId}", ex);
                task.Result = false;
            }
            finally
            {
                task.Done.Set();
            }
        }

        // ── Helpers ──────────────────────────────────────────

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
}
