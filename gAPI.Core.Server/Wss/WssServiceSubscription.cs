using gAPI.Core.Server.Collections;
using gAPI.Core.Dtos;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Wss;

public class WssServiceSubscription 
    : ISseHost
    , IAsyncDisposable
{
    public WssServiceSubscription(
        ISignalRInvoker connection, 
        ILoggerFactory loggerFactory,
        SseHostCollection hubHosts, 
        FabricClient fabricClient,
        ConnectionId connectionId,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        HubHosts = hubHosts;
        Connection = connection;
        FabricClient = fabricClient;
        ConnectionId = connectionId;
        SessionId = sessionId;
        UserId = userId;
        ServiceId = serviceId;

        Id = HubHosts.Add(this);
        Logger = loggerFactory.CreateLogger<WssServiceSubscription>();
    }

    private byte Disposed;

    public SseHostId Id { get; }
    public ILogger Logger { get; }
    public ISignalRInvoker Connection { get; }
    public SseHostCollection HubHosts { get; }
    public FabricClient FabricClient { get; }
    public ConnectionId ConnectionId { get; }
    public SessionId SessionId { get; }
    public UserId UserId { get; }
    public ServiceId ServiceId { get; }

    // SignalRConnection => FabricClient: Subscribe
    public Task InitializeAsync(CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " InitializeAsync()");

        return FabricClient.SubscribeAsync(this, ct);
    }

    // FabricClient => SignalRConnection: Client functies
    public Task SendAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " SendAsync({message})", message);

        return Connection.Send_SendRequest_ToClientAsync(this, message, ct);
    }

    public IAsyncEnumerable<InvokeResponseDto> InvokeAsync(InvokeRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " InvokeAsync({request})", request);

        return Connection.Send_InvokeRequest_ToClientAsync(this, request, ct);
    }

    // SignalRConnection => FabricClient: Unsubscribe (op dispose)
    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " DisposeAsync()");

        if (Interlocked.Exchange(ref Disposed, 1) == 0)
        {
            await FabricClient.UnsubscribeAsync(this, default);
            HubHosts.Remove(Id);
        }
        GC.SuppressFinalize(this);
    }
}