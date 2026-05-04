using gAPI.FabricNode.Collections;
using gAPI.FabricNode.Interfaces;
using gAPI.Ids;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.FabricNode.Models;

public record Service(ServiceId Id, FabricManager FabricManager) : IActor
{
    public ServiceId Id { get; } = Id;
    public UserCollection Users { get; } = new();
    public SessionCollection Sessions { get; } = new();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();


    public async Task Subscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.TryAdd(userId, connection);
        Sessions.TryAdd(sessionId, connection);
        EnqueueReceive(receiveSize);
    }
    public async Task Unsubscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.TryRemove(userId, connection);
        Sessions.TryRemove(sessionId, connection);
        EnqueueReceive(receiveSize);
    }

    public (IEnumerable<FabricHost>, IActor) GetFabricHosts(UserId? userId, SessionId? sessionId)
    {
        if (userId != null)
        {
            var user = Users.TryGet(userId.Value);
            if (user == null) return ([], this);
            return (user.Connections.Values, user);
        }
        else if (sessionId != null)
        {
            var session = Sessions.TryGet(sessionId.Value);
            if (session == null) return ([], this);
            return (session.Connections.Values, session);
        }
        else
        {
            return (FabricManager.Connections, this);
        }
    }

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

    public override string ToString()
    {
        return Id.ToString();
    }
}