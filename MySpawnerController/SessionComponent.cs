using MySpawnerController.Api;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace MySpawnerController
{
    /// <summary>
    /// Session component that initializes the HTTP API server and
    /// processes spawn/delete requests on the main game thread.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private ApiServer _apiServer;
        private SpawnService _spawnService;
        private const int Port = 9998;

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
