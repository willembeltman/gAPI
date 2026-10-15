using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.ServiceBus.Config;
using gAPI.Core.ServiceBus.Helpers;
using gAPI.Core.ServiceBus.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace gAPI.Core.ServiceBus.Fabric;

public abstract class BaseFabricConnection(
    IFabricClient fabricClient,
    ServiceBusConfig busConfig) 
    : IBaseBusConnection
{
    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    public bool Initialized { get; private set; }
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

    // Calling this will route the request to service
    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

    // This is called everytime something wants to subscribe
    public async Task TryConnectAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    // This is called AFTER the service has been unregistered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected async Task SendUnsubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    // This is called when a service has to be called through the service bus
    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        yield break;
    }
    private async Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

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
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new AsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<T>(
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
                cancellationToken.IsCancellationRequested)
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
}
