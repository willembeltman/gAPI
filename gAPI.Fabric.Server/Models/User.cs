using gAPI.Core.Ids;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.Fabric.Server.Models;

public record User(UserId Id) : IActor
{
    public UserId Id { get; } = Id;
    public ConcurrentDictionary<FabricConnectionId, FabricHost> Connections { get; } = new();

    public void Subscribe(FabricHost connection) => Connections[connection.FabricConnectionId] = connection;
    public void Unsubscribe(FabricHost connection) => Connections.TryRemove(connection.FabricConnectionId, out _);

    public override string ToString() => Id.ToString();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();

    private long GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        return queue.Sum(x => x.bytes);

    }
    public long GetSendSpeed() => GetSpeed(SendLogger);
    public long GetReceiveSpeed() => GetSpeed(ReceiveLogger);

    public void EnqueueSend(long size)
    {
        SendLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceiveLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
}
