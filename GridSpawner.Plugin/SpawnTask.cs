using System.Collections.Generic;
using System.Threading;
using GridSpawner.Shared;
using VRageMath;

namespace GridSpawner.Plugin
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
