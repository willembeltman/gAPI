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
        ClientConnectionId clientConnectionId,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        HubHosts = hubHosts;
        Connection = connection;
        FabricClient = fabricClient;
        ClientConnectionId = clientConnectionId;
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
    public ClientConnectionId ClientConnectionId { get; }
    public SessionId SessionId { get; }
    public UserId UserId { get; }
    public ServiceId ServiceId { get; }

    public bool HasRequest(RequestId requestId)
        => Connection.HasRequest(requestId);

    // FabricClient => SignalRConnection
    public Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct) 
        => Connection.Send_SendRequest_ToClientAsync(message, ct);
    public Task Send_SendRequestCancelled_ToClient_Async(SendRequestCancelledDto message, CancellationToken ct)
        => Connection.Send_SendRequestCancelled_ToClientAsync(message, ct);
    public Task SendStreamingRequestAsync(StreamingRequestDto request, CancellationToken ct)
        => Connection.Send_StreamingRequest_ToClientAsync(request, ct);
    public Task SendStreamingResponseAsync(StreamingResponseDto response, CancellationToken ct)
        => Connection.Send_StreamingResponse_ToClientAsync(response, ct);
    public IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct)
        => Connection.Send_InvokeRequest_ToClientAsync(request, ct);
    public Task Send_InvokeCancelled_ToClient_Async(InvokeRequestCancelledDto request, CancellationToken ct)
        => Connection.Send_InvokeCancelled_ToClientAsync(request, ct);

    // SignalRConnection => FabricClient
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