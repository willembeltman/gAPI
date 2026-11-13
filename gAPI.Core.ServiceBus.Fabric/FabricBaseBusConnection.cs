using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.ServiceBus.Fabric;

public abstract class FabricBaseBusConnection(
    IHostApplicationLifetime appLifetime,
    ILoggerFactory loggerFactory,
    BusFabricClient fabricClient,
    ServiceBusConfig config)
    : IFabricBaseBusConnection
{
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();
    public ServiceBusConfig Config { get; } = config;

    public void Start(CancellationToken ct)
    {
        fabricClient.Start(this, ct);
    }

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task SendSubscribeAsync(object implementation, ServiceId serviceId, CancellationToken ct)
        => fabricClient.BusSubscribeAsync(this, implementation, serviceId, ct);
    // This is called AFTER the service has been unregistered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task SendUnsubscribeAsync(object implementation, ServiceId serviceId, CancellationToken ct)
        => fabricClient.BusUnsubscribeAsync(this, implementation, serviceId, ct);

    // This is called when a service has to be called through the service bus
    public Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data)
        => fabricClient.SendFireAndForgetAsync(this, new(routing, data));
    public Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
        => fabricClient.SendAsync(this, new(routing, data), cts);
    public IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
        => fabricClient.InvokeAsync(this, new(routing, data), cts);

    // Therse functions are called when there are Enumerable arguments 
    public void RegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts)
        => fabricClient.RegisterTimeout(routing, cts);
    public void UnRegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts)
        => fabricClient.UnRegisterTimeout(routing, cts);

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer) 
        => fabricClient.RegisterBusRemoteAsyncEnumerableArgument<T>(routing, argumentIndex, deserializer);
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose cts)
        => fabricClient.RegisterBusAsyncEnumerableArgument<T>(routing, argumentIndex, source, serializer, cts);
    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex) 
        => await fabricClient.UnRegisterBusAsyncEnumerableArgument(routing, argumentIndex);

    // Callbacks from fabric (to the actual services)
    public abstract Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    public abstract Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

    // Calling this will route the request to service
    public abstract Task Send_FireAndForgetRequest_ToServiceAsync(BusSendRequestDto requestDto, CancellationToken ct);
    public abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    public abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

}
