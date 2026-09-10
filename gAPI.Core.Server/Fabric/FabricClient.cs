using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Interfaces;
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
public sealed class FabricClient : IAsyncDisposable
{
    public FabricClient(
        SessionCache sessionCache,
        StreamingCache requestCache,
        ServiceSubscriptionCollection serviceSubscriptions,
        ILoggerFactory loggerFactory,
        string? fabricConnectionString)
    {
        Sender = new FabricClientSender(loggerFactory);

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
            Logger.LogError($"Reconnecting FabricClient ....");

        await DisconnectAsync();
        await ConnectAsync();
        foreach (var service in ServiceSubscriptions.Services.Values)
        {
            foreach (var SseServiceSubscription in service.Values)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(
                        "Resubscribe IServiceSubscription {HostId} to {ServiceId} (userId {UserId}, sessionId {SessionId})",
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
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Reconnecting FabricClient DONE");
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
        if (Host == null)
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
        if (Host == null)
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
            using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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
                Logger.LogTrace(
                    "FabricClient {Id.Value} started",
                    FabricConnectionId.Value);

            while (!ct.IsCancellationRequested)
            {
                var messageType = FabricConverter.ReadHostToClientMessageType(BinaryReader);
                switch (messageType)
                {
                    case FabricHostToClientMessageEnum.SynchronizeFabricIds:
                        var synchronizeFabricIds = BinaryReader.ReadSynchronizeFabricIdsDto();
                        await Receive_SynchronizeFabricIds_FromFabricAsync(synchronizeFabricIds, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequest:
                        var sendRequest = BinaryReader.ReadSendRequestDto();
                        await Receive_SendRequest_FromFabricAsync(sendRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var sendRequestDone = BinaryReader.ReadSendRequestDoneDto();
                        await Receive_SendRequestDone_FromFabricAsync(sendRequestDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestCancelled:
                        var sendRequestCancelled = BinaryReader.ReadSendRequestCancelledDto();
                        await Receive_SendRequestCancelled_FromFabricAsync(sendRequestCancelled, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequest:
                        var invokeRequest = BinaryReader.ReadInvokeRequestDto();
                        await Receive_InvokeRequest_FromFabricAsync(invokeRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestDone:
                        var invokeResponseDone = BinaryReader.ReadInvokeRequestDoneDto();
                        await Receive_InvokeRequestDone_FromFabricAsync(invokeResponseDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequestCancelled:
                        var invokeRequestCancelled = BinaryReader.ReadInvokeRequestCancelledDto();
                        await Receive_InvokeRequestCancelled_FromFabricAsync(invokeRequestCancelled, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingRequest:
                        var argumentRequest = BinaryReader.ReadStreamingRequestDto();
                        await Receive_StreamingRequest_FromFabricAsync(argumentRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingResponse:
                        var argumentResponse = BinaryReader.ReadStreamingResponseDto();
                        await Receive_StreamingResponse_FromFabricAsync(argumentResponse, ct);
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

    private async Task Receive_SynchronizeFabricIds_FromFabricAsync(SynchronizeFabricIdsDto synchronizeFabricIds, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SynchronizeFabricIds_FromFabricAsync({synchronizeFabricIds})", synchronizeFabricIds);

        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;
    }

    private async Task Receive_SendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_SendRequest_FromFabricAsync({message})", message);

            try
            {
                await Handle_SendRequest_ToClient_Async(message, ct);
                await Sender.Send_SendRequestDone_ToFabricAsync(
                    new SendRequestDoneDto(
                        message.Routing,
                        false,
                        null
                    ), ct);
            }
            catch (Exception ex)
            {
                await Sender.Send_SendRequestDone_ToFabricAsync(
                    new SendRequestDoneDto(
                        message.Routing,
                        false,
                        ex.Message
                    ), ct);
            }
        }, ct);
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_SendRequestDone_FromFabricAsync({done})", done);

            if (StreamingCache.PendingFabricSendRequests.TryRemove(done.Routing.RequestId, out var completion))
                completion.TrySetResult(done);
        }, ct);
    }
    private async Task Receive_SendRequestCancelled_FromFabricAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_SendRequestCancelled_FromFabricAsync({cancel})", cancel);

        }, ct);
    }
    private async Task Receive_InvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Receive_InvokeRequest_FromFabricAsync({message})",
                message);

        try
        {
            var enumerable = Handle_InvokeRequest_ToClientAsync(message, ct);

            RegisterAsyncEnumerableArgumentByte(message.Routing, -1, enumerable, ct);

            await Sender.Send_InvokeRequestDone_ToFabricAsync(
                new InvokeRequestDoneDto(
                    message.Routing,
                    false,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await Sender.Send_InvokeRequestDone_ToFabricAsync(
                new InvokeRequestDoneDto(
                    message.Routing,
                    false,
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_InvokeRequestDone_FromFabricAsync(InvokeRequestDoneDto invokeResponseDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_InvokeRequestDone_FromFabricAsync({invokeResponseDone})", invokeResponseDone);

            if (StreamingCache.PendingFabricInvokeRequests.TryRemove(invokeResponseDone.Routing.RequestId, out var completion))
                completion.TrySetResult(invokeResponseDone);

            //if (Timeouts.TryRemove(invokeResponseDone.RequestId, out var timeout))
            //    timeout.Dispose();

            //if (PendingInvokeRequests.TryRemove(invokeResponseDone.RequestId, out var channel))
            //    channel.Writer.TryComplete();
        }, ct);
    }
    private async Task Receive_InvokeRequestCancelled_FromFabricAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_InvokeRequestCancelled_FromFabricAsync({cancel})", cancel);

        }, ct);
    }
    private async Task Receive_StreamingRequest_FromFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_StreamingRequest_FromFabricAsync({message})", request);

        if (StreamingCache.Timeouts.TryGetValue(request.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (await Handle_StreamingRequest_FromFabricAsync(request, ct))
        {
            if (TryTakeStreamingResponse(
                request.Routing,
                request.ArgumentIndex,
                request.StreamId,
                out var response))
            {
                // Terug naar FABRIC!!! (dat is er anders)
                await Sender.Send_StreamingResponse_ToFabricAsync(response, ct);
            }
        }
    }
    private async Task Receive_StreamingResponse_FromFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_StreamingResponse_FromFabricAsync({message})", response);

            if (StreamingCache.Timeouts.TryGetValue(response.Routing.RequestId, out var timeout))
                timeout.Reset();

            var SseServiceSubscriptions = GetServiceSubscriptions(response.Routing);
            foreach (var SseServiceSubscription in SseServiceSubscriptions)
                await SseServiceSubscription.Send_StreamingResponse_ToClientAsync(response, ct);
        }, ct);
    }

    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", getSessionResponse);

            // Zoek de wachtende taak op en zet het resultaat zodra het antwoord binnen is
            if (StreamingCache.PendingGetSessionRequests.TryRemove(getSessionResponse.SessionId, out var tcs))
                tcs.TrySetResult(getSessionResponse.CookieData);
        }, ct);
    }

    private async Task Receive_Log_FromFabricAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        if (log.Data == null)
            Logger.Log(log.Level, log.Message);
        else
            Logger.Log(log.Level, log.Message, log.Data.Select(a => a.Value));
    }

    #endregion

    #region Sender

    //public async Task Send_InvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto invokeResponseDone, CancellationToken ct)
    //{
    //    await Sender.Send_InvokeRequestDone_ToFabricAsync(invokeResponseDone, ct);
    //}
    public async Task Send_StreamingRequest_ToFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        await Sender.Send_StreamingRequest_ToFabricAsync(request, ct);
    }
    public async Task Send_StreamingResponse_ToFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        await Sender.Send_StreamingResponse_ToFabricAsync(response, ct);
    }

    #endregion

    // uuh?
    public async Task<bool> Handle_StreamingRequest_FromFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (StreamingCache.Timeouts.TryGetValue(request.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingCache.StreamingRequestHandlers.TryGetValue((request.Routing.RequestId, request.ArgumentIndex), out var handler))
        {
            await handler.Invoke(request.StreamId, ct);
            return true;
        }

        return false;
    }

    public bool TryTakeStreamingResponse(RoutingDto routing, int argumentIndex, StreamId streamId, out StreamingResponseDto response)
        => StreamingCache.PendingStreamingResponses.TryRemove((routing.RequestId, argumentIndex, streamId), out response!);

    #region Call's vanuit gegenereerde code

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<StreamId, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingCache.StreamingRequestHandlers[(routing.RequestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return (source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
                });

            await gate.WaitAsync(ct);

            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                var response = new StreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    hasNext ? serializer(enumerator.Current) : []);
                //if (Host != null)
                //    await Sender.Send_StreamingResponse_ToFabricAsync(response, ct);
                //else
                StreamingCache.PendingStreamingResponses[(response.Routing.RequestId, response.ArgumentIndex, streamId)] = response;

                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
                    await enumerator.DisposeAsync();
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    throw;

                var response = new StreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    []);
                //if (Host != null)
                //    await Sender.Send_StreamingResponse_ToFabricAsync(response, CancellationToken.None);
                //else
                StreamingCache.PendingStreamingResponses[(response.Routing.RequestId, response.ArgumentIndex, streamId)] = response;

                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<StreamId, (IAsyncEnumerator<byte[]> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingCache.StreamingRequestHandlers[(routing.RequestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return (source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
                });

            await gate.WaitAsync(ct);

            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                var response = new StreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    hasNext ? enumerator.Current : []);
                //if (Host == null || IsConnected == false)
                StreamingCache.PendingStreamingResponses[(response.Routing.RequestId, response.ArgumentIndex, streamId)] = response;
                //else
                //    await Sender.Send_StreamingResponse_ToFabricAsync(response, ct);

                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
                    await enumerator.DisposeAsync();
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    throw;

                var response = new StreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    []);
                //if (Host != null)
                //    await Sender.Send_StreamingResponse_ToFabricAsync(response, CancellationToken.None);
                //else
                StreamingCache.PendingStreamingResponses[(response.Routing.RequestId, response.ArgumentIndex, streamId)] = response;

                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public void UnRegisterAsyncEnumerableArguments(RoutingDto routing)
    {

    }

    private IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        return new RemoteAsyncEnumerable<byte[]>((streamId, push, complete, ct) =>
        {
            var key = (routing.RequestId, argumentIndex, streamId);
            if (!StreamingCache.StreamingResponseHandlers.ContainsKey(key))
            {
                StreamingCache.StreamingResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
                        complete.Invoke(null);
                    }
                    else
                    {
                        push.Invoke(response.BinaryData);
                    }
                };
            }
            return Sender.Send_StreamingRequest_ToFabricAsync(
                new StreamingRequestDto(
                    routing,
                    argumentIndex,
                    streamId
                ), ct);
        });
    }
    private void UnRegisterRemoteAsyncEnumerableArguments(RoutingDto routing)
    {

    }

    public Task SendAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SendAsync({routing}, {data})",
                routing, data);

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
            Logger.LogTrace(
                "Handle_SendRequest_ToFabric_Async({request})",
                request);

        var completion = StreamingCache.PendingFabricSendRequests.GetOrAdd(
            request.Routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_SendRequest_ToFabricAsync(request, ct);

        try
        {
            var done = await completion.Task.WaitAsync(//TimeSpan.FromSeconds(60), ct);
                ct);
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
            Logger.LogTrace(
                "Handle_SendRequest_ToClient_Async({message})",
                request);

        var sessions = GetServiceSubscriptions(request.Routing).GroupBy(a => a.SessionId).Select(a => a.First());

        foreach (var serviceSubscription in sessions)
        {
            try
            {
                await serviceSubscription.Send_SendRequest_ToClient_Async(request, ct); // deze is blocking vanuit WssServerConnection
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    public IAsyncEnumerable<byte[]> InvokeAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "InvokeAsync({routing}, {data})",
                routing,
                data);

        var request = new InvokeRequestDto(
            routing,
            data);
        if (Host == null || IsConnected == false)
        {
            return Handle_InvokeRequest_ToClientAsync(request, ct);
        }
        return Handle_InvokeRequest_ToFabricAsync(request, ct);
    }
    private async IAsyncEnumerable<byte[]> Handle_InvokeRequest_ToFabricAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Handle_InvokeRequest_ToFabricAsync({request})",
                request);

        var completion = StreamingCache.PendingFabricInvokeRequests.GetOrAdd(
            request.Routing.RequestId,
            _ => new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);

            try
            {
                var enumerable = RegisterRemoteAsyncEnumerableArgumentByte(request.Routing, -1);
                await foreach (var item in enumerable)
                {
                    yield return item;
                }
            }
            finally
            {
                UnRegisterRemoteAsyncEnumerableArguments(request.Routing);
            }
        }
        finally
        {
            StreamingCache.PendingFabricInvokeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async IAsyncEnumerable<byte[]> Handle_InvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({request})",
                request);

        var SseServiceSubscriptions = GetServiceSubscriptions(request.Routing);
        foreach (var SseServiceSubscription in SseServiceSubscriptions)
        {
            var responses = SseServiceSubscription.Send_InvokeRequest_ToClient_Async(request, ct);
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
                (routing.UserId != null && SseServiceSubscription.UserId == routing.UserId));
    }
    //private IEnumerable<IServiceSubscription> GetServiceSubscriptions(RequestId requestId)
    //{
    //    return ServiceSubscriptions.Values.SelectMany(a => a.Values)
    //        .Where(a => a.HasRequest(requestId));
    //}

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
        await SenderCts.CancelAsync();
        SenderCts.Dispose();
    }

}