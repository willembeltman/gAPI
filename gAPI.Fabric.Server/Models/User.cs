using gAPI.Fabric.Interfaces;
using gAPI.Core.Ids;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.Fabric.Models;

public record User(UserId Id) : IActor
{
    public UserId Id { get; } = Id;
    public ConcurrentDictionary<FabricHostId, FabricHost> Connections { get; } = new();

    public void Subscribe(FabricHost connection) => Connections[connection.Id] = connection;
    public void Unsubscribe(FabricHost connection) => Connections.TryRemove(connection.Id, out _);

    public override string ToString() => Id.ToString();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();

    private string GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = queue.Sum(x => x.bytes);

        return bytes switch
        {
            < 1024 => $"{bytes}b/sec",
            < 1024 * 1024 => $"{bytes / 1024}kb/sec",
            < 1024L * 1024 * 1024 => $"{bytes / (1024 * 1024)}mb/sec",
            < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024L * 1024 * 1024)}gb/sec",
            _ => $"{bytes / (1024L * 1024 * 1024 * 1024)}tb/sec"
        };
    }
    public string GetSendSpeed() => GetSpeed(SendLogger);
    public string GetReceiveSpeed() => GetSpeed(ReceiveLogger);

    public void EnqueueSend(long size)
    {
        SendLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceiveLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
}
