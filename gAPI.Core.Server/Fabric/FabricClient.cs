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
using System.Runtime.CompilerServices;

namespace gAPI.Core.Server.Fabric;

// Deze class houd alle communicatie consistentie in de gate, en routeert alles. Voor de WssServerConnection is dit gewoon het doorgeef luik "doe call naar client"
// De fabricReceiver is wel autonoom wat betreft de afhandeling richting fabric, dus als er een bericht terug moet naar de fabric doet hij dit zelf.
// De fabricSender is zo dom mogelijk
public sealed class FabricClient : IAsyncDisposable
{
    public FabricClient(
        SessionCache sessionCache,
        StreamingCache requestCache,
        ServiceSubscriptionCollection serviceSubscriptions,
        ILoggerFactory loggerFactory,
        string? fabricConnectionString)
    {
        Sender = new FabricClientSender(this, loggerFactory);

        LocalSessionCache = sessionCache;
        StreamingCache = requestCache;
        ServiceSubscriptions = serviceSubscriptions;
        Logger = loggerFactory.CreateLogger<FabricClient>();

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

    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public bool IsConnected => IsDisconnecting || IsConnecting || Tcp?.Connected == true;

    #region Connection

    public async Task ConnectAsync()
    {
        if (Host == null || Port == null) return;
        if (IsConnected || IsDisconnecting) return;

        try
        {
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation($"Starting FabricClient");

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
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation(
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

            // Maak een time-out van 30 seconden aan
            using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            // Koppel de time-out aan de meegegeven CancellationToken van de gebruiker
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, ctsTimeout.Token);

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
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning("{now} FabricClient {Id.Value} started", DateTime.Now.ToString("HH:mm:ss.fff"), FabricConnectionId.Value);

            while (!ct.IsCancellationRequested)
            {
                var messageType = FabricConverter.ReadHostToClientMessageType(BinaryReader);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ReceiveKernel({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);
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
                        var fabricInvokeResponseReady = BinaryReader.ReadInvokeRequestDoneDto();
                        await Receive_InvokeRequestDone_FromFabricAsync(fabricInvokeResponseReady, ct);
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
                        await Receive_StreamingRequest_ClientToServer_FromFabricAsync(argumentRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingResponseClientToServer:
                        var argumentResponse = BinaryReader.ReadStreamingResponseDto();
                        await Receive_StreamingResponse_ClientToServer_FromFabricAsync(argumentResponse, ct);
                        break;

                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = BinaryReader.ReadSendGetSessionCookieDataResponseDto();
                        await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        break;
                    case FabricHostToClientMessageEnum.Log:
                        var log = BinaryReader.ReadWssLoggerLogDto();
                        await Receive_Log_FromFabricAsync(log, ct);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "FabricClient #{Id.Value}: Exception occured, restarting fabric client\r\n{ex}",
                    FabricConnectionId?.Value,
                    ex);
            }
        }

        await ReconnectAsync(ct); // Letop deze moet naar boven
    }

    private async Task Receive_FabricSendRequest_FromFabricAsync(SendRequestDto request, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequest_FromFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

            // Routeren naar client(s)
            var SseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricSendRequest_ToClientAsync(request, ct);
            }

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
    private async Task Receive_FabricSendRequestCancelled_FromFabricAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestCancelled_FromFabricAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

            // Routeren naar client(s)
            var serviceSubscriptions = GetServiceSubscriptions(cancel.Routing);
            foreach (var serviceSubscription in serviceSubscriptions)
            {
                await serviceSubscription.Send_FabricSendRequestCancelled_ToClientAsync(cancel, ct);
            }
        }, ct);
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestDone_FromFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

            if (StreamingCache.PendingFabricSendRequests.TryRemove(done.Routing.RequestId, out var completion))
                completion.TrySetResult(done);
        }, ct);
    }

    private async Task Receive_FabricInvokeRequest_FromFabricAsync(InvokeRequestDto request, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequest_FromFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

            // Routeren naar client(s)
            var SseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricInvokeRequest_ToClientAsync(request, ct);
            }

            //try
            //{

            //    if (StreamingCache.Timeouts.TryGetValue(request.Routing.RequestId, out var timeout))
            //        timeout.Reset();

            //    var enumerable = Handle_FabricInvokeRequest_ToClientAsync(request, ct);
            //    RegisterAsyncEnumerableArgumentByte(request.Routing, -1, enumerable, ct);

            //    await Sender.Send_InvokeRequestDone_ToFabricAsync(
            //        new InvokeRequestDoneDto(
            //            request.Routing,
            //            false,
            //            null
            //        ), ct);
            //}
            //catch (Exception ex)
            //{
            //    await Sender.Send_InvokeRequestDone_ToFabricAsync(
            //        new InvokeRequestDoneDto(
            //            request.Routing,
            //            false,
            //            ex.Message
            //        ), ct);
            //}
        }, ct);
    }
    private async Task Receive_FabricInvokeRequestCancelled_FromFabricAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequestCancelled_FromFabricAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

            // Routeren naar client(s)
            var SseServiceSubscriptions = GetServiceSubscriptions(cancel.Routing);
            foreach (var serviceSubscription in SseServiceSubscriptions)
            {
                await serviceSubscription.Send_FabricInvokeRequestCancelled_ToClientAsync(cancel, ct);
            }
        }, ct);
    }
    private async Task Receive_InvokeRequestDone_FromFabricAsync(InvokeRequestDoneDto invokeResponseDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequestDone_FromFabricAsync({invokeResponseDone})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeResponseDone);

            if (StreamingCache.PendingFabricInvokeRequests.TryRemove(invokeResponseDone.Routing.RequestId, out var completion))
                completion.TrySetResult(invokeResponseDone);
        }, ct);
    }

    private async Task Receive_StreamingRequest_ServerToClient_FromFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingRequestServerToClient_FromFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

            var serviceSubscriptions = GetServiceSubscriptions(request.Routing);
            foreach (var SseServiceSubscription in serviceSubscriptions)
            {
                await SseServiceSubscription.Send_FabricStreamingRequest_ToClientAsync(request, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_ServerToClient_FromFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingResponse_ServerToClient_FromFabricAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

            var serviceSubscriptions = GetServiceSubscriptions(response.Routing);
            foreach (var SseServiceSubscription in serviceSubscriptions)
                await SseServiceSubscription.Send_FabricStreamingResponse_ToClientAsync(response, ct);
        }, ct);
    }

    private async Task Receive_StreamingRequest_ClientToServer_FromFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingRequest_ClientToServer_FromFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

            if (StreamingCache.Timeouts.TryGetValue(request.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(request.Routing.RequestId, request.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(request.StreamId, false, ct);
                await Sender.Send_StreamingResponseServerToClient_ToFabricAsync(response, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_ClientToServer_FromFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingResponse_ClientToServer_FromFabricAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

            if (StreamingCache.Timeouts.TryGetValue(response.Routing.RequestId, out var timeout))
                timeout.Reset();

            var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
            if (StreamingCache.StreamingResponseHandlers.TryGetValue(key, out var responseHandler))
            {
                responseHandler.Invoke(response);
            }
        }, ct);
    }

    private async Task Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizeFabricIdsDto synchronizeFabricIds, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeFabricIds);

            FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
            FabricManagerId = synchronizeFabricIds.FabricManagerId;
        }, ct);
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
            if (log.Data == null)
                Logger.Log(log.Level, log.Message);
            else
                Logger.Log(log.Level, log.Message, log.Data.Select(a => a.Value));
        }, ct);
#pragma warning restore CA2254 // Template should be a static expression
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }

    #endregion

    #region Sender


    internal async Task Send_FabricInvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricInvokeRequestDone_ToFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

        await Sender.Send_InvokeRequestDone_ToFabricAsync(done, ct);
    }
    internal async Task Send_FabricSendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricSendRequestDone_ToFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

        await Sender.Send_SendRequestDone_ToFabricAsync(done, ct);
    }

    public async Task Send_StreamingRequestClientToServer_ToFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingRequestClientToServer_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await Sender.Send_StreamingRequestClientToServer_ToFabricAsync(request, ct);
    }
    public async Task Send_StreamingResponseClientToServer_ToFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingResponseClientToServer_ToFabricAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await Sender.Send_StreamingResponseClientToServer_ToFabricAsync(response, ct);
    }


    #endregion

    #region Call's vanuit gegenereerde code

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({routing}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, argumentIndex);

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
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({routing}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, argumentIndex);

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
            Logger.LogTrace("{now} UnRegisterAsyncEnumerableArgument({routing})", DateTime.Now.ToString("HH:mm:ss.fff"), routing);

        if (StreamingCache.StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    private RemoteAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({routing})",
                routing);

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
                        -1,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (streamId, enumerator) =>
            {
                return Sender.Send_StreamingRequestServerToClient_ToFabricAsync(
                    new StreamingRequestDto(
                        routing,
                        -1,
                        streamId,
                        true));

            },

            dispose: streamId =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, -1, streamId);
                StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    public Task SendAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} SendAsync({routing}, {data})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, data);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Handle_SendRequest_ToFabric_Async({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Handle_SendRequest_ToClient_Async({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} InvokeAsync({routing}, {data})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, data);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Handle_InvokeRequest_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        var completion = StreamingCache.PendingFabricInvokeRequests.GetOrAdd(
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
            StreamingCache.PendingFabricInvokeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async IAsyncEnumerable<byte[]> Handle_FabricInvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequest_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

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