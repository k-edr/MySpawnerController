using System.Threading;

namespace GridSpawner.Plugin.Application
{
    internal sealed class DeleteTask
    {
        public long EntityId;
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        public bool Result;
    }
}
