using System;
using VRage.Plugins;

namespace MySpawnerController
{
    /// <summary>
    /// Plugin entry point. HTTP server starts in SessionComponent.Init().
    /// Send GET http://localhost:9998/spawn to trigger grid spawning.
    /// </summary>
    public class Plugin : IPlugin
    {
        public void Init(object gameInstance)
        {
            Logger.Init();
            Logger.Info($"Plugin loaded. Send GET http://localhost:9998/spawn after world loads.");
        }

        public void Update() { }
        public void Dispose() { }
    }
}
