using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

// Deze class houd alle communicatie consistentie in de gate, en routeert alles. Voor de WssServerConnection is dit gewoon het doorgeef luik "doe call naar client"
// De fabricReceiver is wel autonoom wat betreft de afhandeling richting fabric, dus als er een bericht terug moet naar de fabric doet hij dit zelf.
// De fabricSender is zo dom mogelijk
public sealed class FabricClient : IFabricClient
{
    public FabricClient(
        SessionCache sessionCache,
        StreamingCache streamingCache,
        ServiceSubscriptionCollection serviceSubscriptions,
        ILoggerFactory loggerFactory,
        string? fabricConnectionString)
    {
        Sender = new FabricClientSender(this, loggerFactory);

        LocalSessionCache = sessionCache;
        StreamingCache = streamingCache;
        ServiceSubscriptions = serviceSubscriptions;
        Logger = loggerFactory.CreateLogger<FabricClient>();
        //ServerServiceRouter = serverServiceRouter;

        if (!string.IsNullOrEmpty(fabricConnectionString))
        {
            // Parse connection string
            var parts = fabricConnectionString!.Split(';')
                .Where(x => x.Contains('='))
                .Select(x => x.Split(['='], 2))
                .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (!parts.TryGetValue("Server", out var host))
                throw new Exception("AutoSse ConnectionString must contain 'Server' parameter");
            if (!parts.TryGetValue("Port", out var portString))
                throw new Exception("AutoSse ConnectionString must contain 'Port' parameter");
            if (!int.TryParse(portString, out var port))
                throw new Exception("AutoSse ConnectionString 'Port' parameter must be a int");

            Host = host;
            Port = port;

            _ = Task.Run(ConnectAsync);
        }
    }

    readonly ILogger Logger;

    readonly string? Host;
    readonly int? Port;

    readonly FabricClientSender Sender;
    readonly StreamingCache StreamingCache;
    readonly ServiceSubscriptionCollection ServiceSubscriptions;
    readonly SessionCache LocalSessionCache;

    TcpClient? Tcp;
    NetworkStream? Stream;
    bool FirstTime;
    bool IsConnecting;
    bool IsDisconnecting;

    CancellationTokenSource? SenderCts;
    CancellationTokenSource? ReceiverCts;
    BinaryWriter? BinaryWriter;
    BinaryReader? BinaryReader;

    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
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
                _ = Task.Run(async () => { await Sender.SendKernel(BinaryWriter, SenderCts.Token); });
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
        foreach (var service in ServiceSubscriptions.Services.Values)
        {
            foreach (var SseServiceSubscription in service.Values)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(
                        "{now} Resubscribe IServiceSubscription {HostId} to {ServiceId} (userId {UserId}, sessionId {SessionId})",
                        DateTime.Now.ToString("HH:mm:ss.fff"),
                        SseServiceSubscription.ServiceSubscriptionId,
                        SseServiceSubscription.ServiceId,
                        SseServiceSubscription.UserId,
                        SseServiceSubscription.SessionId);

                var request = new SubscribeDto(
                    SseServiceSubscription.ServiceId,
                    SseServiceSubscription.UserId,
                    SseServiceSubscription.SessionId);
                await Sender.Send_Subscribe_ToFabricAsync(request, ct);
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

    #region Session

    public async Task UpdateSession(SessionId sessionId, string? cookieData, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UpdateSession({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        if (Host == null || IsConnected == false)
        {
            LocalSessionCache.AddOrUpdate(sessionId, cookieData);
            return;
        }

        var updateSessionDto = new UpdateSessionDto(sessionId, cookieData);
        await Sender.Send_UpdateSession_ToFabricAsync(updateSessionDto, ct);
    }
    public async Task ClearSession(SessionId sessionId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} ClearSession({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        if (Host == null || IsConnected == false)
        {
            LocalSessionCache.Remove(sessionId);
            return;
        }

        var clearSessionDto = new SendClearSessionDto(sessionId);
        await Sender.Send_ClearSession_ToFabricAsync(clearSessionDto, ct);
    }
    public async Task<string?> GetSessionCookieData(string sessionIdString, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} GetSessionCookieData({sessionIdString})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionIdString);

        var sessionId = new SessionId(sessionIdString);

        // als er geen fabric is
        if (Host == null || IsConnected == false)
        {
            if (LocalSessionCache.TryGet(sessionId, out var cookieData))
                return cookieData;
            return null;
        }

        // Maak een TaskCompletionSource aan voor deze specifieke sessie
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingGetSessionRequests[sessionId] = tcs;

        try
        {
            var getSessionDto = new SendGetSessionCookieDataDto(sessionId);
            await Sender.Send_GetSession_ToFabricAsync(getSessionDto, ct);

            // Koppel de time-out aan de meegegeven CancellationToken van de gebruiker
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(100));

            // Wacht tot óf de TaskCompletionSource klaar is, óf de time-out/cancel afgaat
            using (linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token)))
            {
                return await tcs.Task;
            }
        }
        catch (OperationCanceledException)
        {
            // Log hier eventueel dat er een time-out of annulering heeft plaatsgevonden
            return null;
        }
        finally
        {
            // Zorg dat we de sessie altijd netjes opruimen uit de dictionary
            StreamingCache.PendingGetSessionRequests.TryRemove(sessionId, out _);
        }
    }

    #endregion

    #region Subscription

    public async Task SubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SubscribeAsync({SseServiceSubscription})",
                serviceSubscription);

        var SseServiceSubscriptionsForService = ServiceSubscriptions.Services.AddOrUpdate(
            serviceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(),
            (a, b) => b);
        SseServiceSubscriptionsForService[serviceSubscription.ServiceSubscriptionId] = serviceSubscription;

        if (Host == null)
        {
            return;
        }

        var request = new SubscribeDto(
            serviceSubscription.ServiceId,
            serviceSubscription.UserId,
            serviceSubscription.SessionId);
        await Sender.Send_Subscribe_ToFabricAsync(request, ct);
    }
    public async Task UnsubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({SseServiceSubscription})",
                serviceSubscription);

        ServiceSubscriptions.Services[serviceSubscription.ServiceId].TryRemove(serviceSubscription.ServiceSubscriptionId, out _);

        if (Host == null)
        {
            return;
        }

        var request = new UnsubscribeDto(
            serviceSubscription.ServiceId,
            serviceSubscription.UserId,
            serviceSubscription.SessionId
        );
        await Sender.Send_Unsubscribe_ToFabricAsync(request, ct);
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
                var messageType = FabricConverter.ReadHostToClientMessageType(BinaryReader);

                //if (Logger.IsEnabled(LogLevel.Trace))
                //    Logger.LogTrace("{now} ReceiveKernel(messageType = {messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

                switch (messageType)
                {
                    case FabricHostToClientMessageEnum.SynchronizeFabricIds:
                        var synchronizeFabricIds = BinaryReader.ReadSynchronizeFabricIdsDto();
                        await Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                        break;

                    case FabricHostToClientMessageEnum.FabricSendRequest:
                        var fabricSendRequest = BinaryReader.ReadSendRequestDto();
                        await Receive_FabricSendRequest_FromFabricAsync(fabricSendRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricSendRequestCancelled:
                        var fabricSendRequestCancelled = BinaryReader.ReadSendRequestCancelledDto();
                        await Receive_FabricSendRequestCancelled_FromFabricAsync(fabricSendRequestCancelled, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var fabricSendRequestDone = BinaryReader.ReadSendRequestDoneDto();
                        await Receive_SendRequestDone_FromFabricAsync(fabricSendRequestDone, ct);
                        break;

                    case FabricHostToClientMessageEnum.FabricInvokeRequest:
                        var fabricInvokeRequest = BinaryReader.ReadInvokeRequestDto();
                        await Receive_FabricInvokeRequest_FromFabricAsync(fabricInvokeRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricInvokeRequestCancelled:
                        var fabricInvokeRequestCancelled = BinaryReader.ReadInvokeRequestCancelledDto();
                        await Receive_FabricInvokeRequestCancelled_FromFabricAsync(fabricInvokeRequestCancelled, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestDone:
                        var fabricInvokeResponseDone = BinaryReader.ReadInvokeRequestDoneDto();

                        _ = Task.Run(async () =>
                        {
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace("{now} Receive_InvokeRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), fabricInvokeResponseDone.Routing.ServiceId, fabricInvokeResponseDone.Routing.MethodId, fabricInvokeResponseDone.Routing.RequestId);

                            await Receive_InvokeRequestDone_FromFabricAsync(fabricInvokeResponseDone, ct);
                        }, ct);

                        break;

                    case FabricHostToClientMessageEnum.StreamingRequestServerToClient:
                        var fabricStreamingRequest = BinaryReader.ReadStreamingRequestDto();
                        await Receive_StreamingRequest_ServerToClient_FromFabricAsync(fabricStreamingRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingResponseServerToClient:
                        var fabricStreamingResponse = BinaryReader.ReadStreamingResponseDto();
                        await Receive_StreamingResponse_ServerToClient_FromFabricAsync(fabricStreamingResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingRequestClientToServer:
                        var argumentRequest = BinaryReader.ReadStreamingRequestDto();
                        _ = Task.Run(async () =>
                        {
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace("{now} Receive_StreamingRequest_ClientToServer_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), argumentRequest.Routing.ServiceId, argumentRequest.Routing.MethodId, argumentRequest.Routing.RequestId);

                            await Receive_StreamingRequest_ClientToServer_FromFabricAsync(argumentRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingResponseClientToServer:
                        var argumentResponse = BinaryReader.ReadStreamingResponseDto();
                        _ = Task.Run(async () =>
                        {
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace("{now} Receive_StreamingResponse_ClientToServer_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), argumentResponse.Routing.ServiceId, argumentResponse.Routing.MethodId, argumentResponse.Routing.RequestId);

                            await Receive_StreamingResponse_ClientToServer_FromFabricAsync(argumentResponse, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = BinaryReader.ReadSendGetSessionCookieDataResponseDto();
                        await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        break;
                    case FabricHostToClientMessageEnum.Log:
                        var log = BinaryReader.ReadWssLoggerLogDto();
                        await Receive_Log_FromFabricAsync(log, ct);
                        break;


                    //case FabricHostToClientMessageEnum.ServerSendRequest:
                    //    var serverSendRequest = BinaryReader.ReadServerSendRequestDto();
                    //    await Receive_ServerSendRequest_FromFabricAsync(serverSendRequest, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerSendRequestCancelled:
                    //    var serverSendRequestCancelled = BinaryReader.ReadServerSendRequestCancelledDto();
                    //    await Receive_ServerSendRequestCancelled_FromFabricAsync(serverSendRequestCancelled, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerSendRequestDone:
                    //    var serverSendRequestDone = BinaryReader.ReadServerSendRequestDoneDto();
                    //    await Receive_ServerSendRequestDone_FromFabricAsync(serverSendRequestDone, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerInvokeRequest:
                    //    var serverInvokeRequest = BinaryReader.ReadServerInvokeRequestDto();
                    //    await Receive_ServerInvokeRequest_FromFabricAsync(serverInvokeRequest, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerInvokeRequestCancelled:
                    //    var serverInvokeRequestCancelled = BinaryReader.ReadServerInvokeRequestCancelledDto();
                    //    await Receive_ServerInvokeRequestCancelled_FromFabricAsync(serverInvokeRequestCancelled, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerInvokeRequestDone:
                    //    var serverInvokeResponseDone = BinaryReader.ReadServerInvokeRequestDoneDto();
                    //    await Receive_ServerInvokeRequestDone_FromFabricAsync(serverInvokeResponseDone, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerStreamingRequest:
                    //    var serverStreamingRequest = BinaryReader.ReadServerStreamingRequestDto();
                    //    await Receive_ServerStreamingRequest_ClientToServer_FromFabricAsync(serverStreamingRequest, ct);
                    //    break;
                    //case FabricHostToClientMessageEnum.ServerStreamingResponse:
                    //    var serverStreamingResponse = BinaryReader.ReadServerStreamingResponseDto();
                    //    await Receive_ServerStreamingResponse_ClientToServer_FromFabricAsync(serverStreamingResponse, ct);
                    //    break;

                    case FabricHostToClientMessageEnum.BusSubscribeAck:
                        var busSubscribe = BinaryReader.ReadBusSubscribeDto();
                        await Receive_BusSubscribeAckAsync(busSubscribe, ct);
                        break;
                    case FabricHostToClientMessageEnum.BusUnsubscribeAck:
                        var busUnsubscribe = BinaryReader.ReadBusUnsubscribeDto();
                        await Receive_BusUnsubscribeAckAsync(busUnsubscribe, ct);
                        break;

                    case FabricHostToClientMessageEnum.BusFireAndForget:
                        var busFireAndForget = BinaryReader.ReadBusSendRequestDto();
                        await Receive_BusFireAndForgetAsync(busFireAndForget, ct);
                        break;
                    case FabricHostToClientMessageEnum.BusSendRequest:
                        var busSendRequest = BinaryReader.ReadBusSendRequestDto();
                        await Receive_BusSendAsync(busSendRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.BusSendRequestAck:
                        var busSendRequestAck = BinaryReader.ReadBusSendRequestAckDto();
                        await Receive_BusSendAckAsync(busSendRequestAck, ct);
                        break;

                    case FabricHostToClientMessageEnum.BusInvokeRequest:
                        var busInvokeRequest = BinaryReader.ReadBusInvokeRequestDto();
                        await Receive_BusInvokeAsync(busInvokeRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.BusInvokeRequestAck:
                        var busInvokeResponseAck = BinaryReader.ReadBusInvokeRequestAckDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusInvokeAckAsync(busInvokeResponseAck, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.BusStreamingRequest:
                        var busStreamingRequest = BinaryReader.ReadBusStreamingRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusStreamingRequestAsync(busStreamingRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.BusStreamingResponse:
                        var busStreamingResponse = BinaryReader.ReadBusStreamingResponseDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_BusStreamingResponseAsync(busStreamingResponse, ct);
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

    private async Task Receive_FabricSendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricSendRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Routeren naar client(s)
            var called = false;
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricSendRequest_ToClientAsync(message, ct);
                called = true;
            }

            if (!called)
                await Sender.Send_SendRequestDone_ToFabricAsync(new(message.Routing, false, null), ct);

            //try
            //{

            //    if (StreamingCache.Timeouts.TryGetValue(request.Routing.RequestId, out var timeout))
            //        timeout.Reset();

            //    await Handle_SendRequest_ToClient_Async(request, ct);
            //    await Sender.Send_SendRequestDone_ToFabricAsync(
            //        new SendRequestDoneDto(
            //            request.Routing,
            //            false,
            //            null
            //        ), ct);
            //}
            //catch (Exception ex)
            //{
            //    await Sender.Send_SendRequestDone_ToFabricAsync(
            //        new SendRequestDoneDto(
            //            request.Routing,
            //            false,
            //            ex.Message
            //        ), ct);
            //}
        }, ct);
    }
    private async Task Receive_FabricSendRequestCancelled_FromFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricSendRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Routeren naar client(s)
            var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in serviceSubscriptions)
            {
                await serviceSubscription.Send_FabricSendRequestCancelled_ToClientAsync(message, ct);
            }
        }, ct);
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (StreamingCache.PendingFabricSendRequests.TryRemove(message.Routing.RequestId, out var completion))
                completion.TrySetResult(message);
        }, ct);
    }

    private async Task Receive_FabricInvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricInvokeRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Routeren naar client(s)
            var called = false;
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricInvokeRequest_ToClientAsync(message, ct);
                called = true;
            }

            if (!called)
                await Sender.Send_InvokeRequestDone_ToFabricAsync(new(message.Routing, false, null), ct);

        }, ct);
    }
    private async Task Receive_FabricInvokeRequestCancelled_FromFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricInvokeRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Routeren naar client(s)
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricInvokeRequestCancelled_ToClientAsync(message, ct);
            }
        }, ct);
    }
    private async Task Receive_InvokeRequestDone_FromFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.PendingInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
            completion.TrySetResult(message);
    }

    private async Task Receive_StreamingRequest_ServerToClient_FromFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingRequest_ServerToClient_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var called = false;
            var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var SseServiceSubscription in serviceSubscriptions)
            {
                await SseServiceSubscription.Send_FabricStreamingRequest_ToClientAsync(message, ct);
                called = true;
            }

            if (!called)
                await Sender.Send_StreamingResponseClientToServer_ToFabricAsync(new(message.Routing, message.ArgumentIndex, message.StreamId, true, false, null, []), ct);

        }, ct);
    }
    private async Task Receive_StreamingResponse_ServerToClient_FromFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingResponse_ServerToClient_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var SseServiceSubscription in serviceSubscriptions)
            {
                await SseServiceSubscription.Send_FabricStreamingResponse_ToClientAsync(message, ct);
            }

        }, ct);
    }

    private async Task Receive_StreamingRequest_ClientToServer_FromFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            var response = await handler.Invoke(message.StreamId, false, ct);
            await Sender.Send_StreamingResponseServerToClient_ToFabricAsync(response, ct);
        }
    }
    private async Task Receive_StreamingResponse_ClientToServer_FromFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
        if (StreamingCache.StreamingResponseHandlers.TryGetValue(key, out var responseHandler))
        {
            responseHandler.Invoke(message);
        }
    }

    private async Task Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizeFabricIdsDto synchronizeFabricIds, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        //{
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeFabricIds);

        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;

        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} FabricClient {FabricConnectionId.Value} started");

        //}, ct);
    }
    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionResponse);

            // Zoek de wachtende taak op en zet het resultaat zodra het antwoord binnen is
            if (StreamingCache.PendingGetSessionRequests.TryRemove(getSessionResponse.SessionId, out var tcs))
                tcs.TrySetResult(getSessionResponse.CookieData);
        }, ct);
    }
    private async Task Receive_Log_FromFabricAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        // niet loggen ;)
#pragma warning disable CA2254 // Template should be a static expression
#pragma warning disable CA1873 // Avoid potentially expensive logging
        _ = Task.Run(async () =>
        {
            Logger.Log(log.Level, log.Message);
        }, ct);
#pragma warning restore CA2254 // Template should be a static expression
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }

    //private async Task Receive_ServerSendRequest_FromFabricAsync(ServerSendRequestDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerSendRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        try
    //        {
    //            await ServerServiceRouter.SendRequestAsync(this, message, ct);
    //            await Sender.Send_ServerSendRequestDone_ToFabricAsync(new ServerSendRequestDoneDto(message.Routing, false, null), ct);
    //        }
    //        catch (TaskCanceledException ex)
    //        {
    //            await Sender.Send_ServerSendRequestDone_ToFabricAsync(new ServerSendRequestDoneDto(message.Routing, true, ex.Message), ct);
    //        }
    //        catch (Exception ex)
    //        {
    //            await Sender.Send_ServerSendRequestDone_ToFabricAsync(new ServerSendRequestDoneDto(message.Routing, false, ex.Message), ct);
    //        }
    //    }, ct);
    //}
    //private async Task Receive_ServerSendRequestCancelled_FromFabricAsync(ServerSendRequestCancelledDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerSendRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        await ServerServiceRouter.SendRequestCancelled(this, message, ct);
    //    }, ct);
    //}
    //private async Task Receive_ServerSendRequestDone_FromFabricAsync(ServerSendRequestDoneDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerSendRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        if (StreamingCache.PendingServerSendRequests.TryRemove(message.Routing.RequestId, out var completion))
    //            completion.TrySetResult(message);
    //    }, ct);
    //}
    //private async Task Receive_ServerInvokeRequest_FromFabricAsync(ServerInvokeRequestDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerInvokeRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        var list = ServerServiceRouter.InvokeRequestAsync(this, message, ct);
    //        throw new InvalidOperationException();
    //    }, ct);
    //}
    //private async Task Receive_ServerInvokeRequestCancelled_FromFabricAsync(ServerInvokeRequestCancelledDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerInvokeRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        await ServerServiceRouter.InvokeRequestCancelled(this, message, ct);
    //        throw new InvalidOperationException();
    //    }, ct);
    //}
    //private async Task Receive_ServerInvokeRequestDone_FromFabricAsync(ServerInvokeRequestDoneDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerInvokeRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        if (StreamingCache.PendingServerInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
    //            completion.TrySetResult(message);
    //    }, ct);
    //}
    //private async Task Receive_ServerStreamingRequest_ClientToServer_FromFabricAsync(ServerStreamingRequestDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerStreamingRequest_ClientToServer_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        throw new InvalidOperationException();
    //        //await ServerServiceRouter.StreamingRequest(this, message, ct);
    //    }, ct);
    //}
    //private async Task Receive_ServerStreamingResponse_ClientToServer_FromFabricAsync(ServerStreamingResponseDto message, CancellationToken ct)
    //{
    //    _ = Task.Run(async () =>
    //    {
    //        if (Logger.IsEnabled(LogLevel.Trace))
    //            Logger.LogTrace("{now} Receive_ServerStreamingResponse_ClientToServer_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

    //        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
    //            timeout.Reset();

    //        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
    //        if (StreamingCache.ServerStreamingResponseHandlers.TryGetValue(key, out var responseHandler))
    //        {
    //            responseHandler.Invoke(message);
    //        }
    //    }, ct);
    //}



    #endregion

    #region Sender

    public async Task Send_FabricInvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricInvokeRequestDone_ToFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

        if (IsConnected)
        {
            await Sender.Send_InvokeRequestDone_ToFabricAsync(message, ct);
        }
        else
        {
            await Receive_InvokeRequestDone_FromFabricAsync(message, ct);

            //// Voor als er geen fabric is
            //if (StreamingCache.PendingClientInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
            //    completion.TrySetResult(message);
            //else
            //    await Send_FabricInvokeRequestDone_ToFabricAsync(message, ct);
        }
    }
    public async Task Send_FabricSendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricSendRequestDone_ToFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

        await Sender.Send_SendRequestDone_ToFabricAsync(done, ct);
    }

    public async Task Send_StreamingRequestClientToServer_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            await Sender.Send_StreamingRequestClientToServer_ToFabricAsync(message, ct);
        }
        else
        {
            await Receive_StreamingRequest_ClientToServer_FromFabricAsync(message, ct);
            //if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            //    timeout.Reset();

            //if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            //{{
            //    var response = await handler.Invoke(message.StreamId, false, ct);
            //    await Send_StreamingResponse_ToClientAsync(response, ct);
            //}}
        }
    }
    public async Task Send_StreamingResponseClientToServer_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            // Doorlussen naar fabric
            await Sender.Send_StreamingResponseClientToServer_ToFabricAsync(message, ct);
        }
        else
        {
            await Receive_StreamingResponse_ClientToServer_FromFabricAsync(message, ct);
            //// Fallback maar eigenlijk niet helemaal netjes.
            //if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            //    timeout.Reset();

            //if (StreamingCache.StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
            //    responseHandler.Invoke(message);
        }

    }


    #endregion

    #region Call's vanuit gegenereerde code

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingCache.StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<T>(
                async (activeStreams, streamId, cancelled, ct) =>
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

                        var hasNext = !cancelled && await enumerator.MoveNextAsync(ct);

                        var response = new StreamingResponseDto(
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

                        return new StreamingResponseDto(
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

                        return new StreamingResponseDto(
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
    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingCache.StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<byte[]>(
                async (activeStreams, streamId, cancelled, ct) =>
                {
                    var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                        streamId,
                        _ =>
                        {
                            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                            return new AsyncEnumerableRegistrationInstance<byte[]>(
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

                        var hasNext = !cancelled && await enumerator.MoveNextAsync(ct);

                        var response = new StreamingResponseDto(
                            routing,
                            argumentIndex,
                            streamId,
                            !hasNext,
                            cancelled,
                            null,
                            hasNext ? enumerator.Current : []);

                        if (!hasNext)
                            await Cleanup();

                        return response;
                    }
                    catch (OperationCanceledException ex) when (
                        ct.IsCancellationRequested ||
                        cancellationToken.IsCancellationRequested)
                    {
                        await Cleanup();

                        return new StreamingResponseDto(
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

                        return new StreamingResponseDto(
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
    public async Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UnRegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        if (StreamingCache.StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    public RemoteAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId})",
                routing.ServiceId, routing.MethodId, routing.RequestId);

        return new RemoteAsyncEnumerable<byte[]>(
            construct: (streamId, enumerator, ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryAdd(key, response =>
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
                return Sender.Send_StreamingRequestServerToClient_ToFabricAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return Sender.Send_StreamingRequestServerToClient_ToFabricAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true),
                    default);

            },

            dispose: streamId =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    public Task SendFireAndForgetAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        var request = new SendRequestDto(routing, data);

        if (Host == null || IsConnected == false)
        {
            return Handle_SendFireAndForget_ToClient_Async(request, ct);
        }

        return Handle_SendFireAndForget_ToFabric_Async(request, ct);
    }

    private async Task Handle_SendFireAndForget_ToFabric_Async(SendRequestDto request, CancellationToken ct)
    {
        // GEEN StreamingCache.PendingFabricSendRequests meer!
        // Schiet hem direct de netwerkqueue in (EnqueueAsync onder water)
        await Sender.Send_SendRequest_ToFabricAsync(request, ct);
        // Direct klaar, geen 100s timeout wachtrij!
    }

    private async Task Handle_SendFireAndForget_ToClient_Async(SendRequestDto request, CancellationToken ct)
    {
        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            try
            {
                // We awaiten dit wel, maar omdat de client-kant nu ook async void/fire-and-forget is,
                // accepteert de WssServerConnection de bytes direct in de queue zonder te wachten.
                await serviceSubscription.SendRequestAsync(request, ct);
            }
            catch (Exception)
            {
                // Zorg dat een falende sessie de streaming voor andere sessies niet permanent blokkeert
            }
        }
    }


    public Task SendAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} SendAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        var request = new SendRequestDto(
            routing,
            data);

        if (Host == null || IsConnected == false)
        {
            return Handle_SendRequest_ToClient_Async(request, ct);
        }

        return Handle_SendRequest_ToFabric_Async(request, ct);
    }
    private async Task Handle_SendRequest_ToFabric_Async(SendRequestDto request, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Handle_SendRequest_ToFabric_Async({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), request.Routing.ServiceId, request.Routing.MethodId, request.Routing.RequestId);

        var completion = StreamingCache.PendingFabricSendRequests.GetOrAdd(
            request.Routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_SendRequest_ToFabricAsync(request, ct);

        try
        {
            var done = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
            return;
        }
        finally
        {
            StreamingCache.PendingFabricSendRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task Handle_SendRequest_ToClient_Async(SendRequestDto request, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Handle_SendRequest_ToClient_Async({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), request.Routing.ServiceId, request.Routing.MethodId, request.Routing.RequestId);

        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            try
            {
                await serviceSubscription.SendRequestAsync(request, ct); // deze is blocking vanuit WssServerConnection
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    public IAsyncEnumerable<byte[]> InvokeAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} InvokeAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        var request = new InvokeRequestDto(
            routing,
            data);
        if (Host == null || IsConnected == false)
        {
            return Handle_FabricInvokeRequest_ToClientAsync(request, ct);
        }
        return Handle_FabricInvokeRequest_ToFabricAsync(request, ct);
    }
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToFabricAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Handle_InvokeRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), request.Routing.ServiceId, request.Routing.MethodId, request.Routing.RequestId);

        var completion = StreamingCache.PendingInvokeRequests.GetOrAdd(
            request.Routing.RequestId,
            _ => new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);

            var enumerable = RegisterRemoteAsyncEnumerableArgumentByte(request.Routing, -1);
            await foreach (var item in enumerable)
            {
                yield return item;
            }
        }
        finally
        {
            StreamingCache.PendingInvokeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_InvokeRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), request.Routing.ServiceId, request.Routing.MethodId, request.Routing.RequestId);

        var SseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
        foreach (var SseServiceSubscription in SseServiceSubscriptions)
        {
            var responses = SseServiceSubscription.InvokeRequestAsync(request, ct);
            await foreach (var response in responses)
            {
                yield return response;
            }
        }
    }

    #endregion

    #region Bus

    public Task BusSubscribeAsync(IBaseFabricConnection baseBusConnection, BusSubscribeDto subscribe, CancellationToken ct)
    {
        return Sender.Send_BusSubscribe_ToFabricAsync(subscribe, ct);
    }
    private Task Receive_BusSubscribeAckAsync(BusSubscribeDto subscribe, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task BusUnsubscribeAsync(IBaseFabricConnection baseBusConnection, BusUnsubscribeDto unsubscribe, CancellationToken ct)
    {
        return Sender.Send_BusUnsubscribe_ToFabricAsync(unsubscribe, ct);
    }
    private Task Receive_BusUnsubscribeAckAsync(BusUnsubscribeDto unsubscribe, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task BusSendFireAndForgetAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationToken ct)
    {
        await Sender.Send_BusSendFireAndForget_ToFabricAsync(request, ct);
    }
    public async Task BusSendAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationToken ct)
    {
        CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        timeoutCts.Token.Register(() => { timeoutCts.Dispose(); });
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
        ct = cts.Token;

        var complete = new TaskCompletionSource<BusSendRequestAckDto>();

        if (StreamingCache.BusSends.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");
        timeoutCts.Token.Register(() => { StreamingCache.BusSends.TryRemove(request.Routing, out _); });

        await Sender.Send_BusSend_ToFabricAsync(request, ct);

        var sendRequestAck = await complete.Task.WaitAsync(cts.Token);
    }
    private async Task Receive_BusSendAckAsync(BusSendRequestAckDto request, CancellationToken ct)
    {
        if (StreamingCache.BusSends.TryRemove(request.Routing, out var complete) == false)
            return;

        if (request.Exception == null)
            complete.SetResult(request);
        else
            complete.SetException(new Exception(request.Exception));
    }
    public async IAsyncEnumerable<byte[]> BusInvokeAsync(IBaseFabricConnection baseBusConnection, BusInvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        timeoutCts.Token.Register(() => { timeoutCts.Dispose(); });
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
        ct = cts.Token;

        var complete = new TaskCompletionSource<BusInvokeRequestAckDto>();
        if (StreamingCache.BusInvokes.TryAdd(request.Routing, complete) == false)
            throw new Exception("Cannot store taskcompletion");
        timeoutCts.Token.Register(() => { StreamingCache.BusInvokes.TryRemove(request.Routing, out _); });

        await Sender.Send_BusInvoke_ToFabricAsync(request, ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(100));
        var invokeRequestAck = await complete.Task.WaitAsync(ct);

        var remoteEnumerable = RegisterBusRemoteAsyncEnumerableArgumentByte(request.Routing, -1);

        await foreach (var item in remoteEnumerable)
        {
            yield return item;
        }
    }
    private async Task Receive_BusInvokeAckAsync(BusInvokeRequestAckDto request, CancellationToken ct)
    {
        if (StreamingCache.BusInvokes.TryRemove(request.Routing, out var complete) == false)
            return;

        if (request.Exception == null)
            complete.SetResult(request);
        else
            complete.SetException(new Exception(request.Exception));
    }


    public Task BusStreamingRequestAsync(IBaseFabricConnection baseBusConnection, BusStreamingRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task BusStreamingResponseAsync(IBaseFabricConnection baseBusConnection, BusStreamingResponseDto response, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private Task Receive_BusFireAndForgetAsync(BusSendRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    private Task Receive_BusSendAsync(BusSendRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    private Task Receive_BusInvokeAsync(BusInvokeRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private RemoteAsyncEnumerable<byte[]> RegisterBusRemoteAsyncEnumerableArgumentByte(BusRoutingDto routing, int argumentIndex)
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

    private Task Receive_BusStreamingRequestAsync(BusStreamingRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    private async Task Receive_BusStreamingResponseAsync(BusStreamingResponseDto response, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
        if (StreamingCache.BusStreamingResponseHandlers.TryRemove(key, out var handler) == false)
            return;
        handler.Invoke(response);
    }


    #endregion

    #region Helpers
    private IEnumerable<IServiceSubscription> GetServiceSubscriptions(ServerRoutingDto routing)
    {
        if (ServiceSubscriptions.Services.TryGetValue(routing.ServiceId, out var serviceSubscriptions) == false)
            return [];

        return serviceSubscriptions.Values
            .Where(a => a.ClientConnectionId == routing.ClientConnectionId)
            .GroupBy(a => a.ClientConnectionId)
            .Select(a => a.First());
    }

    private IEnumerable<IServiceSubscription> GetServiceSubscriptions(RoutingDto routing)
    {
        if (ServiceSubscriptions.Services.TryGetValue(routing.ServiceId, out var serviceSubscriptions) == false)
            return [];

        return serviceSubscriptions.Values
            .Where(SseServiceSubscription =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (routing.SessionId == null && routing.UserId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (routing.SessionId != null && SseServiceSubscription.SessionId == routing.SessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (routing.UserId != null && SseServiceSubscription.UserId == routing.UserId))
            .GroupBy(a => a.ClientConnectionId)
            .Select(a => a.First());
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