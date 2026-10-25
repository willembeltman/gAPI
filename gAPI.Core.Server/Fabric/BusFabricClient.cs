using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.Server.Wss;
using gAPI.Core.Sse;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

// Deze class houd alle communicatie consistentie in de gate, en routeert alles. Voor de WssServerConnection is dit gewoon het doorgeef luik "doe call naar client"
// De fabricReceiver is wel autonoom wat betreft de afhandeling richting fabric, dus als er een bericht terug moet naar de fabric doet hij dit zelf.
// De fabricSender is zo dom mogelijk
public sealed class BusFabricClient 
{
    public BusFabricClient(
        SessionCache sessionCache,
        StreamingCache streamingCache,
        BusConnectionCollection busConnections,
        ILoggerFactory loggerFactory,
        string? fabricConnectionString)
    {
        Sender = new BusFabricClientSender(this, loggerFactory);

        LocalSessionCache = sessionCache;
        StreamingCache = streamingCache;
        BusConnections = busConnections;
        Logger = loggerFactory.CreateLogger<FabricClient>();

        if (!string.IsNullOrEmpty(fabricConnectionString))
        {
            if (fabricConnectionString.Contains('='))
            {
                var parts = fabricConnectionString!.Split(';')
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
            else if (Uri.TryCreate(fabricConnectionString, UriKind.Absolute, out var uri))
            {
                Host = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 9000;
            }
            else if (fabricConnectionString.Contains(':'))
            {
                var split = fabricConnectionString.Split(':');
                Host = split[0].Trim();
                Port = int.TryParse(split[1].Trim(), out var p) ? p : 9000;
            }
            else
            {
                Host = fabricConnectionString.Trim();
                Port = 9000;
            }

            _ = Task.Run(ConnectAsync);
        }
        else
        {

        }
    }

    readonly ILogger Logger;

    readonly string? Host;
    readonly int? Port;

    readonly BusFabricClientSender Sender;
    readonly StreamingCache StreamingCache;
    readonly BusConnectionCollection BusConnections;
    readonly SessionCache LocalSessionCache;

    TcpClient? Tcp;
    NetworkStream? Stream;
    bool FirstTime;
    bool IsConnecting;
    bool IsDisconnecting;

    CancellationTokenSource? SenderCts;
    CancellationTokenSource? ReceiverCts;
    internal BinaryWriter? BinaryWriter;
    BinaryReader? BinaryReader;

    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricBusConnectionId FabricConnectionId { get; private set; } = new FabricBusConnectionId(-1);
    public bool IsConnected => IsDisconnecting || IsConnecting || Tcp?.Connected == true;

    #region Connection

    public async Task ConnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} ConnectAsync()", DateTime.Now.ToString("HH:mm:ss.fff"));

        if (Host == null || Port == null) return;
        if (IsConnected || IsDisconnecting) return;

        try
        {
            IsConnecting = true;

            SenderCts = new CancellationTokenSource();
            ReceiverCts = new CancellationTokenSource();
            Tcp = new TcpClient();
            Tcp.Connect(Host, Port.Value);
            Stream = Tcp.GetStream();
            BinaryReader = new BinaryReader(Stream);
            BinaryWriter = new BinaryWriter(Stream);

            if (!FirstTime)
            {
                FirstTime = true;
                _ = Task.Run(async () => { await Sender.SendKernel(SenderCts.Token); });
            }

            _ = Task.Run(async () => { await ReceiveKernel(SenderCts.Token); });

            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} ConnectAsync() Connected!", DateTime.Now.ToString("HH:mm:ss.fff"));
        }
        finally
        {
            IsConnecting = false;
        }
    }
    public async Task ReconnectAsync(CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError("{now} Reconnecting FabricClient ....", DateTime.Now.ToString("HH:mm:ss.fff"));

        await DisconnectAsync();
        await ConnectAsync();
        foreach (var service in BusConnections.Services)
        {
            foreach (var SseServiceSubscription in service.Value.Values)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(
                        "{now} Resubscribe IServiceSubscription {HostId}",
                        DateTime.Now.ToString("HH:mm:ss.fff"),
                        SseServiceSubscription.BusConnectionId);

                var request = new BusSubscribeDto(
                    service.Key,
                    SseServiceSubscription.BusConnectionId);
                await Sender.Send_BusSubscribe_ToFabricAsync(request, ct);
            }
        }
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Reconnecting FabricClient DONE", DateTime.Now.ToString("HH:mm:ss.fff"));
    }
    public async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting FabricClient {Id}",
                FabricConnectionId);

        if (IsConnecting) return;

        try
        {
            IsDisconnecting = true;

            if (ReceiverCts != null)
                await ReceiverCts.CancelAsync();
            ReceiverCts?.Dispose();
            ReceiverCts = null;

            BinaryReader?.Dispose();
            BinaryReader = null;

            BinaryWriter?.Dispose();
            BinaryWriter = null;

            Stream?.Dispose();
            Stream = null;

            Tcp?.Dispose();
            Tcp = null;
        }
        finally
        {
            IsDisconnecting = false;
        }
    }

    #endregion

    #region Subscription

    public async Task BusSubscribeAsync(IBaseFabricConnection baseBusConnection, BusSubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "BusSubscribeAsync({baseBusConnection},{subscribe})",
                baseBusConnection,
                subscribe);

        var service = BusConnections.Services.AddOrUpdate(
            subscribe.ServiceId,
            new ConcurrentDictionary<BusConnectionId, IBaseFabricConnection>(),
            (a, b) => b);
        service[baseBusConnection.BusConnectionId] = baseBusConnection;

        if (Host == null)
        {
            throw new Exception("Fabric is not initialized, missing Host setting");
        }

        var request = new BusSubscribeDto(
            subscribe.ServiceId,
            subscribe.BusConnectionId);
        await Sender.Send_BusSubscribe_ToFabricAsync(request, ct);
    }
    public async Task BusUnsubscribeAsync(IBaseFabricConnection baseBusConnection, BusUnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({baseBusConnection}, {unsubscribe})",
                baseBusConnection,
                unsubscribe);

        var service = BusConnections.Services.AddOrUpdate(
            unsubscribe.ServiceId,
            new ConcurrentDictionary<BusConnectionId, IBaseFabricConnection>(),
            (a, b) => b);
        service.TryRemove(baseBusConnection.BusConnectionId, out _);

        if (Host == null)
        {
            throw new Exception("Fabric is not initialized, missing Host setting");
        }

        var request = new BusUnsubscribeDto(
            unsubscribe.ServiceId,
            unsubscribe.BusConnectionId
        );
        await Sender.Send_BusUnsubscribe_ToFabricAsync(request, ct);
    }

    #endregion

    #region Receiver

    public async Task ReceiveKernel(CancellationToken ct)
    {
        if (BinaryReader == null) return;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var messageType = BusFabricConverter.ReadHostToClientMessageType(BinaryReader);

                //if (Logger.IsEnabled(LogLevel.Trace))
                //    Logger.LogTrace("{now} ReceiveKernel(messageType = {messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

                switch (messageType)
                {
                    case BusFabricHostToClientMessageEnum.SynchronizeFabricIds:
                        var synchronizeFabricIds = BinaryReader.ReadSynchronizeBusFabricIdsDto();
                        await Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                        break;

                    case BusFabricHostToClientMessageEnum.BusFireAndForget:
                        var busFireAndForget = BinaryReader.ReadBusSendRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusFireAndForget_FromFabricAsync(busFireAndForget, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusFireAndForgetCancelled:
                        var busFireAndForgetCancelled = BinaryReader.ReadBusSendRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusFireAndForgetCancelled_FromFabricAsync(busFireAndForgetCancelled, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusSendRequest:
                        var busSendRequest = BinaryReader.ReadBusSendRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusSendRequest_FromFabricAsync(busSendRequest, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusSendRequestCancelled:
                        var busSendRequestCancelled = BinaryReader.ReadBusSendRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusSendRequestCancelled_FromFabricAsync(busSendRequestCancelled, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusSendRequestDone:
                        var busSendRequestAck = BinaryReader.ReadBusSendRequestDoneDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusSendRequestAck_FromFabricAsync(busSendRequestAck, ct);
                        }, ct);
                        break;

                    case BusFabricHostToClientMessageEnum.BusInvokeRequest:
                        var busInvokeRequest = BinaryReader.ReadBusInvokeRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusInvokeRequest_FromFabricAsync(busInvokeRequest, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusInvokeRequestCancelled:
                        var busInvokeRequestCancelled = BinaryReader.ReadBusInvokeRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusInvokeRequestCancelled_FromFabricAsync(busInvokeRequestCancelled, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusInvokeRequestAck:
                        var busInvokeResponseAck = BinaryReader.ReadBusInvokeRequestAckDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusInvokeRequestAck_FromFabricAsync(busInvokeResponseAck, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusInvokeRequestDone:
                        var busInvokeResponseDone = BinaryReader.ReadBusInvokeRequestDoneDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusInvokeRequestDone_FromFabricAsync(busInvokeResponseDone, ct);
                        }, ct);
                        break;

                    case BusFabricHostToClientMessageEnum.BusStreamingRequest:
                        var busStreamingRequest_ResponseEnumerable = BinaryReader.ReadBusStreamingRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusStreamingRequest_FromFabricAsync(busStreamingRequest_ResponseEnumerable, ct);
                        }, ct);
                        break;
                    case BusFabricHostToClientMessageEnum.BusStreamingResponse:
                        var busStreamingResponse_ResponseEnumerable = BinaryReader.ReadBusStreamingResponseDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusStreamingResponse_FromFabricAsync(busStreamingResponse_ResponseEnumerable, ct);
                        }, ct);
                        break;

                }
            }
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    "FabricClient #{Id.Value}: Exception occured, restarting fabric client\r\n{ex}",
                    FabricConnectionId?.Value,
                    ex);
            }
        }

        await ReconnectAsync(ct); // Letop deze moet naar boven
    }

    #region Bus
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

        using var cts = new LinkedCancellationTokenSourceWithTimeout(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            // Routeren naar client(s)
            var connections = GetBusConnections(message.Routing);
            foreach (var connection in connections)
            {
                await connection.Receive_BusFireAndForget_FromFabricAsync(message, cts.Token);
                cts.Reset();
            }
        }
        finally
        {
            StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
        }

        //// Routeren naar client(s)
        //var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        //foreach (var serviceSubscription in SseServiceSubscriptions)
        //{
        //    await serviceSubscription.SendFireAndForgetAsync(message.Routing, message.BinaryData, ct);
        //}
    }
    private async Task Receive_BusFireAndForgetCancelled_FromFabricAsync(BusSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusFireAndForgetCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        cts.Cancel();

        //// Routeren naar client(s)
        //var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
        //foreach (var serviceSubscription in serviceSubscriptions)
        //{
        //    await serviceSubscription.Send_FabricSendAndForgetRequestCancelled_ToClientAsync(message, ct);
        //}
    }

    private async Task Receive_BusSendRequest_FromFabricAsync(BusSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusSendRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        using var cts = new LinkedCancellationTokenSourceWithTimeout(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            // Routeren naar client(s)
            var connections = GetBusConnections(message.Routing);
            foreach (var connection in connections)
            {
                await connection.Receive_BusSendRequest_FromFabricAsync(message, cts.Token);
                cts.Reset();
            }
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


        //string? exception = null;
        //try
        //{
        //    var targetConn = BusConnections.TryGetValue(request.Routing.ToBusConnectionId ?? default, out var directConn)
        //        ? directConn
        //        : BusConnections.Values.FirstOrDefault();

        //    if (targetConn != null)
        //    {
        //        await targetConn.ReceiveSendRequestAsync(request, ct);
        //    }
        //    else
        //    {
        //        exception = $"No local BusConnection found for service {request.Routing.ServiceId}";
        //    }
        //}
        //catch (Exception ex)
        //{
        //    exception = ex.Message;
        //}

        //var ackRouting = request.Routing with
        //{
        //    FromBusConnectionId = request.Routing.ToBusConnectionId ?? (BusConnections.Keys.FirstOrDefault()),
        //    To = BusToChoice.ToBusConnectionId,
        //    ToBusConnectionId = request.Routing.FromBusConnectionId
        //};
        //await Sender.Send_BusSendAck_ToFabricAsync(new BusSendRequestDoneDto(ackRouting, false, exception), ct);
    }
    private async Task Receive_BusSendRequestCancelled_FromFabricAsync(BusSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusSendRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        cts.Cancel();

        //// Routeren naar client(s)
        //var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
        //foreach (var serviceSubscription in serviceSubscriptions)
        //{
        //    await serviceSubscription.Send_FabricSendRequestCancelled_ToClientAsync(message, ct);
        //}
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

        var cts = new LinkedCancellationTokenSourceWithTimeout(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        try
        {
            var connections = GetBusConnections(message.Routing);
            var enumerable = CreateBusEnumerable(message, cts, connections);
            RegisterBusAsyncEnumerableArgument(message.Routing, -1, enumerable, b => b, cts.Cts);

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

        //// Routeren naar client(s)
        //var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        //foreach (var serviceSubscription in SseServiceSubscriptions)
        //{
        //    await serviceSubscription.Send_FabricInvokeRequestCancelled_ToClientAsync(message, ct);
        //}
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

    private async Task Receive_BusStreamingResponse_FromFabricAsync(BusStreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusStreamingResponse_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
        if (StreamingCache.BusStreamingResponseHandlers.TryGetValue(key, out var responseHandler))
        {
            responseHandler.Invoke(message);
        }
    }
    private async Task Receive_BusStreamingRequest_FromFabricAsync(BusStreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_BusStreamingRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.BusStreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            try
            {
                var response = await handler.Invoke(message.StreamId, false, ct);
                await Sender.Send_BusStreamingResponse_ToFabricAsync(response, ct);
            }
            catch (Exception ex)
            {

            }
        }
    }

    #endregion

    #endregion

    #region Call's vanuit gegenereerde code

    #region Bus

    public IAsyncEnumerable<T> RegisterBusRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
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
    public void RegisterBusAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
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
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
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
                cts.IsCancellationRequested)
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

    public async Task BusSendFireAndForgetAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationTokenSource timeoutCts)
    {
        await Sender.Send_BusSendAndForgetRequest_ToFabricAsync(request, timeoutCts.Token);

        timeoutCts.Token.Register(async () =>
        {
            await Sender.Send_BusSendAndForgetRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });
    }
    public async Task BusSendAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationTokenSource timeoutCts)
    {
        var ct = timeoutCts.Token;

        var complete = new TaskCompletionSource<BusSendRequestDoneDto>();

        if (StreamingCache.PendingBusSendRequests.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");
        timeoutCts.Token.Register(() => { StreamingCache.PendingBusSendRequests.TryRemove(request.Routing, out _); });

        await Sender.Send_BusSendRequest_ToFabricAsync(request, ct);

        timeoutCts.Token.Register(async () =>
        {
            await Sender.Send_BusSendCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        var sendRequestAck = await complete.Task.WaitAsync(timeoutCts.Token);
        if (sendRequestAck.ExceptionMessage != null)
            throw new RemoteException(sendRequestAck.ExceptionMessage);
    }
    public async IAsyncEnumerable<byte[]> BusInvokeAsync(IBaseFabricConnection baseBusConnection, BusInvokeRequestDto request, CancellationTokenSource timeoutCts)
    {
        var ct = timeoutCts.Token;

        var complete = new TaskCompletionSource<BusInvokeRequestAckDto>();
        if (StreamingCache.PendingBusInvokeRequests.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");
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
                yield return item;
            }
        }
        finally
        {
            StreamingCache.PendingBusInvokeRequests.TryRemove(request.Routing, out _);
        }
    }

    #endregion

    #endregion

    #region Helpers

    private IEnumerable<IBaseFabricConnection> GetBusConnections(BusRoutingDto routing)
    {
        if (BusConnections.Services.TryGetValue(routing.ServiceId, out var serviceSubscriptions) == false)
            return [];

        if (routing.To == BusToChoice.ToAll)
            return serviceSubscriptions.Values;

        var any = serviceSubscriptions.Values.FirstOrDefault(); // TODO Loadbalancing
        if (routing.To == BusToChoice.ToAny && any != null)
            return [any]; // TODO Loadbalancing

        var conn = serviceSubscriptions.Values.FirstOrDefault(a => a.BusConnectionId == routing.ToBusConnectionId);
        if (routing.To == BusToChoice.ToBusConnectionId && conn != null)
            return [conn];

        return [];
    }

    private async IAsyncEnumerable<byte[]> CreateBusEnumerable(BusInvokeRequestDto message, LinkedCancellationTokenSourceWithTimeout cts, IEnumerable<IBaseFabricConnection> connections)
    {
        foreach (var connection in connections)
        {
            var list = connection.Receive_BusInvokeRequest_FromFabricAsync(message, cts.Token);
            await foreach (var item in list)
                yield return item;
            cts.Reset();
        }
        StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
    }


    #endregion

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Closing FabricClient {Id}",
                FabricConnectionId);
        }
        await DisconnectAsync();
        if (SenderCts != null)
        {
            await SenderCts.CancelAsync();
            SenderCts.Dispose();
        }
    }

}