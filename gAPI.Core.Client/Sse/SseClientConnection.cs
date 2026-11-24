using gAPI.Core.Client.Collections;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace gAPI.Core.Client.Sse;

public class SseClientConnection : IDisposable
{
    public SseClientConnection(
        ILoggerFactory loggerFactory,
        IClientAuthenticatedHttpClient clientAuthenticatedHttpClient,
        SseManagerCollection sseManagerCollection)
    {
        LoggerFactory = loggerFactory;
        ClientAuthenticatedHttpClient = clientAuthenticatedHttpClient;
        SseManagerCollection = sseManagerCollection;
        Id = SseManagerCollection.Add(this);
    }

    public readonly ILoggerFactory LoggerFactory;
    public readonly IClientAuthenticatedHttpClient ClientAuthenticatedHttpClient;
    public readonly SseManagerCollection SseManagerCollection;
    public readonly ConcurrentDictionary<ServiceId, SseClientSubscription> ServiceClients = [];
    public CancellationTokenSource? Cts = new();
    public SseManagerId Id { get; protected set; }
    public bool Initialized { get; set; }

    public void Start(CancellationToken ct)
    {

    }

    public void Dispose()
    {
        if (Cts == null)
            return;
        Cts.Cancel();
        Cts.Dispose();
        Cts = null;
        foreach (var client in ServiceClients.Values)
        {
            client.Dispose();
        }
        SseManagerCollection.Remove(Id);
    }
}
