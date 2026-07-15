using System.Collections.Generic;
using System.Threading;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using VRageMath;

namespace GridSpawner.Plugin.Application
{
    internal sealed class SpawnTask
    {
        public string BpFile;
        public Vector3D Offset;
        public string BlueprintName;
        public string DisplayName;
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        public List<GridDto> Result;
    }
}
