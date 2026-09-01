using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Wss;

public class WssServiceSubscription
    : IServiceSubscription
    , IAsyncDisposable
{
    public WssServiceSubscription(
        IWssServerConnection connection,
        ILoggerFactory loggerFactory,
        ServiceSubscriptionCollection hubHosts,
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

    public ServiceSubscriptionId Id { get; }
    public ILogger Logger { get; }
    public IWssServerConnection Connection { get; }
    public ServiceSubscriptionCollection HubHosts { get; }
    public FabricClient FabricClient { get; }
    public ConnectionId ConnectionId { get; }
    public SessionId SessionId { get; }
    public UserId UserId { get; }
    public ServiceId ServiceId { get; }

    // SignalRConnection => FabricClient: Subscribe
    public Task InitializeAsync(CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("InitializeAsync()");

        return FabricClient.SubscribeAsync(this, ct);
    }

    // FabricClient => SignalRConnection: Client functies
    public Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("SendAsync({message})", message);

        return Connection.Send_SendRequest_ToClientAsync(this, message, ct);
    }
    public Task Send_SendRequestCancelled_ToClient_Async(SendRequestCancelledDto message, CancellationToken ct)
        => Connection.Send_SendRequestCancelled_ToClientAsync(this, message, ct);

    public bool HasRequest(RequestId requestId) => Connection.HasRequest(requestId);

    public Task SendArgumentRequestAsync(InvokeArgumentRequestDto request, CancellationToken ct)
        => Connection.Send_InvokeArgumentRequest_ToClientAsync(this, request, ct);

    public Task SendArgumentResponseAsync(InvokeArgumentResponseDto response, CancellationToken ct)
        => Connection.Send_InvokeArgumentResponse_ToClientAsync(this, response, ct);
    public Task SendArgumentCancelledAsync(InvokeArgumentCancelledDto response, CancellationToken ct)
        => Connection.Send_InvokeArgumentCancelled_ToClientAsync(this, response, ct);

    public IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("InvokeAsync({request})", request);

        return Connection.Send_InvokeRequest_ToClientAsync(this, request, ct);
    }
    public Task Send_InvokeRequestCancelled_ToClient_Async(InvokeRequestCancelledDto request, CancellationToken ct)
        => Connection.Send_InvokeRequestCancelled_ToClientAsync(this, request, ct);

    // SignalRConnection => FabricClient: Unsubscribe (op dispose)
    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("DisposeAsync()");

        if (Interlocked.Exchange(ref Disposed, 1) == 0)
        {
            await FabricClient.UnsubscribeAsync(this, default);
            HubHosts.Remove(Id);
        }
        GC.SuppressFinalize(this);
    }
}