namespace FinderDemo.Areas.Upload;

/// <summary>
/// Reports progress immediately on the calling thread. <see cref="Progress{T}"/> would post every report to the
/// thread pool in a console app (no synchronization context), where they can run out of order.
/// </summary>
internal sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
