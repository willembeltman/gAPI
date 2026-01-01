using gAPI.Interfaces;
using gAPI.Types;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace gAPI.Sse;

public abstract class SseManagerBase : ISseManagerBase
{
    protected readonly IClientAuthenticationService ClientAuthenticationService;
    protected readonly SseManagerCollection SseManagerCollection;
    protected readonly CancellationTokenSource Cts = new();
    protected readonly ConcurrentDictionary<ServiceId, ISseClient> ServiceClients = new();

    public SseManagerId Id { get; }

    public SseManagerBase(
        IClientAuthenticationService clientAuthenticationService,
        SseManagerCollection sseManagerCollection)
    {
        ClientAuthenticationService = clientAuthenticationService;
        SseManagerCollection = sseManagerCollection;
        Id = SseManagerCollection.Add(this);
    }


    protected async Task AddClient(ServiceId serviceId, Func<ServiceId, ISseClient> Create)
    {
        ServiceClients.GetOrAdd(
            serviceId,
            serviceId => Create(serviceId));
    }

    protected async Task RemoveClient(ServiceId serviceId)
    {
        if (!ServiceClients.TryGetValue(serviceId, out var client))
            return;
        await client.DisposeAsync();
        ServiceClients.TryRemove(serviceId, out _);
    }


    public async ValueTask DisposeAsync()
    {
        Cts.Cancel();
        Cts.Dispose();
        foreach (var client in ServiceClients.Values)
        {
            await client.DisposeAsync();
        }
        SseManagerCollection.Remove(Id);
    }

    public abstract Task MessageReceived(SseMessage message);
}
