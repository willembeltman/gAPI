using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.BackPlane.Fabric;

public abstract class FabricBasePlaneConnection(
#pragma warning disable CS9113 // Parameter is unread.
    IHostApplicationLifetime appLifetime,
    ILoggerFactory loggerFactory,
#pragma warning restore CS9113 // Parameter is unread.
    PlaneFabricClient fabricClient,
    BackPlaneConfig config)
    : IFabricBasePlaneConnection
{
    public PlaneConnectionId PlaneConnectionId { get; private set; } = PlaneConnectionId.New();
    public BackPlaneConfig Config { get; } = config;

    public void Start(CancellationToken ct)
    {
        fabricClient.Start(this, ct);
    }

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task<ServiceSubscriptionId> SendSubscribeAsync(object implementation, ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
        => fabricClient.PlaneSubscribeAsync(this, implementation, serviceId, serviceSubscriptionId, ct);
    // This is called AFTER the service has been unregistered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected Task SendUnsubscribeAsync(object implementation, ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
        => fabricClient.PlaneUnsubscribeAsync(this, implementation, serviceId, serviceSubscriptionId, ct);

    // This is called when a service has to be called through the service bus
    public Task SendFireAndForgetAsync(PlaneRoutingDto routing, byte[] data, CancellationToken ct)
        => fabricClient.SendFireAndForgetAsync(this, new(routing, data), ct);
    public Task SendAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
        => fabricClient.SendAsync(this, new(routing, data), cts);
    public IAsyncEnumerable<byte[]> InvokeAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts, CancellationToken ct)
        => fabricClient.InvokeAsync(this, new(routing, data), cts, ct);

    // Therse functions are called when there are Enumerable arguments 
    public void RegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
        => fabricClient.RegisterTimeout(routing, cts);
    public void UnRegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
        => fabricClient.UnRegisterTimeout(routing, cts);

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer, CancellationTokenSourceDispose timeoutCts) 
        => fabricClient.RegisterPlaneRemoteAsyncEnumerableArgument<T>(routing, argumentIndex, deserializer, timeoutCts);
    public void RegisterAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timeoutCts)
        => fabricClient.RegisterPlaneAsyncEnumerableArgument<T>(routing, argumentIndex, source, serializer, timeoutCts);
    public async Task UnRegisterAsyncEnumerableArgument(PlaneRoutingDto routing, int argumentIndex) 
        => await fabricClient.UnRegisterPlaneAsyncEnumerableArgument(routing, argumentIndex);

    // Callbacks from fabric (to the actual services)
    public abstract Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    public abstract Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

    // Calling this will route the request to service
    public abstract Task Send_FireAndForgetRequest_ToServiceAsync(PlaneSendRequestDto requestDto, CancellationToken ct);
    public abstract Task Send_SendRequest_ToServiceAsync(PlaneSendRequestDto sendRequest, CancellationTokenSourceDispose timeoutCts);
    public abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(PlaneInvokeRequestDto invokeRequest, CancellationTokenSourceDispose timeoutCts, CancellationToken ct);

}
