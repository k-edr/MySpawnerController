using System;
using VRage.Plugins;

namespace GridSpawner.Plugin.Infrastructure
{
    /// <summary>
    /// Plugin entry point.
    /// HTTP API starts in SessionComponent.Init() after world loads.
    /// Open http://localhost:9998/swagger for Swagger UI.
    ///
    /// Quick test:
    ///   Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9997/api/v1/spawn-tests
    /// </summary>
    public class Plugin : IPlugin
    {
        public void Init(object gameInstance)
        {
            Logger.Init();
            Logger.Info("Plugin loaded. Game API: http://localhost:9997 | Swagger: http://localhost:9998/swagger (separate process)");
        }

        public void Update() { }
        public void Dispose() { }
    }
}
