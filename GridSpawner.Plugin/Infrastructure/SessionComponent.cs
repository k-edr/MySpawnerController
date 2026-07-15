using GridSpawner.Api.Infrastructure;
using GridSpawner.Plugin.Application;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace GridSpawner.Plugin.Infrastructure
{
    /// <summary>
    /// Session component: starts JSON API server.
    /// Reads config from %APPDATA%\SpaceEngineers\GridSpawner.json
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private ApiServer _apiServer;
        private SpawnService _spawnService;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var config = AppConfigLoader.LoadOrCreate();
            Logger.Info($"Config: apiPort={config.ApiPort}, cors={config.SwaggerCorsOrigin}");

            _spawnService = new SpawnService(config);
            _apiServer = new ApiServer(config, _spawnService, Logger.Info);
            _apiServer.Start();
        }

        public override void UpdateAfterSimulation()
        {
            _spawnService?.ProcessQueue();
        }

        protected override void UnloadData()
        {
            _apiServer?.Dispose();
            Logger.Info("API server stopped");
        }
    }
}
