using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace gAPI.Core.Server.Fabric;

public sealed class PlaneFabricClient
{
    public PlaneFabricClient(
        SessionCache sessionCache,
        StreamingCache streamingCache,
        ILoggerFactory loggerFactory,
        BackPlaneConfig BackPlaneConfig)
    {
        Sender = new PlaneFabricClientSender(this, loggerFactory);

        StreamingCache = streamingCache;
        Logger = loggerFactory.CreateLogger<PlaneFabricClient>();
        Config = BackPlaneConfig;

        if (!string.IsNullOrEmpty(BackPlaneConfig.ConnectionString))
        {
            if (BackPlaneConfig.ConnectionString.Contains('='))
            {
                var parts = BackPlaneConfig.ConnectionString!.Split(';')
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
            else if (Uri.TryCreate(BackPlaneConfig.ConnectionString, UriKind.Absolute, out var uri))
            {
                Host = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 9000;
            }
            else if (BackPlaneConfig.ConnectionString.Contains(':'))
            {
                var split = BackPlaneConfig.ConnectionString.Split(':');
                Host = split[0].Trim();
                Port = int.TryParse(split[1].Trim(), out var p) ? p : 9000;
            }
            else
            {
                Host = BackPlaneConfig.ConnectionString.Trim();
                Port = 9000;
            }
        }
        else
        {
            throw new Exception("Plane fabric config not set");
        }
    }

    readonly ILogger Logger;

    public BackPlaneConfig Config { get; }

    readonly string Host;
    readonly int Port;

    readonly PlaneFabricClientSender Sender;
    readonly StreamingCache StreamingCache;

    TcpClient? Tcp;
    NetworkStream? Stream;

    public BinaryWriter? Writer { get; private set; }
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricPlaneConnectionId FabricConnectionId { get; private set; } = new FabricPlaneConnectionId(-1);
    public bool IsConnected => Tcp?.Connected == true;

    private CancellationToken? LifeCycleCt;
    private IFabricBasePlaneConnection? PlaneConnection;

    CancellationTokenSource? ConnectedCts;
    bool RunningRequested;
    SemaphoreSlim ConnectingGate = new SemaphoreSlim(1);

    #region Connection

    public void Start(IFabricBasePlaneConnection planeConnection, CancellationToken ct)
    {
        LifeCycleCt = ct;
        PlaneConnection = planeConnection;
        _ = Task.Run(async () =>
        {
            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ConnectAsync");
            }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                await Sender.SendKernel(ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SendKernel");
            }
        });
    }

    private async Task ConnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} ConnectAsync()", DateTime.Now.ToString("HH:mm:ss.fff"));

        await ConnectingGate.WaitAsync();
        try
        {
            RunningRequested = true;
            await Gated_ConnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_ConnectAsync()
    {
        while (RunningRequested == true && ConnectedCts == null)
        {
            try
            {
                Tcp = new TcpClient();
                Tcp.Connect(Host, Port);
                Stream = Tcp.GetStream();
                Writer = new BinaryWriter(Stream);
                var reader = new BinaryReader(Stream);

                ConnectedCts = new CancellationTokenSource();

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ConnectAsync() Connected!", DateTime.Now.ToString("HH:mm:ss.fff"));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ReceiveKernel(reader, ConnectedCts.Token);

                        if (RunningRequested)
                            await ReconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Gated_ConnectAsync");
                    }
                });
            }
            catch (Exception ex)
            {
                // Is niet bereikbaar, niet loggen
                //Logger.LogWarning(ex, "Gated_ConnectAsync");

                try
                {
                    Writer = null;
                    ConnectedCts?.Cancel();
                    ConnectedCts?.Dispose();
                    ConnectedCts = null;
                    Stream?.Dispose();
                    Tcp?.Dispose();
                }
                catch (Exception ex2)
                {
                    Logger.LogError(ex2, "Gated_ConnectAsync catch");
                }
            }
        }
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
        if (PlaneConnection == null || LifeCycleCt == null)
            throw new Exception("Not initialized");

        await Task.Delay(TimeSpan.FromSeconds(5));

        await Gated_DisconnectAsync();
        await PlaneConnection.OnDisconnectedAsync(LifeCycleCt.Value);

        await Gated_ConnectAsync();
        await PlaneConnection.OnRestoreSubscriptionsAsync(LifeCycleCt.Value);

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Reconnecting PlaneFabricClient DONE", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting PlaneFabricClient {Id}",
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
        if (ConnectedCts == null)
            return;

        //foreach (var list in StreamingCache.PlaneServiceSubscriptionIds.Values)
        //{
        //    list.Clear();
        //}

        await ConnectedCts.CancelAsync();

        Stream?.Dispose();
        Stream = null;

        Tcp?.Dispose();
        Tcp = null;

        ConnectedCts.Dispose();
        ConnectedCts = null;
    }

    public async Task<ServiceSubscriptionId> PlaneSubscribeAsync(IFabricBasePlaneConnection planeConnection, object implementation, ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "PlaneSubscribeAsync({basePlaneConnection},{implementation},{subscribe})",
                planeConnection,
                implementation,
                serviceId);

        //var service = StreamingCache.PlaneServiceSubscriptionIds.GetOrAdd(serviceId, a => new ConcurrentDictionary<object, ServiceSubscriptionId>(ReferenceEqualityComparer.Instance));
        //var serviceSubscriptionId = service.GetOrAdd(implementation, a => ServiceSubscriptionId.New());

        var message = new PlaneSubscribeDto(serviceSubscriptionId, serviceId, planeConnection.PlaneConnectionId);

        var completion = new TaskCompletionSource<PlaneSubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PlaneSubscribeTcss.TryAdd(serviceSubscriptionId, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add to cache");
        }

        try
        {
            await Sender.Send_PlaneSubscribe_ToFabricAsync(message, ct);

            var ack = await completion.Task.WaitAsync(ct);

            if (implementation is IPlaneServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnSubscribedAsync(message.ServiceId, message.ServiceSubscriptionId, ct);
            }
        }
        finally
        {
            StreamingCache.PlaneSubscribeTcss.TryRemove(message.ServiceSubscriptionId, out _);
        }

        return serviceSubscriptionId;
    }
    public async Task PlaneUnsubscribeAsync(IFabricBasePlaneConnection planeConnection, object implementation, ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "PlaneUnsubscribeAsync({basePlaneConnection},{implementation},{subscribe})",
                planeConnection,
                implementation,
                serviceId);

        //var service = StreamingCache.PlaneServiceSubscriptionIds.GetOrAdd(serviceId, a => new ConcurrentDictionary<object, ServiceSubscriptionId>(ReferenceEqualityComparer.Instance));
        //if (service.TryRemove(implementation, out var serviceSubscriptionId) == false)
        //{
        //    Logger.LogError("Cannot remove subscription");
        //    throw new Exception("Cannot add to cache");
        //}

        var message = new PlaneUnsubscribeDto(serviceSubscriptionId, serviceId, planeConnection.PlaneConnectionId);

        var completion = new TaskCompletionSource<PlaneUnsubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PlaneUnsubscribeTcss.TryAdd(message.ServiceSubscriptionId, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add to cache");
        }

        try
        {
            await Sender.Send_PlaneUnsubscribe_ToFabricAsync(message, ct);

            var ack = await completion.Task.WaitAsync(ct);

            if (implementation is IPlaneServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnUnsubscribedAsync(message.ServiceId, message.ServiceSubscriptionId, ct);
            }
        }
        finally
        {
            StreamingCache.PlaneUnsubscribeTcss.TryRemove(message.ServiceSubscriptionId, out _);
        }
    }

    #endregion

    #region Receiver

    private async Task ReceiveKernel(BinaryReader binaryReader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var messageType = PlaneFabricConverter.ReadHostToClientMessageType(binaryReader);

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ReceiveKernel(messageType = {messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

                switch (messageType)
                {
                    case PlaneFabricHostToClientMessageEnum.SynchronizeFabricIds:
                        var synchronizeFabricIds = binaryReader.ReadSynchronizePlaneFabricIdsDto(Config.MaxPackageSize ?? 512 * 1024);
                        Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                        break;

                    case PlaneFabricHostToClientMessageEnum.SubscribeAck:
                        var subscribeAck = binaryReader.ReadPlaneSubscribeAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        Received_SubscribeAck_FromFabric(subscribeAck, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.UnsubscribeAck:
                        var unsubscribeAck = binaryReader.ReadPlaneUnsubscribeAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        Received_UnsubscribeAck_FromFabric(unsubscribeAck, ct);
                        break;

                    case PlaneFabricHostToClientMessageEnum.PlaneFireAndForget:
                        var busFireAndForget = binaryReader.ReadPlaneSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Receive_PlaneFireAndForget_FromFabricAsync(busFireAndForget, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneFireAndForget_FromFabricAsync({message})", busFireAndForget);
                            }
                        }, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneSendRequest:
                        var busSendRequest = binaryReader.ReadPlaneSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Receive_PlaneSendRequest_FromFabricAsync(busSendRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneSendRequest_FromFabricAsync({message})", busSendRequest);
                            }
                        }, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneSendRequestCancelled:
                        var busSendRequestCancelled = binaryReader.ReadPlaneSendRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                        Receive_PlaneSendRequestCancelled_FromFabricAsync(busSendRequestCancelled, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneSendRequestDone:
                        var busSendRequestAck = binaryReader.ReadPlaneSendRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                        Receive_PlaneSendRequestAck_FromFabricAsync(busSendRequestAck, ct);
                        break;

                    case PlaneFabricHostToClientMessageEnum.PlaneInvokeRequest:
                        var busInvokeRequest = binaryReader.ReadPlaneInvokeRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Receive_PlaneInvokeRequest_FromFabricAsync(busInvokeRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneInvokeRequest_FromFabricAsync({message})", busInvokeRequest);
                            }
                        }, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestCancelled:
                        var busInvokeRequestCancelled = binaryReader.ReadPlaneInvokeRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                        Receive_PlaneInvokeRequestCancelled_FromFabricAsync(busInvokeRequestCancelled, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestAck:
                        var busInvokeResponseAck = binaryReader.ReadPlaneInvokeRequestAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        Receive_PlaneInvokeRequestAck_FromFabricAsync(busInvokeResponseAck, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestDone:
                        var busInvokeResponseDone = binaryReader.ReadPlaneInvokeRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Receive_PlaneInvokeRequestDone_FromFabricAsync(busInvokeResponseDone, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneInvokeRequestDone_FromFabricAsync({message})", busInvokeResponseDone);
                            }
                        }, ct);
                        break;

                    case PlaneFabricHostToClientMessageEnum.PlaneStreamingRequest:
                        var busStreamingRequest_ResponseEnumerable = binaryReader.ReadPlaneStreamingRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Receive_PlaneStreamingRequest_FromFabricAsync(busStreamingRequest_ResponseEnumerable, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneStreamingRequest_FromFabricAsync({message})", busStreamingRequest_ResponseEnumerable);
                            }
                        }, ct);
                        break;
                    case PlaneFabricHostToClientMessageEnum.PlaneStreamingResponse:
                        var busStreamingResponse_ResponseEnumerable = binaryReader.ReadPlaneStreamingResponseDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Receive_PlaneStreamingResponse_FromFabricAsync(busStreamingResponse_ResponseEnumerable, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Receive_PlaneStreamingResponse_FromFabricAsync({message})", busStreamingResponse_ResponseEnumerable);
                            }
                        }, ct);
                        break;

                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                "FabricClient #{Id.Value}: Exception occured, probably disconnect, restarting fabric client\r\n{ex}",
                FabricConnectionId?.Value,
                ex);
        }
    }

    private void Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizePlaneFabricIdsDto synchronizeFabricIds, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeFabricIds);

        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;

        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} PlaneFabricClient {FabricConnectionId.Value} started");
    }

    private void Received_SubscribeAck_FromFabric(PlaneSubscribeAckDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SubscribeAck_FromFabric({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (StreamingCache.PlaneSubscribeTcss.TryRemove(message.SubscribeDto.ServiceSubscriptionId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled();
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.TrySetResult(message);
        }
    }
    private void Received_UnsubscribeAck_FromFabric(PlaneUnsubscribeAckDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_UnsubscribeAck_FromFabric({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (StreamingCache.PlaneUnsubscribeTcss.TryRemove(message.UnsubscribeDto.ServiceSubscriptionId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled();
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Receive_PlaneFireAndForget_FromFabricAsync(PlaneSendRequestDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneFireAndForgetAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PlaneConnection == null)
            throw new Exception("Not started");

        await PlaneConnection.Send_FireAndForgetRequest_ToServiceAsync(message, connectionLifecycleCt);
    }

    private async Task Receive_PlaneSendRequest_FromFabricAsync(PlaneSendRequestDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneSendRequest_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PlaneConnection == null)
            throw new Exception("Not started");

        using var timeoutCts = new CancellationTokenSourceDispose(message.Routing.Timeout, connectionLifecycleCt);
        if (StreamingCache.PlaneReceivedTimeouts.TryAdd(message.Routing.RequestId, timeoutCts) == false)
        {
            Logger.LogError("Cannot add key");
            throw new Exception("Cannot add to cache");
        }
        var ct = timeoutCts.Token;

        try
        {
            await PlaneConnection.Send_SendRequest_ToServiceAsync(message, timeoutCts);
            await Sender.Send_PlaneSendRequestDone_ToFabricAsync(
                new PlaneSendRequestDoneDto(
                    message.Routing,
                    false,
                    null),
                ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Task is cancelled");
            await Sender.Send_PlaneSendRequestDone_ToFabricAsync(
                new PlaneSendRequestDoneDto(
                    message.Routing,
                    true,
                    new ExceptionDto(ex)),
                ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not send data to fabric");
            await Sender.Send_PlaneSendRequestDone_ToFabricAsync(
                new PlaneSendRequestDoneDto(
                    message.Routing,
                    false,
                    new ExceptionDto(ex)),
                ct);
        }
        finally
        {
            StreamingCache.PlaneReceivedTimeouts.TryRemove(message.Routing.RequestId, out _);
        }
    }
    private void Receive_PlaneSendRequestCancelled_FromFabricAsync(PlaneSendRequestCancelledDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneSendRequestCancelled_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (StreamingCache.PlaneSenderTimeouts.TryRemove(message.Routing.RequestId, out var cts))
            cts.Cancel();
    }
    private void Receive_PlaneSendRequestAck_FromFabricAsync(PlaneSendRequestDoneDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneSendRequestAck_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (StreamingCache.PendingPlaneSendRequests.TryRemove(message.Routing, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled();
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Receive_PlaneInvokeRequest_FromFabricAsync(PlaneInvokeRequestDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneInvokeRequest_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PlaneConnection == null)
            throw new Exception("Not started");

        var timeoutCts = new CancellationTokenSourceDispose(message.Routing.Timeout, connectionLifecycleCt);
        StreamingCache.PlaneReceivedTimeouts[message.Routing.RequestId] = timeoutCts;
        var ct = timeoutCts.Token;

        try
        {
            var enumerable = PlaneConnection.Send_InvokeRequest_ToServiceAsync(message, timeoutCts, timeoutCts.Token);
            RegisterPlaneAsyncEnumerableArgument(message.Routing, -1, enumerable, b => b, timeoutCts);
            await Sender.Send_PlaneInvokeRequestAck_ToFabricAsync(
                new PlaneInvokeRequestAckDto(
                    message.Routing,
                    false,
                    null),
                ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Task was cancelled");
            await Sender.Send_PlaneSendRequestDone_ToFabricAsync(
                new PlaneSendRequestDoneDto(
                    message.Routing,
                    true,
                    new ExceptionDto(ex)),
                ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Receive_PlaneInvokeRequest_FromFabricAsync");
            await Sender.Send_PlaneSendRequestDone_ToFabricAsync(
                new PlaneSendRequestDoneDto(
                    message.Routing,
                    false,
                    new ExceptionDto(ex)),
                ct);
        }
    }
    private void Receive_PlaneInvokeRequestCancelled_FromFabricAsync(PlaneInvokeRequestCancelledDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequestCancelled_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (StreamingCache.PlaneReceivedTimeouts.TryRemove(message.Routing.RequestId, out var cts))
            cts.Cancel();
    }
    private void Receive_PlaneInvokeRequestAck_FromFabricAsync(PlaneInvokeRequestAckDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneInvokeRequestAck_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (StreamingCache.PlaneReceivedTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingCache.PendingPlaneInvokeRequests.TryRemove(message.Routing, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled();
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetResult(message);
        }
    }
    private async Task Receive_PlaneInvokeRequestDone_FromFabricAsync(PlaneInvokeRequestDoneDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneInvokeRequestDone_FromFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (StreamingCache.PlaneReceivedTimeouts.TryRemove(message.Routing.RequestId, out var handler))
            handler.Dispose();

        if (StreamingCache.PendingPlaneInvokeRequests.TryRemove(message.Routing, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled();
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetCanceled(); // hij zit te wachten op beginnen met streamen, maar request is al done. Dus error?! Iig out-of-sync
        }
    }

    private async Task Receive_PlaneStreamingRequest_FromFabricAsync(PlaneStreamingRequestDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneStreamingRequest_FromFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        CancellationTokenSourceDispose? timeoutCts = null;
        if (StreamingCache.PlaneReceivedTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
        {
            timeout.CancelAfter(message.Routing.Timeout);
            timeoutCts = timeout;
        }
        if (StreamingCache.PlaneSenderTimeouts.TryGetValue(message.Routing.RequestId, out var timeout2))
        {
            timeout2.CancelAfter(message.Routing.Timeout);
            timeoutCts = timeout2;
        }
        if (timeoutCts == null)
            Logger.LogError("cannot find timeout");

        var ct = timeoutCts?.Token ?? connectionLifecycleCt;

        if (ct.IsCancellationRequested)
        {
            Logger.LogError("Receive_PlaneStreamingRequest_FromFabricAsync CANCELLATION IS REQUESTED {timeout}, {timeout2}", timeout != null, timeout2 != null);

        }


        if (!ct.IsCancellationRequested && StreamingCache.PlaneStreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            try
            {
                var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
                await Sender.Send_PlaneStreamingResponse_ToFabricAsync(response, ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Receive_PlaneStreamingRequest_FromFabricAsync({message})", message);
            }
        }
        else
        {
            await Sender.Send_PlaneStreamingResponse_ToFabricAsync(
                new PlaneStreamingResponseDto(
                    message.Routing,
                    message.ArgumentIndex,
                    message.StreamId,
                    true,
                    false,
                    new ExceptionDto("No StreamingRequestHandlers found", null, Environment.StackTrace),
                    []),
                ct);
        }
    }
    private void Receive_PlaneStreamingResponse_FromFabricAsync(PlaneStreamingResponseDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_PlaneStreamingResponse_FromFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        CancellationTokenSourceDispose? timeoutCts = null;
        if (StreamingCache.PlaneReceivedTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
        {
            timeout.CancelAfter(message.Routing.Timeout);
            timeoutCts = timeout;
        }
        if (StreamingCache.PlaneSenderTimeouts.TryGetValue(message.Routing.RequestId, out var timeout2))
        {
            timeout2.CancelAfter(message.Routing.Timeout);
            timeoutCts = timeout2;
        }
        if (timeoutCts == null)
            Logger.LogError("cannot find timeout");

        var ct = timeoutCts?.Token ?? connectionLifecycleCt;

        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
        if (StreamingCache.PlaneStreamingResponseHandlers.TryGetValue(key, out var responseHandler))
        {
            responseHandler.Invoke(message);
        }
    }

    #endregion

    #region Calls vanuit code

    public void RegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        if (StreamingCache.PlaneSenderTimeouts.TryAdd(routing.RequestId, cts) == false)
        {
            Logger.LogError("RegisterTimeout cannot all timeout");
        }
    }
    public void UnRegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        StreamingCache.PlaneSenderTimeouts.TryRemove(routing.RequestId, out _);
    }

    public IAsyncEnumerable<T> RegisterPlaneRemoteAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer, CancellationTokenSourceDispose timeoutCts)
    {
        return new RemoteAsyncEnumerable<T>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.PlaneStreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(message.BinaryData));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Exception in Remote Plane AsyncEnumerable {argIndex}, routing: {routing}", argumentIndex, routing);
                        enumerator.Complete(ex);
                    }
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                return Sender.Send_PlaneStreamingRequest_ToFabricAsync(
                    new(routing,
                    argumentIndex,
                    streamId,
                    false),
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return Sender.Send_PlaneStreamingRequest_ToFabricAsync(
                    new(routing,
                    argumentIndex,
                    streamId,
                    true),
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.PlaneStreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    public void RegisterPlaneAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timeoutCts)
    {
        StreamingCache.PlaneStreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new PlaneAsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
                    return new PlaneAsyncEnumerableRegistrationInstance<T>(
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
                    await Sender.Send_PlaneInvokeRequestDone_ToFabricAsync(
                        new PlaneInvokeRequestDoneDto(
                            routing,
                            false,
                            ex == null ? null : new ExceptionDto(ex)),
                        ct);
                }
            }

            var entered = false;
            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && await enumerator.MoveNextAsync();

                var response = new PlaneStreamingResponseDto(
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
                Logger.LogError("Exception in Remote Plane AsyncEnumerable {argIndex}, routing: {routing}", argumentIndex, routing);

                await Cleanup(ex);

                return new PlaneStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    new ExceptionDto(ex),
                    []);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception in Remote Plane AsyncEnumerable {argIndex}, routing: {routing}", argumentIndex, routing);

                await Cleanup(ex);

                return new PlaneStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    new ExceptionDto(ex),
                    []);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }
    public async Task UnRegisterPlaneAsyncEnumerableArgument(PlaneRoutingDto routing, int argumentIndex)
    {
        if (StreamingCache.PlaneStreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    public RemoteAsyncEnumerable<byte[]> RegisterPlaneRemoteAsyncEnumerableArgumentByte(PlaneRoutingDto routing, int argumentIndex, CancellationTokenSourceDispose timeoutCts)
    {
        return new RemoteAsyncEnumerable<byte[]>(
            timeoutCts: timeoutCts,
            construct: (streamId, enumerator, ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.PlaneStreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    enumerator.Push(message.BinaryData);
                });
            },

            requestNext: (streamId, enumerator, ct) =>
            {
                return Sender.Send_PlaneStreamingRequest_ToFabricAsync(
                    new PlaneStreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return Sender.Send_PlaneStreamingRequest_ToFabricAsync(
                    new PlaneStreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true),
                    default);

            },

            dispose: streamId =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.PlaneStreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    public async Task SendFireAndForgetAsync(IBasePlaneConnection basePlaneConnection, PlaneSendRequestDto request, CancellationToken ct)
    {
        if (LifeCycleCt == null)
            throw new Exception("PlaneFabric not initilazed");

        await Sender.Send_PlaneSendAndForgetRequest_ToFabricAsync(request, ct);
    }
    public async Task SendAsync(IBasePlaneConnection basePlaneConnection, PlaneSendRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;

        var completion = new TaskCompletionSource<PlaneSendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PendingPlaneSendRequests.TryAdd(request.Routing, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add completion");
        }

        timeoutCts.RegisterDispose(async () =>
        {
            StreamingCache.PendingPlaneSendRequests.TryRemove(request.Routing, out _);
        });

        await Sender.Send_PlaneSendRequest_ToFabricAsync(request, ct);
        timeoutCts.CancelAfter(request.Routing.Timeout);

        timeoutCts.Token.Register(async () =>
        {
            await Sender.Send_PlaneSendCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        var message = await completion.Task.WaitAsync(timeoutCts.Token);

        if (message.Exception != null)
            throw new RemoteException(message.Exception);
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(IBasePlaneConnection basePlaneConnection, PlaneInvokeRequestDto request, CancellationTokenSourceDispose timeoutCts, [EnumeratorCancellation] CancellationToken ct)
    {
        var completion = new TaskCompletionSource<PlaneInvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PendingPlaneInvokeRequests.TryAdd(request.Routing, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add completion");
        }

        try
        {
            await Sender.Send_PlaneInvokeRequest_ToFabricAsync(request, ct);
            timeoutCts.CancelAfter(request.Routing.Timeout);

            timeoutCts.Token.Register(async () =>
            {
                await Sender.Send_PlaneInvokeRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

            var response = await completion.Task.WaitAsync(request.Routing.Timeout, ct);

            var enumerable = RegisterPlaneRemoteAsyncEnumerableArgumentByte(request.Routing, -1, timeoutCts);
            await foreach (var item in enumerable)
            {
                timeoutCts.CancelAfter(request.Routing.Timeout);
                yield return item;
            }
        }
        finally
        {
            StreamingCache.PendingPlaneInvokeRequests.TryRemove(request.Routing, out _);
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Closing PlaneFabricClient {Id}",
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