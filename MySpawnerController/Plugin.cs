using System;
using VRage.Plugins;

namespace MySpawnerController
{
    /// <summary>
    /// Plugin entry point.
    /// HTTP API starts in SessionComponent.Init() after world loads.
    /// Open http://localhost:9998/swagger for Swagger UI.
    ///
    /// Quick test:
    ///   Invoke-WebRequest -UseBasicParsing -Method POST http://localhost:9998/api/v1/spawn-tests
    /// </summary>
    public class Plugin : IPlugin
    {
        public void Init(object gameInstance)
        {
            Logger.Init();
            Logger.Info("Plugin loaded. Swagger UI at http://localhost:9998/swagger");
        }

        public void Update() { }
        public void Dispose() { }
    }
}
