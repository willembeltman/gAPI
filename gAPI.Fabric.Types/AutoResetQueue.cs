using System.Collections.Concurrent;

namespace gAPI.Fabric.Types;

public class AutoResetQueue<T> : IDisposable
{
    private readonly ConcurrentQueue<T> Queue = new();
    private readonly AutoResetEvent SendSignal = new(false);

    public void Enqueue(T item)
    {
        Queue.Enqueue(item);
        SendSignal.Set();
    }

    public IEnumerable<T> GetEnumerable(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
            if (SendSignal.WaitOne(100))
                while (Queue.TryDequeue(out var item))
                    yield return item;
    }

    public void Dispose()
    {
        SendSignal.Set();
        SendSignal.Dispose();
    }
}