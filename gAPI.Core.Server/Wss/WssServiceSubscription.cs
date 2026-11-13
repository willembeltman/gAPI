using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.Core.Server.Wss;

public class WssServiceSubscription
    : IServiceSubscription
    , IAsyncDisposable
{
    public WssServiceSubscription(
        IServerConnection connection,
        ILoggerFactory loggerFactory,
        ServiceSubscriptionCollection serviceSubscriptionCollection,
        FabricClient fabricClient,
        ClientConnectionId clientConnectionId,
        ServiceSubscriptionId serviceSubscriptionId,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        ServiceSubscriptionCollection = serviceSubscriptionCollection;
        Connection = connection;
        FabricClient = fabricClient;
        ClientConnectionId = clientConnectionId;
        ServiceSubscriptionId = serviceSubscriptionId;
        SessionId = sessionId;
        UserId = userId;
        ServiceId = serviceId;

        ServiceSubscriptionCollection.Add(this, serviceId);
        Logger = loggerFactory.CreateLogger<WssServiceSubscription>();
        Stopwatch = Stopwatch.StartNew();
    }

    private byte Disposed;

    private readonly ILogger Logger;
    public Stopwatch Stopwatch { get; }
    public IServerConnection Connection { get; }
    public ServiceSubscriptionCollection ServiceSubscriptionCollection { get; }
    public FabricClient FabricClient { get; }
    public ClientConnectionId ClientConnectionId { get; }
    public ServiceSubscriptionId ServiceSubscriptionId { get; }
    public ServiceId ServiceId { get; }
    public UserId UserId { get; }
    public SessionId SessionId { get; }

    private readonly ConcurrentQueue<(double time, long bytes)> SendBytesLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceivedBytesLogger = new();

    private (int count, long bytes) GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = 0L;
        var count = 0;
        foreach (var item in queue)
        {
            bytes += item.bytes;
            count++;
        }
        return new((int)(count / interval), (int)(bytes / interval));
    }
    public (int count, long bytes) GetReceiveBytesPerSecond() => GetSpeed(SendBytesLogger);
    public (int count, long bytes) GetSendBytesPerSecond() => GetSpeed(ReceivedBytesLogger);
    public void EnqueueSend(long size)
    {
        SendBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceivedBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("DisposeAsync()");

        if (Interlocked.Exchange(ref Disposed, 1) == 0)
        {
            await FabricClient.UnsubscribeAsync(this, default);
            ServiceSubscriptionCollection.Remove(ServiceSubscriptionId);
        }
        GC.SuppressFinalize(this);
    }
}