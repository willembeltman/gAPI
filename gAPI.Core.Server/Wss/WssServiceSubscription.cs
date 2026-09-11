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

        ServiceSubscriptionId = ServiceSubscriptionCollection.Add(this, serviceId);
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

    public Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
        => Connection.Send_StreamingRequest_ToClientAsync(request, ct);
    public Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct)
        => Connection.Send_StreamingResponse_ToClientAsync(response, ct);

    public Task SendRequestAsync(SendRequestDto message, CancellationToken ct)
        => Connection.SendRequestAsync(message, ct);
    public IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto request, CancellationToken ct)
        => Connection.InvokeRequestAsync(request, ct);

    public Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
        => Connection.Send_FabricStreamingRequest_ToClientAsync(request, ct);
    public Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct)
        => Connection.Send_FabricStreamingResponse_ToClientAsync(response, ct);
    public Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
        => Connection.Send_FabricSendRequest_ToClientAsync(sendRequest, ct);
    public Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
        => Connection.Send_FabricInvokeRequest_ToClientAsync(invokeRequest, ct);
    public Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
        => Connection.Send_FabricInvokeRequestCancelled_ToClientAsync(cancel, ct);
    public Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
        => Connection.Send_FabricSendRequestCancelled_ToClientAsync(cancel, ct);

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