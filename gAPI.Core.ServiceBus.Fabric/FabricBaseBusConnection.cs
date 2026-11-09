using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.ServiceBus.Fabric;

public abstract class FabricBaseBusConnection(
    ILoggerFactory loggerFactory,
    BusFabricClient fabricClient,
    ServiceBusConfig config)
    : IFabricBaseBusConnection
{
    public bool Initialized => fabricClient.IsConnected;
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();
    public ServiceBusConfig Config { get; } = config;

    // This is called everytime something wants to subscribe
    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (!fabricClient.IsConnected)
        {
            await fabricClient.ConnectAsync();
        }
    }

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
        => fabricClient.BusSubscribeAsync(this, new(serviceId, BusConnectionId), ct);
    // This is called AFTER the service has been unregistered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task SendUnsubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
        => fabricClient.BusUnsubscribeAsync(this, new(serviceId, BusConnectionId), ct);

    // This is called when a service has to be called through the service bus
    public Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts)
        => fabricClient.BusSendFireAndForgetAsync(this, new(routing, data), cts);
    public Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts)
        => fabricClient.BusSendAsync(this, new(routing, data), cts);
    public IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts)
        => fabricClient.BusInvokeAsync(this, new(routing, data), cts);

    // Therse functions are called when there are Enumerable arguments 
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return fabricClient.RegisterBusRemoteAsyncEnumerableArgument<T>(routing, argumentIndex, deserializer);
    }
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    {
        fabricClient.RegisterBusAsyncEnumerableArgument<T>(routing, argumentIndex, source, serializer, cts);
    }
    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        await fabricClient.UnRegisterBusAsyncEnumerableArgument(routing, argumentIndex);
    }

    // Callbacks from fabric (to the actual services)
    public Task Receive_BusFireAndForget_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct)
        => Send_SendRequest_ToServiceAsync(requestDto, ct);
    public Task Receive_BusSendRequest_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct)
        => Send_SendRequest_ToServiceAsync(requestDto, ct);
    public IAsyncEnumerable<byte[]> Receive_BusInvokeRequest_FromFabricAsync(BusInvokeRequestDto requestDto, CancellationToken ct)
        => Send_InvokeRequest_ToServiceAsync(requestDto, ct);

    // Calling this will route the request to service
    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

}
