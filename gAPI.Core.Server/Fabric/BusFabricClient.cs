using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Numerics;

namespace gAPI.Core.Server.Fabric;

public sealed class BusFabricClient
{
    public BusFabricClient(
        SessionCache sessionCache,
        StreamingCache streamingCache,
        ILoggerFactory loggerFactory,
        ServiceBusConfig serviceBusConfig)
    {
        Sender = new BusFabricClientSender(this, loggerFactory);

        StreamingCache = streamingCache;
        Logger = loggerFactory.CreateLogger<FabricClient>();
        Config = serviceBusConfig;

        if (!string.IsNullOrEmpty(serviceBusConfig.ConnectionString))
        {
            if (serviceBusConfig.ConnectionString.Contains('='))
            {
                var parts = serviceBusConfig.ConnectionString!.Split(';')
                    .Where(x => x.Contains('='))
                    .Select(x => x.Split(['='], 2))
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);

                if (!parts.TryGetValue("Server", out var host))
                    throw new Exception("Fabric ConnectionString must contain 'Server' parameter");
                if (!parts.TryGetValue("Port", out var portString))
                    throw new Exception("Fabric ConnectionString must contain 'Port' parameter");
                if (!int.TryParse(portString, out var port))
                    throw new Exception("Fabric ConnectionString 'Port' parameter must be an int");

                Host = host;
                Port = port;
            }
            else if (Uri.TryCreate(serviceBusConfig.ConnectionString, UriKind.Absolute, out var uri))
            {
                Host = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 9000;
            }
            else if (serviceBusConfig.ConnectionString.Contains(':'))
            {
                var split = serviceBusConfig.ConnectionString.Split(':');
                Host = split[0].Trim();
                Port = int.TryParse(split[1].Trim(), out var p) ? p : 9000;
            }
            else
            {
                Host = serviceBusConfig.ConnectionString.Trim();
                Port = 9000;
            }
        }
        else
        {
            throw new Exception("Bus fabric config not set");
        }
    }

    readonly ILogger Logger;

    public ServiceBusConfig Config { get; }

    readonly string Host;
    readonly int Port;

    readonly BusFabricClientSender Sender;
    readonly StreamingCache StreamingCache;

    TcpClient? Tcp;
    NetworkStream? Stream;

    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricBusConnectionId FabricConnectionId { get; private set; } = new FabricBusConnectionId(-1);
    public bool IsConnected => Tcp?.Connected == true;
    public bool Initialized => ConnectedCts != null;

    private CancellationToken? LifeCycleCt;
    private IFabricBaseBusConnection? BusConnection;

    CancellationTokenSource? ConnectedCts;
    bool RunningRequested;
    SemaphoreSlim ConnectingGate = new SemaphoreSlim(1);

    public void Start(IFabricBaseBusConnection busConnection, CancellationToken ct)
    {
        LifeCycleCt = ct;
        BusConnection = busConnection;
        _ = Task.Run(ConnectAsync);
    }

    private async Task ConnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} ConnectAsync()", DateTime.Now.ToString("HH:mm:ss.fff"));

        if (BusConnection == null || LifeCycleCt == null)
            throw new Exception("Not initialized");

        await ConnectingGate.WaitAsync();
        try
        {
            await Gated_ConnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }

    private async Task Gated_ConnectAsync()
    {
        RunningRequested = true;
        while (RunningRequested == true && ConnectedCts == null)
        {
            try
            {
                Tcp = new TcpClient();
                Tcp.Connect(Host, Port);
                Stream = Tcp.GetStream();
                var writer = new BinaryWriter(Stream);
                var reader = new BinaryReader(Stream);

                ConnectedCts = new CancellationTokenSource();
                _ = Task.Run(async () => { await Sender.SendKernel(writer, ConnectedCts.Token); });
                _ = Task.Run(async () => { await TryStartReceiveKernelElseReconnectAsync(reader, ConnectedCts.Token); });

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ConnectAsync() Connected!", DateTime.Now.ToString("HH:mm:ss.fff"));
            }
            catch
            {
                try
                {
                    ConnectedCts?.Cancel();
                    ConnectedCts?.Dispose();
                    ConnectedCts = null;
                    Stream?.Dispose();
                    Tcp?.Dispose();
                }
                catch { }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    private async Task TryStartReceiveKernelElseReconnectAsync(BinaryReader binaryReader, CancellationToken ct)
    {
        try
        {
            await ReceiveKernel(binaryReader, ct);
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(
                    "FabricClient #{Id.Value}: Exception occured, restarting fabric client\r\n{ex}",
                    FabricConnectionId?.Value,
                    ex);
        }

        if (RunningRequested)
            await ReconnectAsync();
    }

    private async Task ReconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError("{now} Reconnecting FabricClient ....",
                DateTime.Now.ToString("HH:mm:ss.fff"));

        await ConnectingGate.WaitAsync();
        try
        {
            await Gated_ReconnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_ReconnectAsync()
    {
        if (BusConnection == null || LifeCycleCt == null)
            throw new Exception("Not initialized");

        await Gated_DisconnectAsync();
        await BusConnection.OnDisconnectedAsync(LifeCycleCt.Value);

        await Gated_ConnectAsync();
        await BusConnection.OnRestoreSubscriptionsAsync(LifeCycleCt.Value);

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Reconnecting FabricClient DONE", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting FabricClient {Id}",
                FabricConnectionId);

        await ConnectingGate.WaitAsync();
        try
        {
            RunningRequested = false;
            await Gated_DisconnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_DisconnectAsync()
    {
        RunningRequested = false;
        if (ConnectedCts == null)
            return;

        await ConnectedCts.CancelAsync();

        Stream?.Dispose();
        Stream = null;

        Tcp?.Dispose();
        Tcp = null;

        ConnectedCts.Dispose();
        ConnectedCts = null;
    }

    public async Task BusSubscribeAsync(IFabricBaseBusConnection busConnection, object implementation, BusSubscribeDto subscribe, CancellationToken ct)
    {
        if (Initialized == false)
            await ConnectAsync();

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "BusSubscribeAsync({baseBusConnection},{subscribe})",
                busConnection,
                subscribe);

        await Sender.Send_BusSubscribe_ToFabricAsync(subscribe, ct);

        if (implementation is IBusServiceSubscription implementationSubscription)
        {
            await implementationSubscription.OnSubscribedAsync(subscribe.ServiceId, ct);
        }
    }
    public async Task BusUnsubscribeAsync(IFabricBaseBusConnection busConnection, object implementation, BusUnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Initialized == false)
            await ConnectAsync();

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({baseBusConnection}, {unsubscribe})",
                busConnection,
                unsubscribe);

        await Sender.Send_BusUnsubscribe_ToFabricAsync(unsubscribe, ct);

        if (implementation is IBusServiceSubscription implementationSubscription)
        {
            await implementationSubscription.OnUnsubscribedAsync(unsubscribe.ServiceId, ct);
        }
    }

    private async Task ReceiveKernel(BinaryReader binaryReader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var messageType = BusFabricConverter.ReadHostToClientMessageType(binaryReader);

            //if (Logger.IsEnabled(LogLevel.Trace))
            //    Logger.LogTrace("{now} ReceiveKernel(messageType = {messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

            switch (messageType)
            {
                case BusFabricHostToClientMessageEnum.SynchronizeFabricIds:
                    var synchronizeFabricIds = binaryReader.ReadSynchronizeBusFabricIdsDto(Config.MaxPackageSize ?? 512 * 1024);
                    await Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                    break;

                case BusFabricHostToClientMessageEnum.BusFireAndForget:
                    var busFireAndForget = binaryReader.ReadBusSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusFireAndForget_FromFabricAsync(busFireAndForget, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusFireAndForgetCancelled:
                    var busFireAndForgetCancelled = binaryReader.ReadBusSendRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusFireAndForgetCancelled_FromFabricAsync(busFireAndForgetCancelled, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusSendRequest:
                    var busSendRequest = binaryReader.ReadBusSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusSendRequest_FromFabricAsync(busSendRequest, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusSendRequestCancelled:
                    var busSendRequestCancelled = binaryReader.ReadBusSendRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusSendRequestCancelled_FromFabricAsync(busSendRequestCancelled, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusSendRequestDone:
                    var busSendRequestAck = binaryReader.ReadBusSendRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusSendRequestAck_FromFabricAsync(busSendRequestAck, ct);
                    }, ct);
                    break;

                case BusFabricHostToClientMessageEnum.BusInvokeRequest:
                    var busInvokeRequest = binaryReader.ReadBusInvokeRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusInvokeRequest_FromFabricAsync(busInvokeRequest, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusInvokeRequestCancelled:
                    var busInvokeRequestCancelled = binaryReader.ReadBusInvokeRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusInvokeRequestCancelled_FromFabricAsync(busInvokeRequestCancelled, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusInvokeRequestAck:
                    var busInvokeResponseAck = binaryReader.ReadBusInvokeRequestAckDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusInvokeRequestAck_FromFabricAsync(busInvokeResponseAck, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusInvokeRequestDone:
                    var busInvokeResponseDone = binaryReader.ReadBusInvokeRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusInvokeRequestDone_FromFabricAsync(busInvokeResponseDone, ct);
                    }, ct);
                    break;

                case BusFabricHostToClientMessageEnum.BusStreamingRequest:
                    var busStreamingRequest_ResponseEnumerable = binaryReader.ReadBusStreamingRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusStreamingRequest_FromFabricAsync(busStreamingRequest_ResponseEnumerable, ct);
                    }, ct);
                    break;
                case BusFabricHostToClientMessageEnum.BusStreamingResponse:
                    var busStreamingResponse_ResponseEnumerable = binaryReader.ReadBusStreamingResponseDto(Config.MaxPackageSize ?? 512 * 1024);
                    _ = Task.Run(async () =>
                    {
                        await Receive_BusStreamingResponse_FromFabricAsync(busStreamingResponse_ResponseEnumerable, ct);
                    }, ct);
                    break;

            }
        }
    }

    private async Task Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizeBusFabricIdsDto synchronizeFabricIds, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeFabricIds);

        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;

        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} FabricClient {FabricConnectionId.Value} started");
    }

    private async Task Receive_BusFireAndForget_FromFabricAsync(BusSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusFireAndForgetAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (BusConnection == null)
            throw new Exception("Not started");

        using var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            await BusConnection.Send_FireAndForgetRequest_ToServiceAsync(message, ct);
        }
        finally
        {
            StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
        }
    }
    private async Task Receive_BusFireAndForgetCancelled_FromFabricAsync(BusSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusFireAndForgetCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        cts.Cancel();
    }

    private async Task Receive_BusSendRequest_FromFabricAsync(BusSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusSendRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (BusConnection == null)
            throw new Exception("Not started");

        using var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            await BusConnection.Send_SendRequest_ToServiceAsync(message, ct);
            await Sender.Send_BusSendRequestDone_ToFabricAsync(new(message.Routing, false, null), ct);
        }
        catch (TaskCanceledException ex)
        {
            await Sender.Send_BusSendRequestDone_ToFabricAsync(new(message.Routing, true, ex.Message), ct);
        }
        catch (Exception ex)
        {
            await Sender.Send_BusSendRequestDone_ToFabricAsync(new(message.Routing, false, ex.Message), ct);
        }
        finally
        {
            StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
        }
    }
    private async Task Receive_BusSendRequestCancelled_FromFabricAsync(BusSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusSendRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        cts.Cancel();
    }
    private async Task Receive_BusSendRequestAck_FromFabricAsync(BusSendRequestDoneDto request, CancellationToken ct)
    {
        if (StreamingCache.PendingBusSendRequests.TryRemove(request.Routing, out var complete) == false)
            return;

        if (request.ExceptionMessage == null)
            complete.SetResult(request);
        else
            complete.SetException(new RemoteException(request.ExceptionMessage));
    }

    private async Task Receive_BusInvokeRequest_FromFabricAsync(BusInvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusInvokeRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (BusConnection == null)
            throw new Exception("Not started");

        var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            var enumerable = BusConnection.Send_InvokeRequest_ToServiceAsync(message, ct);
            RegisterBusAsyncEnumerableArgument(message.Routing, -1, enumerable, b => b, cts);
            await Sender.Send_BusInvokeRequestAck_ToFabricAsync(new(message.Routing), ct);
        }
        catch (TaskCanceledException ex)
        {
            await Sender.Send_BusSendRequestDone_ToFabricAsync(new(message.Routing, true, ex.Message), ct);
        }
        catch (Exception ex)
        {
            await Sender.Send_BusSendRequestDone_ToFabricAsync(new(message.Routing, false, ex.Message), ct);
        }
    }
    private async Task Receive_BusInvokeRequestCancelled_FromFabricAsync(BusInvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        cts.Cancel();
    }
    private async Task Receive_BusInvokeRequestAck_FromFabricAsync(BusInvokeRequestAckDto request, CancellationToken ct)
    {
        if (StreamingCache.PendingBusInvokeRequests.TryRemove(request.Routing, out var complete) == false)
            return;

        complete.SetResult(request);
    }
    private async Task Receive_BusInvokeRequestDone_FromFabricAsync(BusInvokeRequestDoneDto request, CancellationToken ct)
    {
        if (StreamingCache.PendingBusInvokeRequests.TryRemove(request.Routing, out var complete))
        {
            if (request.Cancelled)
                complete.SetCanceled(ct);
            else if (request.ExceptionMessage != null)
                complete.SetException(new RemoteException(request.ExceptionMessage));
            else
                complete.SetCanceled(ct); // hij zit te wachten op beginnen met streamen, maar request is al done.
        }

        // TODO Opruimen streaming spul
    }

    private async Task Receive_BusStreamingRequest_FromFabricAsync(BusStreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusStreamingRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingCache.BusStreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            try
            {
                var response = await handler.Invoke(message.StreamId, false, ct);
                await Sender.Send_BusStreamingResponse_ToFabricAsync(response, ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Receive_BusStreamingRequest_FromFabricAsync({message})", message);
            }
        }
        else
        {
            await Sender.Send_BusStreamingResponse_ToFabricAsync(new(message.Routing, message.ArgumentIndex, message.StreamId, true, false, "No StreamingRequestHandlers found", []), ct);
        }
    }
    private async Task Receive_BusStreamingResponse_FromFabricAsync(BusStreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusStreamingResponse_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
        if (StreamingCache.BusStreamingResponseHandlers.TryGetValue(key, out var responseHandler))
        {
            responseHandler.Invoke(message);
        }
    }

    public void RegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        StreamingCache.Timeouts[routing.RequestId] = cts;
    }
    public void UnRegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        StreamingCache.Timeouts.TryRemove(routing.RequestId, out _);
    }

    public IAsyncEnumerable<T> RegisterBusRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
            timeOut: routing.Timeout,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.BusStreamingResponseHandlers.TryAdd(key, response =>
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
                return Sender.Send_BusStreamingRequest_ToFabricAsync(
                    new(routing,
                    argumentIndex,
                    streamId,
                    false),
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return Sender.Send_BusStreamingRequest_ToFabricAsync(
                    new(routing,
                    argumentIndex,
                    streamId,
                    true),
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.BusStreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    public void RegisterBusAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timeoutCts)
    {
        StreamingCache.BusStreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new BusAsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
                    return new BusAsyncEnumerableRegistrationInstance<T>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });

            async Task Cleanup(Exception? ex = null)
            {
                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
                if (argumentIndex == -1)
                {
                    await Sender.Send_BusInvokeRequestDone_ToFabricAsync(new BusInvokeRequestDoneDto(routing, false, ex == null ? null : ex.Message), ct);
                }
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
                timeoutCts.IsCancellationRequested)
            {
                await Cleanup(ex);

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
                await Cleanup(ex);

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
    public async Task UnRegisterBusAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        if (StreamingCache.BusStreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    public RemoteAsyncEnumerable<byte[]> RegisterBusRemoteAsyncEnumerableArgumentByte(BusRoutingDto routing, int argumentIndex)
    {
        return new RemoteAsyncEnumerable<byte[]>(
            timeOut: routing.Timeout,
            construct: (streamId, enumerator, ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.BusStreamingResponseHandlers.TryAdd(key, response =>
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

                    enumerator.Push(response.BinaryData);
                });
            },

            requestNext: (streamId, enumerator, ct) =>
            {
                return Sender.Send_BusStreamingRequest_ToFabricAsync(
                    new BusStreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return Sender.Send_BusStreamingRequest_ToFabricAsync(
                    new BusStreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true),
                    default);

            },

            dispose: streamId =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.BusStreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    public async Task SendFireAndForgetAsync(IBaseBusConnection baseBusConnection, BusSendRequestDto request)
    {
        if (LifeCycleCt == null)
            throw new Exception("BusFabric not initilazed");

        await Sender.Send_BusSendAndForgetRequest_ToFabricAsync(request, LifeCycleCt.Value);
    }
    public async Task SendAsync(IBaseBusConnection baseBusConnection, BusSendRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;

        var complete = new TaskCompletionSource<BusSendRequestDoneDto>();

        if (StreamingCache.PendingBusSendRequests.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");

        timeoutCts.RegisterDispose(async () =>
        {
            StreamingCache.PendingBusSendRequests.TryRemove(request.Routing, out _);
        });

        await Sender.Send_BusSendRequest_ToFabricAsync(request, ct);
        timeoutCts.CancelAfter(request.Routing.Timeout);

        timeoutCts.Token.Register(async () =>
        {
            await Sender.Send_BusSendCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        var sendRequestAck = await complete.Task.WaitAsync(timeoutCts.Token);
        if (sendRequestAck.ExceptionMessage != null)
            throw new RemoteException(sendRequestAck.ExceptionMessage);
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(IBaseBusConnection baseBusConnection, BusInvokeRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;

        var complete = new TaskCompletionSource<BusInvokeRequestAckDto>();
        if (StreamingCache.PendingBusInvokeRequests.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");
        timeoutCts.RegisterDispose(() =>
        {
            StreamingCache.PendingBusInvokeRequests.TryRemove(request.Routing, out _);
        });

        await Sender.Send_BusInvokeRequest_ToFabricAsync(request, ct);
        timeoutCts.CancelAfter(request.Routing.Timeout);

        timeoutCts.Token.Register(async () =>
        {
            await Sender.Send_BusInvokeRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        try
        {
            var response = await complete.Task.WaitAsync(request.Routing.Timeout, ct);
            var enumerable = RegisterBusRemoteAsyncEnumerableArgumentByte(request.Routing, -1);
            await foreach (var item in enumerable)
            {
                timeoutCts.CancelAfter(request.Routing.Timeout);
                yield return item;
            }
        }
        finally
        {
            StreamingCache.PendingBusInvokeRequests.TryRemove(request.Routing, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Closing FabricClient {Id}",
                FabricConnectionId);
        }
        await DisconnectAsync();
        if (ConnectedCts != null)
        {
            await ConnectedCts.CancelAsync();
            ConnectedCts.Dispose();
        }
    }

}