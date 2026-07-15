using System;
using System.Threading;

namespace GridSpawner.Plugin.Application;

/// <summary>
/// Base task for main-thread processing.
/// API thread creates it, sets <see cref="Process"/>, enqueues, waits on <see cref="Done"/>.
/// Main thread dequeues and invokes <see cref="Process"/>.
/// </summary>
internal class MainThreadTask : IDisposable
{
    public readonly ManualResetEventSlim Done = new(false);
    public Action Process;
    public Exception Error;

    public void Dispose() => Done?.Dispose();
}

/// <summary>Generic variant with typed <see cref="Result"/>.</summary>
internal sealed class MainThreadTask<T> : MainThreadTask
{
    public T Result;
}
