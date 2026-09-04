using gAPI.Core.Dtos;
using gAPI.Core.Ids;
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
        ServiceSubscriptionCollection serviceSubscriptionCollection,
        FabricClient fabricClient,
        ClientConnectionId clientConnectionId,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        ServiceSubscriptionCollection = serviceSubscriptionCollection;
        Connection = connection;
        FabricClient = fabricClient;
        ClientConnectionId = clientConnectionId;
        SessionId = sessionId;
        UserId = userId;
        ServiceId = serviceId;

        ServiceSubscriptionId = ServiceSubscriptionCollection.Add(this);
        Logger = loggerFactory.CreateLogger<WssServiceSubscription>();
    }

    private byte Disposed;

    public ILogger Logger { get; }
    public IWssServerConnection Connection { get; }
    public ServiceSubscriptionCollection ServiceSubscriptionCollection { get; }
    public FabricClient FabricClient { get; }
    public ClientConnectionId ClientConnectionId { get; }
    public ServiceSubscriptionId ServiceSubscriptionId { get; }
    public ServiceId ServiceId { get; }
    public UserId UserId { get; }
    public SessionId SessionId { get; }

    // FabricClient => SignalRConnection
    public Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct) 
        => Connection.Send_SendRequest_ToClientAsync(message, ct);
    public IAsyncEnumerable<StreamingResponseDto> Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct)
        => Connection.Send_InvokeRequest_ToClientAsync(request, ct);
    public Task SendStreamingRequestAsync(StreamingRequestDto request, CancellationToken ct)
        => Connection.Send_StreamingRequest_ToClientAsync(request, ct);
    public Task SendStreamingResponseAsync(StreamingResponseDto response, CancellationToken ct)
        => Connection.Send_StreamingResponse_ToClientAsync(response, ct);


    //public bool HasRequest(RequestId requestId)
    //    => Connection.HasRequest(requestId);


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