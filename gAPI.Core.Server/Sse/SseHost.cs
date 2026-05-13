using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Sse;

public class SseHost(
    SseHostCollection sseHostCollection,
    FabricClient fabricClient,
    ServiceId serviceId,
    UserId userId,
    SessionId sessionId) : ISseHost
{
    private byte closed;

    public Channel<SseEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    public SseHostId Id { get; private set; }
    public ServiceId ServiceId { get; } = serviceId;
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;

    public async Task SendAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        var sseEvent = new SseEvent(sendRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        Id = sseHostCollection.Add(this);
        //Console.WriteLine($"SseHost {Id} started");
        await fabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(Id.Value.ToString(), "SseHostId");

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
                sseHostCollection.Remove(Id);
            }
        }
    }

    IAsyncEnumerable<InvokeResponseDto> ISseHost.InvokeAsync(InvokeRequestDto request, CancellationToken ct)
    {
        throw new NotSupportedException(
            "You cannot use methods that have return types for SSE, " +
            "it also should be impossible to get here so kuddo's for the hacky bug.");
    }
}