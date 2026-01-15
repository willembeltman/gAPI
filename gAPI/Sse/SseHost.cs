using gAPI.Ids;
using Newtonsoft.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using gAPI.Fabric;

namespace gAPI.Sse;

public class SseHost(
    SseHostCollection sseHostCollection,
    FabricClient fabricClient,
    SseServiceId serviceId,
    UserId userId,
    SessionId sessionId)
{
    private byte closed;

    public Channel<SseEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    public SseHostId Id { get; private set; }
    public SseServiceId ServiceId { get; } = serviceId;
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Id = sseHostCollection.Add(this);
        Console.WriteLine($"SseHost {Id} started");
        fabricClient.Subscribe(this);

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

                if (sseMessage.SseMessage == null) continue;

                var json = JsonConvert.SerializeObject(sseMessage.SseMessage);
                yield return new SseItem<string>(json, "SseMessage");
            }
        }
        finally
        {
            if (Interlocked.Exchange(ref closed, 1) == 0)
            {
                fabricClient.Unsubscribe(this);
                sseHostCollection.Remove(Id);
            }
        }
    }
}