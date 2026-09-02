using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Sse;

public class SseServiceSubscription(
    ServiceSubscriptionCollection ServiceSubscriptionCollection,
    FabricClient fabricClient,
    ServiceId serviceId,
    UserId userId,
    SessionId sessionId) : IServiceSubscription
{
    private byte closed;

    public Channel<SseEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    public ServiceSubscriptionId Id { get; private set; }
    public ServiceId ServiceId { get; } = serviceId;
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;

    public async Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto sendRequest, CancellationToken ct)
    {
        var sseEvent = new SseEvent(sendRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
        return new SendRequestDoneDto(
            sendRequest.RequestId,
            sendRequest.ServiceId,
            sendRequest.MethodId,
            sendRequest.UserId,
            sendRequest.SessionId,
            false,
            null,
            null);
    }

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        Id = ServiceSubscriptionCollection.Add(this);
        //Console.WriteLine($"SseServiceSubscription {Id} started");
        await fabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(Id.Value.ToString(), "ServiceSubscriptionId");

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
                await fabricClient.UnsubscribeAsync(this, ct);
                ServiceSubscriptionCollection.Remove(Id);
            }
        }
    }

    IAsyncEnumerable<InvokeResponseDto> IServiceSubscription.Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct)
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