using MySpawnerController.Api.Infrastructure;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace MySpawnerController
{
    /// <summary>
    /// Session component: starts JSON API on port 9997.
    /// Swagger UI is served by a separate process on port 9998.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private ApiServer _apiServer;
        private SpawnService _spawnService;
        private const int Port = 9997;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            _spawnService = new SpawnService();
            _apiServer = new ApiServer(Port, _spawnService, Logger.Info);
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
