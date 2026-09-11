using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Sse;

public class SseServiceSubscription : IServiceSubscription
    , IServerConnection
{
    private byte closed;
    private readonly ServiceSubscriptionCollection ServiceSubscriptionCollection;
    private readonly FabricClient FabricClient;
    private readonly CancellationTokenSource Cts = new();

    public Channel<SseEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    public ClientConnectionId ClientConnectionId { get; }
    public ServiceSubscriptionId ServiceSubscriptionId { get; private set; }
    public ServiceId ServiceId { get; }
    public SessionId SessionId { get; }
    public UserId UserId { get; }

    public SseServiceSubscription(
        ServerConnectionCollection serverConnectionCollection,
        ServiceSubscriptionCollection serviceSubscriptionCollection,
        FabricClient fabricClient,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        ServiceSubscriptionCollection = serviceSubscriptionCollection;
        FabricClient = fabricClient;
        ServiceId = serviceId;
        SessionId = sessionId;
        UserId = userId;
        ServiceSubscriptionId = serviceSubscriptionCollection.Add(this, serviceId);
        ClientConnectionId = serverConnectionCollection.AddConnection(this);
    }

    readonly ConcurrentDictionary<RequestId, int> Requests = [];

    public async Task SendRequestAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        var sseEvent = new SseEvent(sendRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        Requests.TryAdd(sendRequest.Routing.RequestId, 0);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;

        var sseEvent = new SseEvent(sendRequest);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;
        
        await Channel.Writer.WriteAsync(sseEvent, Cts.Token);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;
        
        if (Requests.TryRemove(sendRequest.Routing.RequestId, out _))
        {
            var done = new SendRequestDoneDto(sendRequest.Routing, false, null);
            await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(done, Cts.Token);
        }
    }
    public async Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Requests.TryRemove(cancel.Routing.RequestId, out _))
        {
            var sseEvent = new SseEvent(cancel);
            await Channel.Writer.WriteAsync(sseEvent, ct);
            var done = new SendRequestDoneDto(cancel.Routing, true, null);
            await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(done, ct);
        }
    }

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        //Console.WriteLine($"SseServiceSubscription {Id} started");
        await FabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(ServiceSubscriptionId.Value.ToString(), "ServiceSubscriptionId");

            while (true)
            {
                SseEvent sseMessage;
                try
                {
                    sseMessage = await Channel.Reader.ReadAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    yield break; // <- GEEN ERROR, normale shutdown
                }
                catch (ChannelClosedException)
                {
                    yield break;
                }

                if (sseMessage.EventData == null) continue;
                yield return new SseItem<string>(sseMessage.EventData, sseMessage.EventName);
            }
        }
        finally
        {
            if (Interlocked.Exchange(ref closed, 1) == 0)
            {
                await FabricClient.UnsubscribeAsync(this, ct);
                ServiceSubscriptionCollection.Remove(ServiceSubscriptionId);
                Cts.Dispose();
            }
        }
    }

    #region Not supported
    public IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto request, CancellationToken ct) => throw new NotImplementedException();
    public Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct) => throw new NotSupportedException();
    public Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct) => throw new NotSupportedException();
    public Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct) => throw new NotImplementedException();
    public Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct) => throw new NotImplementedException();
    public Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct) => throw new NotImplementedException();
    public Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct) => throw new NotImplementedException();
    #endregion

}