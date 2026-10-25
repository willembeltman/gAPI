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
public sealed class FabricClient
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
    internal BinaryWriter? BinaryWriter;
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

        var service = ServiceSubscriptions.Services.AddOrUpdate(
            serviceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(),
            (a, b) => b);
        service[serviceSubscription.ServiceSubscriptionId] = serviceSubscription;

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

        var service = ServiceSubscriptions.Services.AddOrUpdate(
            serviceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(),
            (a, b) => b);
        service.TryRemove(serviceSubscription.ServiceSubscriptionId, out _);

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

                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = BinaryReader.ReadSendGetSessionCookieDataResponseDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.Log:
                        var log = BinaryReader.ReadWssLoggerLogDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_Log_FromFabricAsync(log, ct);
                        }, ct);
                        break;


                    case FabricHostToClientMessageEnum.FabricSendAndForgetRequest:
                        var fabricSendAndForgetRequest = BinaryReader.ReadSendRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendAndForgetRequest_FromFabricAsync(fabricSendAndForgetRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricSendAndForgetRequestCancelled:
                        var fabricSendAndForgetRequestCancelled = BinaryReader.ReadSendRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendAndForgetRequestCancelled_FromFabricAsync(fabricSendAndForgetRequestCancelled, ct);
                        }, ct);
                        break;


                    case FabricHostToClientMessageEnum.FabricSendRequest:
                        var fabricSendRequest = BinaryReader.ReadSendRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendRequest_FromFabricAsync(fabricSendRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricSendRequestCancelled:
                        var fabricSendRequestCancelled = BinaryReader.ReadSendRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendRequestCancelled_FromFabricAsync(fabricSendRequestCancelled, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var fabricSendRequestDone = BinaryReader.ReadSendRequestDoneDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_SendRequestDone_FromFabricAsync(fabricSendRequestDone, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.FabricInvokeRequest:
                        var fabricInvokeRequest = BinaryReader.ReadInvokeRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricInvokeRequest_FromFabricAsync(fabricInvokeRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricInvokeRequestCancelled:
                        var fabricInvokeRequestCancelled = BinaryReader.ReadInvokeRequestCancelledDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricInvokeRequestCancelled_FromFabricAsync(fabricInvokeRequestCancelled, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestAck:
                        var fabricInvokeResponseAck = BinaryReader.ReadInvokeRequestAckDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_InvokeRequestAck_FromFabricAsync(fabricInvokeResponseAck, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestDone:
                        var fabricInvokeResponseDone = BinaryReader.ReadInvokeRequestDoneDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_InvokeRequestDone_FromFabricAsync(fabricInvokeResponseDone, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.StreamingResponse:
                        var fabricStreamingResponse = BinaryReader.ReadStreamingResponseDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_StreamingResponse_FromFabricAsync(fabricStreamingResponse, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingRequest:
                        var fabricStreamingRequest = BinaryReader.ReadStreamingRequestDto();
                        _ = Task.Run(async () =>
                        {
                            await Receive_StreamingRequest_FromFabricAsync(fabricStreamingRequest, ct);
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

    #region Session

    private async Task Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizeFabricIdsDto synchronizeFabricIds, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeFabricIds);

        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;

        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} FabricClient {FabricConnectionId.Value} started");
    }
    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionResponse);

        // Zoek de wachtende taak op en zet het resultaat zodra het antwoord binnen is
        if (StreamingCache.PendingGetSessionRequests.TryRemove(getSessionResponse.SessionId, out var tcs))
            tcs.TrySetResult(getSessionResponse.CookieData);
    }
    private async Task Receive_Log_FromFabricAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        // niet loggen ;)
#pragma warning disable CA2254 // Template should be a static expression
#pragma warning disable CA1873 // Avoid potentially expensive logging

        Logger.Log(log.Level, log.Message);

#pragma warning restore CA2254 // Template should be a static expression
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }

    #endregion

    #region Wss/Sse

    private async Task Receive_FabricSendAndForgetRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricSendAndForgetRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Send_FabricSendAndForgetRequest_ToClientAsync(message, ct);
        }
    }
    private async Task Receive_FabricSendAndForgetRequestCancelled_FromFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricSendAndForgetRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in serviceSubscriptions)
        {
            await serviceSubscription.Send_FabricSendAndForgetRequestCancelled_ToClientAsync(message, ct);
        }
    }

    private async Task Receive_FabricSendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
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
    }
    private async Task Receive_FabricSendRequestCancelled_FromFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricSendRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var serviceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in serviceSubscriptions)
        {
            await serviceSubscription.Send_FabricSendRequestCancelled_ToClientAsync(message, ct);
        }
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SendRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.PendingFabricSendRequests.TryRemove(message.Routing.RequestId, out var completion))
            completion.TrySetResult(message);
    }

    private async Task Receive_FabricInvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        var cts = new LinkedCancellationTokenSourceWithTimeout(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        // Routeren naar client(s)
        var called = false;
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Send_FabricInvokeRequest_ToClientAsync(message, ct);
            called = true;
        }

        if (!called)
            await Sender.Send_InvokeRequestAck_ToFabricAsync(new(message.Routing), ct);
    }
    private async Task Receive_FabricInvokeRequestCancelled_FromFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Send_FabricInvokeRequestCancelled_ToClientAsync(message, ct);
        }
    }
    private async Task Receive_InvokeRequestAck_FromFabricAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_InvokeRequestAck_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.PendingFabricInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
            completion.TrySetResult(message);
    }
    private async Task Receive_InvokeRequestDone_FromFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_InvokeRequestAck_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.PendingFabricInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Receive_StreamingRequest_FromFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_StreamingRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (message.ArgumentIndex < 0)
        {
            // Routeren naar client(s)
            var called = false;
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricStreamingRequest_ToClientAsync(message, ct);
                called = true;
            }

            if (!called)
                await Sender.Send_StreamingResponse_ToFabricAsync(new(message.Routing, message.ArgumentIndex, message.StreamId, true, false, "No clients subscribed", []), ct);
        }
        else
        {
            if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, false, ct);
                await Sender.Send_StreamingResponse_ToFabricAsync(response, ct);
            }
            else
                await Sender.Send_StreamingResponse_ToFabricAsync(new(message.Routing, message.ArgumentIndex, message.StreamId, true, false, "No StreamingRequestHandlers found", []), ct);
        }
    }
    private async Task Receive_StreamingResponse_FromFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_StreamingResponse_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (message.ArgumentIndex < 0)
        {
            var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
            if (StreamingCache.StreamingResponseHandlers.TryGetValue(key, out var responseHandler))
            {
                responseHandler.Invoke(message);
            }
        }
        else
        {
            // Routeren naar client(s)
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricStreamingResponse_ToClientAsync(message, ct);
            }
        }
    }

    #endregion

    #endregion

    #region Calls vanuit de connection

    #region Wss / Sse

    public async Task Handle_FabricSendRequestDone_FromClientAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        // Voor als er geen fabric is
        if (StreamingCache.PendingClientSendRequests.TryRemove(message.Routing.RequestId, out var completion))
            completion.TrySetResult(message);
        else
        {
            if (IsConnected)
            {
                await Sender.Send_SendRequestDone_ToFabricAsync(message, ct);
            }
            else
            {
                await Receive_SendRequestDone_FromFabricAsync(message, ct);
            }
        }
    }

    public async Task Handle_FabricInvokeRequestAck_FromClientAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            await Sender.Send_InvokeRequestAck_ToFabricAsync(message, ct);
        }
        else
        {
            await Receive_InvokeRequestAck_FromFabricAsync(message, ct);
        }
    }
    public async Task Handle_FabricInvokeRequestDone_FromClientAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            await Sender.Send_InvokeRequestDone_ToFabricAsync(message, ct);
        }
        else
        {
            await Receive_InvokeRequestDone_FromFabricAsync(message, ct);
        }
    }

    public Task Handle_FabricStreamingRequest_FromClientAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            return Sender.Send_StreamingRequest_ToFabricAsync(message, ct);
        }
        else
        {
            return Receive_StreamingRequest_FromFabricAsync(message, ct);
        }
    }
    public Task Handle_FabricStreamingResponse_FromClientAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            return Sender.Send_StreamingResponse_ToFabricAsync(message, ct);
        }
        else
        {
            return Receive_StreamingResponse_FromFabricAsync(message, ct);
        }
    }

    public async Task<StreamingResponseDto?> Handle_StreamingRequest_FromClientAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            var response = await handler.Invoke(message.StreamId, false, ct);
            return response;
        }
        return null;
    }
    public async Task Handle_StreamingResponse_FromClientAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
            responseHandler.Invoke(message);
    }

    public RemoteAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte_ToClient(IServiceSubscription sseServiceSubscription, RoutingDto routing, int argumentIndex)
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
                return sseServiceSubscription.Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return sseServiceSubscription.Send_StreamingRequest_ToClientAsync(
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

    //public IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument_ToClient<T>(IServiceSubscription wssServerConnection, RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    //{
    //    if (Logger.IsEnabled(LogLevel.Trace))
    //        Logger.LogTrace(
    //            "{now} RegisterRemoteAsyncEnumerableArgument({routing}, {argumentIndex})",
    //            DateTime.Now.ToString("HH:mm:ss.fff"),
    //            routing,
    //            argumentIndex);

    //    return new RemoteAsyncEnumerable<T>(
    //        construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
    //        {
    //            var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
    //            StreamingCache.StreamingResponseHandlers.TryAdd(key, response =>
    //            {
    //                if (response.IsCancelled)
    //                {
    //                    enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
    //                    return;
    //                }

    //                if (response.ExceptionMessage != null)
    //                {
    //                    enumerator.Complete(new RemoteException(response.ExceptionMessage));
    //                    return;
    //                }

    //                if (response.IsCompleted)
    //                {
    //                    enumerator.Complete();
    //                    return;
    //                }

    //                try
    //                {
    //                    enumerator.Push(deserializer.Invoke(response.BinaryData));
    //                }
    //                catch (Exception ex)
    //                {
    //                    enumerator.Complete(ex);
    //                }
    //            });
    //        },

    //        requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
    //        {
    //            return wssServerConnection.Send_StreamingRequest_ToClientAsync(
    //                new StreamingRequestDto(
    //                    routing,
    //                    argumentIndex,
    //                    streamId,
    //                    false),
    //                ct);
    //        },

    //        cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
    //        {
    //            return wssServerConnection.Send_StreamingRequest_ToClientAsync(
    //                new StreamingRequestDto(
    //                    routing,
    //                    argumentIndex,
    //                    streamId,
    //                    true),
    //                default);
    //        },

    //        dispose: (StreamId streamId) =>
    //        {
    //            var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
    //            StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
    //        });
    //}


    #endregion

    #endregion

    #region Call's vanuit gegenereerde code

    #region Wss / Sse
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
                return Send_StreamingRequest_ResponseEnumerable_ToFabricAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return Send_StreamingRequest_ResponseEnumerable_ToFabricAsync(
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

    private Task Send_StreamingRequest_ResponseEnumerable_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (IsConnected)
        {
            return Sender.Send_StreamingRequest_ToFabricAsync(message, ct);
        }
        else
        {
            return Receive_StreamingRequest_FromFabricAsync(message, ct);
        }
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
        await Sender.Send_SendAndForgetRequest_ToFabricAsync(request, ct);
        // Direct klaar, geen 100s timeout wachtrij!
        ct.Register(async () =>
        {
            await Sender.Send_SendAndForgetRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });
    }
    private async Task Handle_SendFireAndForget_ToClient_Async(SendRequestDto request, CancellationToken ct)
    {
        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            // GEEN StreamingCache.PendingFabricSendRequests meer!
            // Schiet hem direct de netwerkqueue in (EnqueueAsync onder water)
            await serviceSubscription.Send_FabricSendAndForgetRequest_ToClientAsync(request, ct);
            // Direct klaar, geen 100s timeout wachtrij!
            ct.Register(async () =>
            {
                await serviceSubscription.Send_FabricSendAndForgetRequestCancelled_ToClientAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

            //try
            //{
            //    // We awaiten dit wel, maar omdat de client-kant nu ook async void/fire-and-forget is,
            //    // accepteert de WssServerConnection de bytes direct in de queue zonder te wachten.
            //    await serviceSubscription.SendAndForgetRequestAsync(request, ct);
            //}
            //catch (Exception)
            //{
            //    // Zorg dat een falende sessie de streaming voor andere sessies niet permanent blokkeert
            //}
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
        var completion = StreamingCache.PendingFabricSendRequests.GetOrAdd(
            request.Routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_SendRequest_ToFabricAsync(request, ct);

        ct.Register(async () =>
        {
            await Sender.Send_SendRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        try
        {
            var done = await completion.Task.WaitAsync(request.Routing.Timeout, ct);
            return;
        }
        finally
        {
            StreamingCache.PendingFabricSendRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task Handle_SendRequest_ToClient_Async(SendRequestDto request, CancellationToken ct)
    {
        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            var completion = StreamingCache.PendingClientSendRequests.GetOrAdd(
                request.Routing.RequestId,
                _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
            await serviceSubscription.Send_FabricSendRequest_ToClientAsync(request, ct);

            ct.Register(async () =>
            {
                await serviceSubscription.Send_FabricSendRequestCancelled_ToClientAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

            try
            {
                var done = await completion.Task.WaitAsync(request.Routing.Timeout, ct);
                return;
            }
            finally
            {
                StreamingCache.PendingClientSendRequests.TryRemove(request.Routing.RequestId, out _);
            }
        }
    }

    public IAsyncEnumerable<byte[]> InvokeAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
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
        var completion = new TaskCompletionSource<InvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PendingFabricInvokeRequests.TryAdd(request.Routing.RequestId, completion) == false)
        {
            throw new Exception("Cache");
        }
        await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

        ct.Register(async () =>
        {
            await Sender.Send_InvokeRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
        });

        try
        {
            var response = await completion.Task.WaitAsync(request.Routing.Timeout, ct);
            var enumerable = RegisterRemoteAsyncEnumerableArgumentByte(request.Routing, -1);
            await foreach (var item in enumerable)
            {
                yield return item;
            }
        }
        finally
        {
            StreamingCache.PendingFabricInvokeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        var sseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
        foreach (var sseServiceSubscription in sseServiceSubscriptions)
        {
            var completion = StreamingCache.PendingClientInvokeRequests.GetOrAdd(
                request.Routing.RequestId,
                _ => new TaskCompletionSource<InvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously));
            await sseServiceSubscription.Send_FabricInvokeRequest_ToClientAsync(request, ct);

            ct.Register(async () =>
            {
                await sseServiceSubscription.Send_FabricInvokeRequestCancelled_ToClientAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

            try
            {
                var response = await completion.Task.WaitAsync(request.Routing.Timeout, ct);
                var enumerable = RegisterRemoteAsyncEnumerableArgumentByte_ToClient(sseServiceSubscription, request.Routing, -1);
                await foreach (var item in enumerable)
                {
                    yield return item;
                }
            }
            finally
            {
                StreamingCache.PendingClientInvokeRequests.TryRemove(request.Routing.RequestId, out _);
            }
        }
    }

    #endregion

    #endregion

    #region Helpers

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