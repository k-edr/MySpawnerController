using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace MySpawnerController
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private HttpListener _listener;
        private Thread _listenThread;
        private volatile bool _running;

        private readonly ConcurrentQueue<(string BpFile, Vector3D Offset, string Name,
            HttpListenerContext Ctx, bool IsBatch, List<GridInfo> Results)> _queue = new();

        private static readonly (string Name, Vector3D Offset)[] DefaultGrids =
        {
            ("TestGrid_MultiConnectorGrid", new Vector3D(0, 0, 0)),
            ("TestGrid_PBWithPanel",        new Vector3D(0, 0, 10)),
            ("TestGrid_SingleConnector",    new Vector3D(0, 0, -10)),
        };

        private static readonly string BlueprintsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpaceEngineers", "Blueprints", "local");

        private const int Port = 9998;

        // ── Lifecycle ──────────────────────────────────────────────

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            StartHttpListener();
        }

        protected override void UnloadData()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            _listener?.Close();
            Logger.Info("Stopped");
        }

        public override void UpdateAfterSimulation()
        {
            while (_queue.TryDequeue(out var item))
            {
                try { ProcessSpawnItem(item); }
                catch (Exception ex) { Logger.Error("ProcessSpawnItem", ex); }
            }
        }

        // ── HTTP server ────────────────────────────────────────────

        private void StartHttpListener()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _running = true;

            _listenThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "Spawner-HTTP"
            };
            _listenThread.Start();

            Logger.Info($"HTTP server started on http://localhost:{Port}/");
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex) when (_running) { Logger.Error("Listen", ex); }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                string path = ctx.Request.Url.AbsolutePath.Trim('/');
                Logger.Info($"HTTP {ctx.Request.HttpMethod} /{path}");

                switch (path)
                {
                    case "status":
                        Respond(ctx, 200, "OK");
                        break;
                    case "list":
                        HandleList(ctx);
                        break;
                    case "spawn":
                        HandleSpawn(ctx);
                        break;
                    default:
                        Respond(ctx, 404, "Try /spawn, /spawn?name=X&x=0&y=0&z=0, /status, /list");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Request", ex);
                try { Respond(ctx, 500, ex.Message); } catch { }
            }
        }

        private void HandleList(HttpListenerContext ctx)
        {
            var sb = new StringBuilder();
            if (Directory.Exists(BlueprintsFolder))
            {
                foreach (var dir in Directory.GetDirectories(BlueprintsFolder))
                {
                    string n = Path.GetFileName(dir);
                    sb.AppendLine(File.Exists(Path.Combine(dir, "bp.sbc"))
                        ? $"  {n}  OK" : $"  {n}  MISSING");
                }
            }
            else
            {
                sb.AppendLine($"Folder not found: {BlueprintsFolder}");
            }
            Respond(ctx, 200, sb.ToString());
        }

        private void HandleSpawn(HttpListenerContext ctx)
        {
            if (MySession.Static == null || !MySession.Static.Ready)
            {
                Respond(ctx, 503, "Session not ready");
                return;
            }

            string name = ctx.Request.QueryString["name"];
            if (!string.IsNullOrEmpty(name))
            {
                float x = ParseFloat(ctx.Request.QueryString["x"], 0);
                float y = ParseFloat(ctx.Request.QueryString["y"], 0);
                float z = ParseFloat(ctx.Request.QueryString["z"], 0);
                string bpFile = Path.Combine(BlueprintsFolder, name, "bp.sbc");

                if (!File.Exists(bpFile))
                {
                    Respond(ctx, 404, $"Not found: {name}");
                    return;
                }
                _queue.Enqueue((bpFile, new Vector3D(x, y, z), name, ctx, false, null));
            }
            else
            {
                var results = new List<GridInfo>();
                foreach (var (gName, offset) in DefaultGrids)
                {
                    string bpFile = Path.Combine(BlueprintsFolder, gName, "bp.sbc");
                    if (!File.Exists(bpFile))
                    {
                        results.Add(new GridInfo { DisplayName = gName + " MISSING" });
                        continue;
                    }
                    _queue.Enqueue((bpFile, offset, gName, ctx, true, results));
                }
                _queue.Enqueue((null, default, null, ctx, true, results)); // sentinel
            }
        }

        // ── Main-thread spawn ──────────────────────────────────────

        private void ProcessSpawnItem((string BpFile, Vector3D Offset, string Name,
            HttpListenerContext Ctx, bool IsBatch, List<GridInfo> Results) item)
        {
            if (item.BpFile == null && item.IsBatch)
            {
                Respond(item.Ctx, 200, FormatGridDetails(item.Results));
                return;
            }

            var result = SpawnBlueprint(item.BpFile, item.Offset, item.Name);

            if (item.IsBatch)
            {
                if (result != null)
                    item.Results.AddRange(result);
                else
                    item.Results.Add(new GridInfo { DisplayName = item.Name + " FAILED" });
            }
            else
            {
                if (result != null && result.Count > 0)
                    Respond(item.Ctx, 200, FormatGridDetails(result));
                else
                    Respond(item.Ctx, 500, $"Failed: {item.Name}");
            }
        }

        private static List<GridInfo> SpawnBlueprint(string bpFile, Vector3D offset, string blueprintName)
        {
            Logger.Info($"Spawning: {blueprintName} @ X:{offset.X} Y:{offset.Y} Z:{offset.Z}");

            MyObjectBuilder_Definitions definitions = null;
            try
            {
                Logger.Info("  Deserializing SBC via DeserializeXML...");
                using (var stream = File.OpenRead(bpFile))
                {
                    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                        stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                    definitions = obj as MyObjectBuilder_Definitions;
                }
                Logger.Info($"  DeserializeXML: ok={(definitions != null)}, definitions={(definitions?.ShipBlueprints?.Length > 0)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"DeserializeXML: {blueprintName}", ex);
                return null;
            }

            if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
            {
                Logger.Error($"No ShipBlueprints in {blueprintName}");
                return null;
            }

            Logger.Info($"  ShipBlueprints.Length={definitions.ShipBlueprints.Length}");

            var shipBp = definitions.ShipBlueprints[0];
            if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
            {
                Logger.Error($"No CubeGrids in {blueprintName}");
                return null;
            }

            var gridBuilders = new List<MyObjectBuilder_CubeGrid>();
            foreach (var grid in shipBp.CubeGrids)
            {
                if (grid is MyObjectBuilder_CubeGrid cubeGrid)
                    gridBuilders.Add(cubeGrid);
            }
            if (gridBuilders.Count == 0) return null;

            MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

            var results = new List<GridInfo>();

            foreach (var gb in gridBuilders)
            {
                gb.CreatePhysics = true;
                gb.Editable = true;
                gb.DestructibleBlocks = true;

                var po = gb.PositionAndOrientation;
                MatrixD m = po.HasValue
                    ? MatrixD.CreateWorld(po.Value.Position + offset, po.Value.Forward, po.Value.Up)
                    : MatrixD.CreateWorld(offset, Vector3D.Forward, Vector3D.Up);
                gb.PositionAndOrientation = new MyPositionAndOrientation(m);

                Logger.Info($"  CreatePhysics={gb.CreatePhysics}, Editable={gb.Editable}");

                IMyEntity ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);
                if (ent is MyCubeGrid g)
                {
                    PostSpawnFixup(g);
                    results.Add(BuildGridInfo(g));
                    Logger.Info($"  OK: {g.DisplayName} Id={g.EntityId}");
                }
            }

            return results;
        }

        // ── Grid info ──────────────────────────────────────────────

        private sealed class GridInfo
        {
            public long EntityId;
            public string DisplayName;
            public Vector3D Position;
            public Vector3 LinearVelocity;
            public List<BlockInfo> Blocks = new List<BlockInfo>();

            public string ToShortString()
            {
                return $"  {DisplayName} Id={EntityId} @ X:{Position.X:F1} Y:{Position.Y:F1} Z:{Position.Z:F1}";
            }
        }

        private sealed class BlockInfo
        {
            public string Name;
            public string TypeName;
            public Vector3I GridPos;
        }

        private static GridInfo BuildGridInfo(MyCubeGrid grid)
        {
            var info = new GridInfo
            {
                EntityId = grid.EntityId,
                DisplayName = grid.DisplayName,
            };

            // Position
            try { info.Position = grid.PositionComp.GetPosition(); }
            catch { }

            // Velocity
            try
            {
                if (grid.Physics != null)
                    info.LinearVelocity = grid.Physics.LinearVelocity;
            }
            catch { }

            // Blocks
            try
            {
                var blocks = new List<IMySlimBlock>();
                ((IMyCubeGrid)grid).GetBlocks(blocks);
                Logger.Info($"  GetBlocks returned {blocks.Count} blocks");
                foreach (var slim in blocks)
                {
                    if (slim == null) continue;
                    var blockInfo = new BlockInfo
                    {
                        GridPos = slim.Position,
                    };

                    if (slim.FatBlock != null)
                    {
                        var fat = slim.FatBlock;
                        blockInfo.Name = fat.DisplayNameText ?? fat.DefinitionDisplayNameText;
                        try { blockInfo.TypeName = fat.BlockDefinition.ToString(); }
                        catch { blockInfo.TypeName = fat.GetType().Name; }
                    }
                    else
                    {
                        var def = slim.BlockDefinition;
                        blockInfo.Name = def != null ? def.DisplayNameText ?? "Armor" : "Armor";
                        if (def != null)
                        {
                            var id = def.Id;
                            blockInfo.TypeName = !string.IsNullOrEmpty(id.SubtypeName) ? id.SubtypeName : "CubeBlock";
                        }
                        else
                            blockInfo.TypeName = "CubeBlock";
                    }

                    info.Blocks.Add(blockInfo);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"  BuildGridInfo blocks: {ex.Message}");
            }

            return info;
        }

        private static string FormatGridDetails(List<GridInfo> grids)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Spawned {grids.Count} grid(s):");
            sb.AppendLine();

            foreach (var g in grids)
            {
                sb.AppendLine($"┌ {g.DisplayName}");
                sb.AppendLine($"├ EntityId : {g.EntityId}");
                sb.AppendLine($"├ Position : X:{g.Position.X:F3} Y:{g.Position.Y:F3} Z:{g.Position.Z:F3}");
                sb.AppendLine($"├ Velocity : X:{g.LinearVelocity.X:F3} Y:{g.LinearVelocity.Y:F3} Z:{g.LinearVelocity.Z:F3}");
                sb.AppendLine($"├ Blocks ({g.Blocks.Count}):");
                foreach (var b in g.Blocks)
                {
                    sb.AppendLine($"│  [{b.GridPos}] {b.Name} ({b.TypeName})");
                }
                sb.AppendLine($"└");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ── Post-spawn fixup ───────────────────────────────────────

        private static void PostSpawnFixup(MyCubeGrid grid)
        {
            if (grid == null) return;

            var gridType = typeof(MyCubeGrid);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var onAdded = gridType.GetMethod("OnAddedToScene", flags);
            if (onAdded != null)
            {
                try
                {
                    onAdded.Invoke(grid, new object[] { grid });
                    Logger.Info("  OnAddedToScene OK");
                }
                catch (Exception ex) { Logger.Warn($"  OnAddedToScene: {ex.Message}"); }
            }

            var activatePhys = gridType.GetMethod("ActivatePhysics", flags);
            if (activatePhys != null)
            {
                try
                {
                    activatePhys.Invoke(grid, null);
                    Logger.Info("  ActivatePhysics OK");
                }
                catch (Exception ex) { Logger.Warn($"  ActivatePhysics: {ex.Message}"); }
            }

            var sessionType = MySession.Static?.GetType();
            var registerMethod = sessionType?.GetMethod("RegisterCubeGrid", flags);
            if (registerMethod != null)
            {
                try
                {
                    registerMethod.Invoke(MySession.Static, new object[] { grid });
                    Logger.Info("  RegisterCubeGrid OK");
                }
                catch (Exception ex) { Logger.Warn($"  RegisterCubeGrid: {ex.Message}"); }
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private static float ParseFloat(string s, float def)
        {
            return float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : def;
        }

        private static void Respond(HttpListenerContext ctx, int code, string body)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(body);
                ctx.Response.StatusCode = code;
                ctx.Response.ContentType = "text/plain; charset=utf-8";
                ctx.Response.ContentLength64 = data.Length;
                ctx.Response.OutputStream.Write(data, 0, data.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Respond failed: {ex.Message}");
            }
        }
    }
}
