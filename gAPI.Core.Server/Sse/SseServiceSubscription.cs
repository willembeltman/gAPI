using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
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
        ServiceSubscriptionId = serviceSubscriptionCollection.Add(this);
        ClientConnectionId = serverConnectionCollection.AddConnection(this);
    }

    public async Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto sendRequest, CancellationToken ct)
    {
        var sseEvent = new SseEvent(sendRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
        return new SendRequestDoneDto(
            sendRequest.Routing,
            false,
            null,
            null);
    }

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        ServiceSubscriptionId = ServiceSubscriptionCollection.Add(this);
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
            }
        }
    }

    IAsyncEnumerable<StreamingResponseDto> IServiceSubscription.Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct)
    {
        throw new NotSupportedException(
            "You cannot use methods that have return types for SSE, " +
            "it also should be impossible to get here so kuddo's for the hacky bug.");
    }

    public bool HasRequest(RequestId requestId) => false;

    public Task SendStreamingRequestAsync(StreamingRequestDto request, CancellationToken ct)
        => throw new NotSupportedException();

    public Task SendStreamingResponseAsync(StreamingResponseDto response, CancellationToken ct)
        => throw new NotSupportedException();

}