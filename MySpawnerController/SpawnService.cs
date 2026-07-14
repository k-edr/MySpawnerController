using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using MySpawnerController.Api;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace MySpawnerController
{
    /// <summary>
    /// Implements ISpawnService for the API layer.
    /// Spawn/delete operations are enqueued and processed on the main thread
    /// via ProcessQueue(), called from SessionComponent.UpdateAfterSimulation.
    /// </summary>
    public sealed class SpawnService : ISpawnService
    {
        private sealed class SpawnTask
        {
            public string BpFile;
            public Vector3D Offset;
            public string BlueprintName;
            public string DisplayName;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public List<GridDto> Result;
        }

        private sealed class DeleteTask
        {
            public long EntityId;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public bool Result;
        }

        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly ConcurrentDictionary<long, MyCubeGrid> _trackedGrids =
            new ConcurrentDictionary<long, MyCubeGrid>();
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        private static readonly BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

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
            return BuildGridDto(grid);
        }

        public bool DeleteGrid(long id)
        {
            if (!_trackedGrids.TryRemove(id, out var grid) || grid == null)
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

        // ── Spawn logic ──────────────────────────────────────

        private void ProcessSpawn(SpawnTask task)
        {
            try
            {
                Logger.Info($"Spawning: {task.BlueprintName} @ X:{task.Offset.X} Y:{task.Offset.Y} Z:{task.Offset.Z}");
                Logger.Info("  Deserializing SBC via DeserializeXML...");

                MyObjectBuilder_Definitions definitions;
                using (var stream = File.OpenRead(task.BpFile))
                {
                    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                        stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                    definitions = obj as MyObjectBuilder_Definitions;
                }

                Logger.Info($"  DeserializeXML: ok={(definitions != null)}, definitions={(definitions?.ShipBlueprints?.Length > 0)}");

                if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
                {
                    Logger.Error($"No ShipBlueprints in {task.BlueprintName}");
                    task.Result = null;
                    return;
                }

                Logger.Info($"  ShipBlueprints.Length={definitions.ShipBlueprints.Length}");

                var shipBp = definitions.ShipBlueprints[0];
                if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
                {
                    Logger.Error($"No CubeGrids in {task.BlueprintName}");
                    task.Result = null;
                    return;
                }

                var gridBuilders = new List<MyObjectBuilder_CubeGrid>();
                foreach (var g in shipBp.CubeGrids)
                {
                    if (g is MyObjectBuilder_CubeGrid cg) gridBuilders.Add(cg);
                }
                if (gridBuilders.Count == 0)
                {
                    task.Result = null;
                    return;
                }

                MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

                var result = new List<GridDto>();
                string displayName = task.DisplayName;

                foreach (var gb in gridBuilders)
                {
                    gb.CreatePhysics = true;
                    gb.Editable = true;
                    gb.DestructibleBlocks = true;

                    var po = gb.PositionAndOrientation;
                    MatrixD m = po.HasValue
                        ? MatrixD.CreateWorld(po.Value.Position + task.Offset, po.Value.Forward, po.Value.Up)
                        : MatrixD.CreateWorld(task.Offset, Vector3D.Forward, Vector3D.Up);
                    gb.PositionAndOrientation = new MyPositionAndOrientation(m);

                    Logger.Info($"  CreatePhysics={gb.CreatePhysics}, Editable={gb.Editable}");

                    IMyEntity ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);
                    if (ent is MyCubeGrid grid)
                    {
                        PostSpawnFixup(grid);

                        if (!string.IsNullOrEmpty(displayName))
                        {
                            grid.DisplayName = displayName;
                            displayName = null;
                        }

                        result.Add(BuildGridDto(grid));
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

        // ── Delete logic ─────────────────────────────────────

        private void ProcessDelete(DeleteTask task)
        {
            try
            {
                if (_trackedGrids.TryGetValue(task.EntityId, out var grid))
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

        // ── Post-spawn fixup ─────────────────────────────────

        private static void PostSpawnFixup(MyCubeGrid grid)
        {
            if (grid == null) return;
            var gridType = typeof(MyCubeGrid);

            try
            {
                gridType.GetMethod("OnAddedToScene", Flags)?.Invoke(grid, new object[] { grid });
                Logger.Info("  OnAddedToScene OK");
            }
            catch (Exception ex) { Logger.Warn($"  OnAddedToScene: {ex.Message}"); }

            try
            {
                gridType.GetMethod("ActivatePhysics", Flags)?.Invoke(grid, null);
                Logger.Info("  ActivatePhysics OK");
            }
            catch (Exception ex) { Logger.Warn($"  ActivatePhysics: {ex.Message}"); }

            try
            {
                var regMethod = MySession.Static?.GetType()?.GetMethod("RegisterCubeGrid", Flags);
                regMethod?.Invoke(MySession.Static, new object[] { grid });
                Logger.Info("  RegisterCubeGrid OK");
            }
            catch (Exception ex) { Logger.Warn($"  RegisterCubeGrid: {ex.Message}"); }
        }

        // ── DTO builder ──────────────────────────────────────

        private static GridDto BuildGridDto(MyCubeGrid grid)
        {
            var dto = new GridDto { Id = grid.EntityId, Name = grid.DisplayName };

            try
            {
                var p = grid.PositionComp.GetPosition();
                dto.Position = new Vector3Dto { X = p.X, Y = p.Y, Z = p.Z };
            }
            catch { }

            try
            {
                if (grid.Physics != null)
                {
                    var v = grid.Physics.LinearVelocity;
                    dto.Velocity = new Vector3Dto { X = v.X, Y = v.Y, Z = v.Z };
                }
            }
            catch { }

            try
            {
                var blocks = new List<IMySlimBlock>();
                ((IMyCubeGrid)grid).GetBlocks(blocks);
                foreach (var slim in blocks)
                {
                    if (slim == null) continue;
                    var b = new BlockDto
                    {
                        GridPosition = new Vector3IDto
                        {
                            X = slim.Position.X,
                            Y = slim.Position.Y,
                            Z = slim.Position.Z
                        }
                    };

                    if (slim.FatBlock != null)
                    {
                        var fat = slim.FatBlock;
                        b.Name = fat.DisplayNameText ?? fat.DefinitionDisplayNameText;
                        try { b.Type = fat.BlockDefinition.ToString(); }
                        catch { b.Type = fat.GetType().Name; }
                    }
                    else
                    {
                        var def = slim.BlockDefinition;
                        b.Name = def?.DisplayNameText ?? "Armor";
                        if (def != null)
                        {
                            var id = def.Id;
                            b.Type = !string.IsNullOrEmpty(id.SubtypeName)
                                ? id.SubtypeName : "CubeBlock";
                        }
                        else b.Type = "CubeBlock";
                    }

                    dto.Blocks.Add(b);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"BuildGridDto blocks: {ex.Message}");
            }

            return dto;
        }
    }
}
