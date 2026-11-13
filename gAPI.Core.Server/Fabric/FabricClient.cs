using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql.Internal;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace gAPI.Core.Server.Fabric;

// Deze class houd alle communicatie consistentie in de gate, en routeert alles. Voor de WssServerConnection is dit gewoon het doorgeef luik "doe call naar client"
// De fabricReceiver is wel autonoom wat betreft de afhandeling richting fabric, dus als er een bericht terug moet naar de fabric doet hij dit zelf.
// De fabricSender is zo dom mogelijk
public sealed class FabricClient
{
    public FabricClient(
        SessionCache sessionCache,
        StreamingCache streamingCache,
        ServerConnectionCollection connections,
        ServiceSubscriptionCollection serviceSubscriptions,
        ILoggerFactory loggerFactory,
        ServerConfig config)
    {
        Sender = new FabricClientSender(this, loggerFactory);

        LocalSessionCache = sessionCache;
        StreamingCache = streamingCache;
        Connections = connections;
        ServiceSubscriptions = serviceSubscriptions;
        Logger = loggerFactory.CreateLogger<FabricClient>();
        Config = config;

        if (!string.IsNullOrEmpty(config.FabricConnectionString))
        {
            if (config.FabricConnectionString.Contains('='))
            {
                var parts = config.FabricConnectionString!.Split(';')
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
            else if (Uri.TryCreate(config.FabricConnectionString, UriKind.Absolute, out var uri))
            {
                Host = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 9000;
            }
            else if (config.FabricConnectionString.Contains(':'))
            {
                var split = config.FabricConnectionString.Split(':');
                Host = split[0].Trim();
                Port = int.TryParse(split[1].Trim(), out var p) ? p : 9000;
            }
            else
            {
                Host = config.FabricConnectionString.Trim();
                Port = 9000;
            }

            UseFabric = true;
        }
        else
        {
            UseFabric = false;
        }
    }

    readonly ILogger Logger;

    public ServerConfig Config { get; }

    readonly string Host = string.Empty;
    readonly int Port;
    readonly bool UseFabric;

    readonly StreamingCache StreamingCache;
    readonly SessionCache LocalSessionCache;

    readonly ServerConnectionCollection Connections;
    readonly ServiceSubscriptionCollection ServiceSubscriptions;

    readonly FabricClientSender Sender;
    TcpClient? Tcp;
    NetworkStream? Stream;

    public BinaryWriter? Writer { get; private set; }

    CancellationTokenSource? ConnectedCts;
    private bool RunningRequested;
    private CancellationToken? LifeCycleCt;
    SemaphoreSlim ConnectingGate = new SemaphoreSlim(1);

    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public bool IsConnected => Tcp?.Connected == true;

    #region Connection

    public void Start(CancellationToken ct)
    {
        LifeCycleCt = ct;
        if (UseFabric == false)
            return;

        _ = Task.Run(ConnectAsync);
        _ = Task.Run(async () => { await Sender.SendKernel(ct); });
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
                    await ReceiveKernel(reader, ConnectedCts.Token);

                    if (RunningRequested)
                        await ReconnectAsync();
                });
            }
            catch
            {
                try
                {
                    Writer = null;
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
        if (LifeCycleCt == null)
            throw new Exception("Not initialized");

        if (UseFabric == false)
        {
            return;
        }

        await Gated_DisconnectAsync();
        //await BusConnection.OnDisconnectedAsync(LifeCycleCt.Value);

        await Gated_ConnectAsync();
        foreach (var serviceSubscription in ServiceSubscriptions.All)
        {
            var request = new SubscribeDto(
                serviceSubscription.ServiceSubscriptionId,
                serviceSubscription.ServiceId,
                serviceSubscription.UserId,
                serviceSubscription.SessionId);
            await Sender.Send_Subscribe_ToFabricAsync(request, LifeCycleCt.Value);
        }

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Reconnecting BusFabricClient DONE", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting BusFabricClient {Id}",
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

        await ConnectedCts.CancelAsync();

        Stream?.Dispose();
        Stream = null;

        Tcp?.Dispose();
        Tcp = null;

        ConnectedCts.Dispose();
        ConnectedCts = null;
    }

    #endregion

    #region Subscription

    public async Task SubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SubscribeAsync({SseServiceSubscription})",
                serviceSubscription);

        var service = ServiceSubscriptions.Services.GetOrAdd(
            serviceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>());
        if (service.TryAdd(serviceSubscription.ServiceSubscriptionId, serviceSubscription) == false)
            throw new Exception("Cannot add subscription");

        var request = new SubscribeDto(
            serviceSubscription.ServiceSubscriptionId,
            serviceSubscription.ServiceId,
            serviceSubscription.UserId,
            serviceSubscription.SessionId);

        if (UseFabric == false)
        {
            if (StreamingCache.SubscribeTcss.TryRemove(request.ServiceSubscriptionId, out var tcs))
            {
                var ack = new SubscribeAckDto(request, false, null);
                tcs.SetResult(ack);
            }
            return;
        }

        await Sender.Send_Subscribe_ToFabricAsync(request, ct);
    }
    public async Task UnsubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({SseServiceSubscription})",
                serviceSubscription);

        var service = ServiceSubscriptions.Services.GetOrAdd(
            serviceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>());
        if (service.TryRemove(serviceSubscription.ServiceSubscriptionId, out _) == false)
            throw new Exception("Error getting service subscription");

        var request = new UnsubscribeDto(
            serviceSubscription.ServiceSubscriptionId,
            serviceSubscription.ServiceId,
            serviceSubscription.UserId,
            serviceSubscription.SessionId
        );

        if (UseFabric == false)
        {
            if (StreamingCache.UnsubscribeTcss.TryRemove(request.ServiceSubscriptionId, out var tcs))
            {
                var ack = new UnsubscribeAckDto(request, false, null);
                tcs.SetResult(ack);
            }
            return;
        }

        await Sender.Send_Unsubscribe_ToFabricAsync(request, ct);
    }

    #endregion

    #region Session

    public async Task UpdateSession(SessionId sessionId, string? cookieData, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UpdateSession({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        if (UseFabric == false)
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

        if (UseFabric == false)
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
        if (UseFabric == false)
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

    #region Receiver

    public async Task ReceiveKernel(BinaryReader BinaryReader, CancellationToken ct)
    {
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
                        var synchronizeFabricIds = BinaryReader.ReadSynchronizeFabricIdsDto(Config.MaxPackageSize ?? 512 * 1024);
                        await Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                        break;

                    case FabricHostToClientMessageEnum.SubscribeAck:
                        var subscribeAck = BinaryReader.ReadSubscribeAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        await Received_SubscribeAck_FromFabric(subscribeAck, ct);
                        break;
                    case FabricHostToClientMessageEnum.UnsubscribeAck:
                        var unsubscribeAck = BinaryReader.ReadUnsubscribeAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        await Received_UnsubscribeAck_FromFabric(unsubscribeAck, ct);
                        break;

                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = BinaryReader.ReadSendGetSessionCookieDataResponseDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.Log:
                        var log = BinaryReader.ReadFabricLogDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_Log_FromFabricAsync(log, ct);
                        }, ct);
                        break;


                    case FabricHostToClientMessageEnum.FabricSendAndForgetRequest:
                        var fabricSendAndForgetRequest = BinaryReader.ReadSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendAndForgetRequest_FromFabricAsync(fabricSendAndForgetRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricSendAndForgetRequestCancelled:
                        var fabricSendAndForgetRequestCancelled = BinaryReader.ReadSendRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendAndForgetRequestCancelled_FromFabricAsync(fabricSendAndForgetRequestCancelled, ct);
                        }, ct);
                        break;


                    case FabricHostToClientMessageEnum.FabricSendRequest:
                        var fabricSendRequest = BinaryReader.ReadSendRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendRequest_FromFabricAsync(fabricSendRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricSendRequestCancelled:
                        var fabricSendRequestCancelled = BinaryReader.ReadSendRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricSendRequestCancelled_FromFabricAsync(fabricSendRequestCancelled, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var fabricSendRequestDone = BinaryReader.ReadSendRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_SendRequestDone_FromFabricAsync(fabricSendRequestDone, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.FabricInvokeRequest:
                        var fabricInvokeRequest = BinaryReader.ReadInvokeRequestDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricInvokeRequest_FromFabricAsync(fabricInvokeRequest, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.FabricInvokeRequestCancelled:
                        var fabricInvokeRequestCancelled = BinaryReader.ReadInvokeRequestCancelledDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_FabricInvokeRequestCancelled_FromFabricAsync(fabricInvokeRequestCancelled, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestAck:
                        var fabricInvokeResponseAck = BinaryReader.ReadInvokeRequestAckDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_InvokeRequestAck_FromFabricAsync(fabricInvokeResponseAck, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestDone:
                        var fabricInvokeResponseDone = BinaryReader.ReadInvokeRequestDoneDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_InvokeRequestDone_FromFabricAsync(fabricInvokeResponseDone, ct);
                        }, ct);
                        break;

                    case FabricHostToClientMessageEnum.StreamingResponse:
                        var fabricStreamingResponse = BinaryReader.ReadStreamingResponseDto(Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            await Receive_StreamingResponse_FromFabricAsync(fabricStreamingResponse, ct);
                        }, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingRequest:
                        var fabricStreamingRequest = BinaryReader.ReadStreamingRequestDto(Config.MaxPackageSize ?? 512 * 1024);
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
    private async Task Receive_Log_FromFabricAsync(FabricLogDto log, CancellationToken ct)
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

    private async Task Received_SubscribeAck_FromFabric(SubscribeAckDto ack, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SubscribeAck_FromFabric({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), ack);

            if (StreamingCache.SubscribeTcss.TryRemove(ack.SubscribeDto.ServiceSubscriptionId, out var tcs))
            {
                tcs.SetResult(ack);
            }
        }, ct);
    }
    private async Task Received_UnsubscribeAck_FromFabric(UnsubscribeAckDto ack, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_UnsubscribeAck_FromFabric({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), ack);

            if (StreamingCache.UnsubscribeTcss.TryRemove(ack.UnsubscribeDto.ServiceSubscriptionId, out var tcs))
            {
                tcs.SetResult(ack);
            }
        }, ct);
    }


    private async Task Receive_FabricSendAndForgetRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricSendAndForgetRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Connection.Send_FabricSendAndForgetRequest_ToClientAsync(message, ct);
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
            await serviceSubscription.Connection.Send_FabricSendAndForgetRequestCancelled_ToClientAsync(message, ct);
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
            await serviceSubscription.Connection.Send_FabricSendRequest_ToClientAsync(message, ct);
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
            await serviceSubscription.Connection.Send_FabricSendRequestCancelled_ToClientAsync(message, ct);
        }
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_SendRequestDone_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.PendingFabricSendRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Receive_FabricInvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
        StreamingCache.Timeouts[message.Routing.RequestId] = cts;

        // Routeren naar client(s)
        var called = false;
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Connection.Send_FabricInvokeRequest_ToClientAsync(message, ct);
            called = true;
        }

        if (!called)
            await Sender.Send_InvokeRequestAck_ToFabricAsync(new(message.Routing, false, null), ct);
    }
    private async Task Receive_FabricInvokeRequestCancelled_FromFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_FabricInvokeRequestCancelled_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        // Routeren naar client(s)
        var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
        foreach (var serviceSubscription in SseServiceSubscriptions)
        {
            await serviceSubscription.Connection.Send_FabricInvokeRequestCancelled_ToClientAsync(message, ct);
        }
    }
    private async Task Receive_InvokeRequestAck_FromFabricAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_InvokeRequestAck_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

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
    private async Task Receive_InvokeRequestDone_FromFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_InvokeRequestAck_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingCache.PendingFabricInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }

        if (StreamingCache.StreamingRequestHandlers.TryRemove(new(message.Routing.RequestId, -1), out var handler))
        {
            await handler.DisposeAsync();
        }
    }

    private async Task Receive_StreamingRequest_FromFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_StreamingRequest_FromFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (message.ArgumentIndex < 0)
        {
            // Routeren naar client(s)
            var called = false;
            var SseServiceSubscriptions = GetServiceSubscriptions(message.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Connection.Send_FabricStreamingRequest_ToClientAsync(message, ct);
                called = true;
            }

            if (!called)
                await Sender.Send_StreamingResponse_ToFabricAsync(new(message.Routing, message.ArgumentIndex, message.StreamId, true, false, message.Routing.ThrowIfNoReceiver ? "No clients subscribed" : null, []), ct);
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
            timeout.CancelAfter(message.Routing.Timeout);

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
                await serviceSubscription.Connection.Send_FabricStreamingResponse_ToClientAsync(message, ct);
            }
        }
    }

    #endregion

    #endregion

    #region Calls vanuit de connection

    public async Task Handle_FabricSendRequestDone_FromClientAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        // Voor als er geen fabric is
        if (StreamingCache.PendingClientSendRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
        else
        {
            if (UseFabric)
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
        if (StreamingCache.PendingClientInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
        else
        {
            if (UseFabric)
            {
                await Sender.Send_InvokeRequestAck_ToFabricAsync(message, ct);
            }
            else
            {
                await Receive_InvokeRequestAck_FromFabricAsync(message, ct);
            }
        }
    }
    public async Task Handle_FabricInvokeRequestDone_FromClientAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (StreamingCache.PendingClientInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
        else
        {
            if (UseFabric)
            {
                await Sender.Send_InvokeRequestDone_ToFabricAsync(message, ct);
            }
            else
            {
                await Receive_InvokeRequestDone_FromFabricAsync(message, ct);
            }
        }

        if (StreamingCache.StreamingRequestHandlers.TryRemove(new(message.Routing.RequestId, -1), out var handler))
        {
            await handler.DisposeAsync();
        }
    }

    public Task Handle_FabricStreamingRequest_FromClientAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (UseFabric)
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
        if (UseFabric)
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
            timeout.CancelAfter(message.Routing.Timeout);

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
            timeout.CancelAfter(message.Routing.Timeout);

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
            timeOut: routing.Timeout,
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
                return sseServiceSubscription.Connection.Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return sseServiceSubscription.Connection.Send_StreamingRequest_ToClientAsync(
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

    #endregion

    #region Call's vanuit gegenereerde code

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        if (StreamingCache.StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<T>(
                async (activeStreams, streamId, cancelled, ct) =>
                {
                    var stream = activeStreams.GetOrAdd(
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
                        await stream.DisposeAsync();
                        stream.linkedCts.Dispose();
                    }

                    var entered = false;
                    try
                    {
                        await stream.gate.WaitAsync(ct);
                        entered = true;

                        var hasNext = !cancelled && await stream.enumerator.MoveNextAsync(ct);
                        var response = new StreamingResponseDto(
                            routing,
                            argumentIndex,
                            streamId,
                            !hasNext,
                            cancelled,
                            null,
                            hasNext ? serializer(stream.enumerator.Current) : []);

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
                            stream.gate.Release();
                    }
                })) == false)
            throw new NotImplementedException();
    }
    public async Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UnRegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        if (StreamingCache.StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
            await registration.DisposeAsync();
    }

    public RemoteAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId})",
                routing.ServiceId, routing.MethodId, routing.RequestId);

        return new RemoteAsyncEnumerable<byte[]>(
            timeOut: routing.Timeout,
            construct: (streamId, enumerator, ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                if (StreamingCache.StreamingResponseHandlers.TryAdd(key, response =>
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
                }) == false)
                    throw new Exception("Cache");
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
        if (UseFabric)
        {
            return Sender.Send_StreamingRequest_ToFabricAsync(message, ct);
        }
        else
        {
            return Receive_StreamingRequest_FromFabricAsync(message, ct);
        }
    }

    public Task SendFireAndForgetAsync(RoutingDto routing, byte[] data)
    {
        if (LifeCycleCt == null)
            throw new Exception("BusFabric not initilazed");

        var request = new SendRequestDto(routing, data);

        if (UseFabric == false)
        {
            return Handle_SendFireAndForget_ToClient_Async(request, LifeCycleCt.Value);
        }

        return Handle_SendFireAndForget_ToFabric_Async(request, LifeCycleCt.Value);
    }
    private async Task Handle_SendFireAndForget_ToFabric_Async(SendRequestDto request, CancellationToken ct)
    {
        try
        {
            await Sender.Send_SendAndForgetRequest_ToFabricAsync(request, ct);

            ct.Register(async () =>
            {
                await Sender.Send_SendRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("FireAndForget chunk afgebroken vanwege time-out.");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fout opgevangen in FireAndForget streaming bus-call");
            throw;
        }
    }
    private async Task Handle_SendFireAndForget_ToClient_Async(SendRequestDto request, CancellationToken ct)
    {
        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            // GEEN StreamingCache.PendingFabricSendRequests meer!
            // Schiet hem direct de netwerkqueue in (EnqueueAsync onder water)
            await serviceSubscription.Connection.Send_FabricSendAndForgetRequest_ToClientAsync(request, ct);
        }
    }

    public Task SendAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} SendAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        var request = new SendRequestDto(
            routing,
            data);

        if (UseFabric == false)
        {
            return Handle_SendRequest_ToClient_Async(request, timeoutCts);
        }

        return Handle_SendRequest_ToFabric_Async(request, timeoutCts);
    }
    private async Task Handle_SendRequest_ToFabric_Async(SendRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;
        var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PendingFabricSendRequests.TryAdd(request.Routing.RequestId, completion) == false)
        {
            throw new Exception("Cache");
        }

        try
        {
            await Sender.Send_SendRequest_ToFabricAsync(request, ct);

            ct.Register(async () =>
            {
                await Sender.Send_SendRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

            var done = await completion.Task.WaitAsync(request.Routing.Timeout, ct);
            return;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("FireAndForget chunk afgebroken vanwege time-out (30s).");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fout opgevangen in FireAndForget streaming bus-call");
            throw;
        }
        finally
        {
            StreamingCache.PendingFabricSendRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task Handle_SendRequest_ToClient_Async(SendRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;
        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (StreamingCache.PendingClientSendRequests.TryAdd(request.Routing.RequestId, completion) == false)
            {
                throw new Exception("Cache");
            }
            await serviceSubscription.Connection.Send_FabricSendRequest_ToClientAsync(request, ct);

            ct.Register(async () =>
            {
                await serviceSubscription.Connection.Send_FabricSendRequestCancelled_ToClientAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
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

    public IAsyncEnumerable<byte[]> InvokeAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts)
    {
        var request = new InvokeRequestDto(
            routing,
            data);
        if (UseFabric == false)
        {
            return Handle_FabricInvokeRequest_ToClientAsync(request, timeoutCts);
        }
        return Handle_FabricInvokeRequest_ToFabricAsync(request, timeoutCts);
    }
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToFabricAsync(InvokeRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;
        var completion = new TaskCompletionSource<InvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (StreamingCache.PendingFabricInvokeRequests.TryAdd(request.Routing.RequestId, completion) == false)
        {
            throw new Exception("Cache");
        }

        try
        {
            await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

            ct.Register(async () =>
            {
                await Sender.Send_InvokeRequestCancelled_ToFabricAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
            });

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
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToClientAsync(InvokeRequestDto request, CancellationTokenSourceDispose timeoutCts)
    {
        var ct = timeoutCts.Token;
        var sseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
        foreach (var sseServiceSubscription in sseServiceSubscriptions)
        {
            var completion = new TaskCompletionSource<InvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (StreamingCache.PendingClientInvokeRequests.TryAdd(request.Routing.RequestId, completion) == false)
            {
                throw new Exception("Cache");
            }
            await sseServiceSubscription.Connection.Send_FabricInvokeRequest_ToClientAsync(request, ct);

            ct.Register(async () =>
            {
                await sseServiceSubscription.Connection.Send_FabricInvokeRequestCancelled_ToClientAsync(new(request.Routing, "Task is cancelled"), CancellationToken.None);
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
        if (ConnectedCts != null)
        {
            await ConnectedCts.CancelAsync();
            ConnectedCts.Dispose();
        }
    }

}