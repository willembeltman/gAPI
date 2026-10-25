using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.Server.Config;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.ServiceBus.Fabric;

public abstract class BaseFabricConnection(
    ILoggerFactory loggerFactory,
    BusFabricClient fabricClient,
    ServiceBusConfig busConfig)
    : IBaseFabricConnection
{
    public bool Initialized => fabricClient.IsConnected;
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

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
    public IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return fabricClient.RegisterBusRemoteAsyncEnumerableArgument<T>(routing, argumentIndex, deserializer);
        //return new RemoteAsyncEnumerable<T>(
        //    construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
        //    {
        //        var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
        //        BusStreamingResponseHandlers.TryAdd(key, response =>
        //        {
        //            if (response.IsCancelled)
        //            {
        //                enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
        //                return;
        //            }

        //            if (response.ExceptionMessage != null)
        //            {
        //                enumerator.Complete(new RemoteException(response.ExceptionMessage));
        //                return;
        //            }

        //            if (response.IsCompleted)
        //            {
        //                enumerator.Complete();
        //                return;
        //            }

        //            try
        //            {
        //                enumerator.Push(deserializer.Invoke(response.BinaryData));
        //            }
        //            catch (Exception ex)
        //            {
        //                enumerator.Complete(ex);
        //            }
        //        });
        //    },

        //    requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
        //    {
        //        return Send_BusStreamingResponse_ToFabricAsync(
        //            routing,
        //            argumentIndex,
        //            streamId,
        //            false,
        //            ct);
        //    },

        //    cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
        //    {
        //        return Send_BusStreamingResponse_ToFabricAsync(
        //            routing,
        //            argumentIndex,
        //            streamId,
        //            true,
        //            default);
        //    },

        //    dispose: (StreamId streamId) =>
        //    {
        //        var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
        //        BusStreamingResponseHandlers.TryRemove(key, out _);
        //    });
    }
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    {
        fabricClient.RegisterBusAsyncEnumerableArgument<T>(routing, argumentIndex, source, serializer, cts);
        //BusStreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new BusAsyncEnumerableRegistration<T>(async (
        //    ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> activeStreams,
        //    StreamId streamId,
        //    bool cancelled,
        //    CancellationToken ct) =>
        //{
        //    var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
        //        streamId,
        //        _ =>
        //        {
        //            var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
        //            return new BusAsyncEnumerableRegistrationInstance<T>(
        //                source.GetAsyncEnumerator(linked.Token),
        //                new SemaphoreSlim(1, 1),
        //                linked);
        //        });

        //    async Task Cleanup(Exception? ex = null)
        //    {
        //        activeStreams.TryRemove(streamId, out _);
        //        await enumerator.DisposeAsync();
        //        linkedCts.Dispose();
        //        if (argumentIndex == -1)
        //        {
        //            await fabricClient.Send_BusInvokeRequestDone_ToFabricAsync(new BusInvokeRequestDoneDto(routing, false, ex == null ? null : ex.Message), ct);
        //        }
        //    }

        //    var entered = false;
        //    try
        //    {
        //        await gate.WaitAsync(ct);
        //        entered = true;

        //        var hasNext = !cancelled && await enumerator.MoveNextAsync();

        //        var response = new BusStreamingResponseDto(
        //            routing,
        //            argumentIndex,
        //            streamId,
        //            !hasNext,
        //            cancelled,
        //            null,
        //            hasNext ? serializer(enumerator.Current) : []);

        //        if (!hasNext)
        //            await Cleanup();

        //        return response;
        //    }
        //    catch (OperationCanceledException ex) when (
        //        ct.IsCancellationRequested ||
        //        cts.IsCancellationRequested)
        //    {
        //        await Cleanup(ex);

        //        return new BusStreamingResponseDto(
        //            routing,
        //            argumentIndex,
        //            streamId,
        //            true,
        //            true,
        //            ex.Message,
        //            []);
        //    }
        //    catch (Exception ex)
        //    {
        //        await Cleanup(ex);

        //        return new BusStreamingResponseDto(
        //            routing,
        //            argumentIndex,
        //            streamId,
        //            true,
        //            false,
        //            ex.Message,
        //            []);
        //    }
        //    finally
        //    {
        //        if (entered)
        //            gate.Release();
        //    }
        //}));
    }
    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        await fabricClient.UnRegisterBusAsyncEnumerableArgument(routing, argumentIndex);

        //if (BusStreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        //{
        //    await registration.DisposeAsync();
        //}
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
