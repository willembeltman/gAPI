using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.ServiceBus.Config;
using gAPI.Core.ServiceBus.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace gAPI.Core.ServiceBus.Fabric;

public abstract class BaseFabricConnection(
    ILoggerFactory loggerFactory,
    IFabricClient fabricClient,
    ServiceBusConfig busConfig)
    : IBaseFabricConnection
{
    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    public bool Initialized => fabricClient.IsConnected;
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

    // Calling this will route the request to service
    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

    // This is called everytime something wants to subscribe
    public async Task TryConnectAsync(CancellationToken ct)
    {
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

    // Callbacks from fabric
    public Task ReceiveFireAndForgetAsync(BusSendRequestDto requestDto, CancellationToken ct)
    {
        return Send_SendRequest_ToServiceAsync(requestDto, ct);
    }
    public Task ReceiveSendRequestAsync(BusSendRequestDto requestDto, CancellationToken ct)
    {
        return Send_SendRequest_ToServiceAsync(requestDto, ct);
    }
    public IAsyncEnumerable<byte[]> ReceiveInvokeRequestAsync(BusInvokeRequestDto requestDto, CancellationToken ct)
    {
        return Send_InvokeRequest_ToServiceAsync(requestDto, ct);
    }

    public async Task ReceiveStreamingRequestAsync(BusStreamingRequestDto requestDto, CancellationToken ct)
    {
        var key = new RequestArgumentIndexDto(requestDto.Routing.RequestId, requestDto.ArgumentIndex);
        if (StreamingRequestHandlers.TryGetValue(key, out var responseHandler) == false)
            return;
        var response = await responseHandler.Invoke(requestDto.StreamId, requestDto.Cancelled, ct);
        await SendStreamingResponseAsync(response, ct);
    }
    public async Task ReceiveStreamingResponseAsync(BusStreamingResponseDto responseDto, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(responseDto.Routing.RequestId, responseDto.ArgumentIndex, responseDto.StreamId);
        if (StreamingResponseHandlers.TryGetValue(key, out var responseHandler) == false)
            return;
        responseHandler.Invoke(responseDto);
    }

    // Enumerable arguments
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
                        return;
                    }

                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(response.ExceptionMessage));
                        return;
                    }

                    if (response.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(response.BinaryData));
                    }
                    catch (Exception ex)
                    {
                        enumerator.Complete(ex);
                    }
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                return SendStreamingRequestAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return SendStreamingRequestAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    {
        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new BusAsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                    return new BusAsyncEnumerableRegistrationInstance<T>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });

            async Task Cleanup()
            {
                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }

            var entered = false;
            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && await enumerator.MoveNextAsync();

                var response = new BusStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    cancelled,
                    null,
                    hasNext ? serializer(enumerator.Current) : []);

                if (!hasNext)
                    await Cleanup();

                return response;
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                cts.IsCancellationRequested)
            {
                await Cleanup();

                return new BusStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    ex.Message,
                    []);
            }
            catch (Exception ex)
            {
                await Cleanup();

                return new BusStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    ex.Message,
                    []);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }
    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        if (StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    // Fabric proxy methods
    private Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
        => fabricClient.BusStreamingRequestAsync(this, new(routing, argumentIndex, streamId, cancelled), ct);
    private Task SendStreamingResponseAsync(BusStreamingResponseDto response, CancellationToken ct)
        => fabricClient.BusStreamingResponseAsync(this, response, ct);

}
