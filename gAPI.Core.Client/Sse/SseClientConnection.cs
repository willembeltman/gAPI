using gAPI.Core.Client.Collections;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Client.Sse;

public class SseClientConnection : IDisposable
{
    public SseClientConnection(
        IClientAuthenticatedHttpClient clientAuthenticatedHttpClient,
        SseManagerCollection sseManagerCollection)
    {
        ClientAuthenticatedHttpClient = clientAuthenticatedHttpClient;
        SseManagerCollection = sseManagerCollection;
        Id = SseManagerCollection.Add(this);
    }
    public readonly IClientAuthenticatedHttpClient ClientAuthenticatedHttpClient;
    public readonly SseManagerCollection SseManagerCollection;
    public readonly ConcurrentDictionary<ServiceId, SseClient> ServiceClients = [];
    public readonly CancellationTokenSource Cts = new();
    public SseManagerId Id { get; protected set; }
    public bool Initialized { get; set; }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        foreach (var client in ServiceClients.Values)
        {
            client.Dispose();
        }
        SseManagerCollection.Remove(Id);
    }
}
