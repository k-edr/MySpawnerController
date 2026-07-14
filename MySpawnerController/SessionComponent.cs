using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace MySpawnerController
{
    /// <summary>
    /// HTTP-controlled grid spawner: http://localhost:9998/
    ///
    /// GET /spawn                        — all 3 test grids at default positions
    /// GET /spawn?name=X&x=0&y=0&z=0    — single grid at custom position
    /// GET /status                       — health check
    /// GET /list                         — available blueprints
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private HttpListener _listener;
        private Thread _listenThread;
        private volatile bool _running;

        private readonly ConcurrentQueue<(string BpFile, Vector3D Offset, string Name,
            HttpListenerContext Ctx, bool IsBatch, StringBuilder BatchLog)> _queue = new();

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

            Logger.Info($"Listening on http://localhost:{Port}/");
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
                Logger.Info($"{ctx.Request.HttpMethod} /{path}");

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
                var log = new StringBuilder();
                foreach (var (gName, offset) in DefaultGrids)
                {
                    string bpFile = Path.Combine(BlueprintsFolder, gName, "bp.sbc");
                    if (!File.Exists(bpFile))
                    {
                        log.AppendLine($"  {gName}: MISSING");
                        continue;
                    }
                    _queue.Enqueue((bpFile, offset, gName, ctx, true, log));
                }
                _queue.Enqueue((null, default, null, ctx, true, log)); // sentinel
            }
        }

        // ── Main-thread spawn ──────────────────────────────────────

        private void ProcessSpawnItem((string BpFile, Vector3D Offset, string Name,
            HttpListenerContext Ctx, bool IsBatch, StringBuilder BatchLog) item)
        {
            if (item.BpFile == null && item.IsBatch)
            {
                Respond(item.Ctx, 200, $"Batch done:\n{item.BatchLog}");
                return;
            }

            bool ok = SpawnBlueprint(item.BpFile, item.Offset, item.Name);

            if (item.IsBatch)
                item.BatchLog.AppendLine(ok ? $"  {item.Name}: OK at {item.Offset}" : $"  {item.Name}: FAILED");
            else
                Respond(item.Ctx, ok ? 200 : 500, ok ? $"Spawned {item.Name} at {item.Offset}" : $"Failed: {item.Name}");
        }

        private static bool SpawnBlueprint(string bpFile, Vector3D offset, string blueprintName)
        {
            Logger.Info($"Spawn: {blueprintName} @ {offset}");

            // Deserialize SBC (definition XML with xsi:type support)
            MyObjectBuilder_Definitions definitions = null;
            try
            {
                using (var stream = File.OpenRead(bpFile))
                {
                    VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                        stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                    definitions = obj as MyObjectBuilder_Definitions;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"DeserializeXML: {blueprintName}", ex);
                return false;
            }

            if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
            {
                Logger.Error($"No ShipBlueprints: {blueprintName}");
                return false;
            }

            var shipBp = definitions.ShipBlueprints[0];
            if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
            {
                Logger.Error($"No CubeGrids: {blueprintName}");
                return false;
            }

            var gridBuilders = new List<MyObjectBuilder_CubeGrid>();
            foreach (var grid in shipBp.CubeGrids)
            {
                if (grid is MyObjectBuilder_CubeGrid cubeGrid)
                    gridBuilders.Add(cubeGrid);
            }
            if (gridBuilders.Count == 0) return false;

            MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

            foreach (var gb in gridBuilders)
            {
                var po = gb.PositionAndOrientation;
                MatrixD m = po.HasValue
                    ? MatrixD.CreateWorld(po.Value.Position + offset, po.Value.Forward, po.Value.Up)
                    : MatrixD.CreateWorld(offset, Vector3D.Forward, Vector3D.Up);
                gb.PositionAndOrientation = new MyPositionAndOrientation(m);

                IMyEntity ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);
                if (ent is MyCubeGrid g)
                    Logger.Info($"  OK: {g.DisplayName} Id={g.EntityId}");
            }

            return true;
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
            catch { /* disconnected */ }
        }
    }
}
