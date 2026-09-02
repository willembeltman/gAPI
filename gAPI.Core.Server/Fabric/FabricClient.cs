using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Interfaces;
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
    // TODO; Cleanup van de handlers
    private readonly ILogger Logger;
    private readonly string? Host;
    private readonly int? Port;
    private TcpClient? Tcp;
    private NetworkStream? Stream;
    private bool FirstTime;
    private bool IsConnecting;
    private bool IsDisconnecting;
    private SessionCache LocalSessionCache;

    private readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();


    public FabricClient(SessionCache sessionCache, ILoggerFactory loggerFactory, string? fabricConnectionString)
    {
        LocalSessionCache = sessionCache;
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

    private readonly ConcurrentDictionary<RequestId, ResettableTimeout> Timeouts = [];
    private readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>> ServiceSubscriptions = [];
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    private readonly ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingInvokeRequests = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<Guid, CancellationToken, Task>> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, Guid StreamId), StreamingResponseDto> PendingStreamingResponses = [];
    private readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];

    private readonly CancellationTokenSource SenderCts = new();
    public BinaryWriter? BinaryWriter { get; private set; }

    private CancellationTokenSource? ReceiverCts;
    public BinaryReader? BinaryReader { get; private set; }

    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");

    public bool IsConnected => IsDisconnecting || IsConnecting || Tcp?.Connected == true;

    public async Task ConnectAsync()
    {
        if (Host == null || Port == null) return;
        if (IsConnected || IsDisconnecting) return;

        try
        {
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation($"Starting FabricClient");

            IsConnecting = true;

            ReceiverCts = new CancellationTokenSource();
            Tcp = new TcpClient();
            Tcp.Connect(Host, Port.Value);
            Stream = Tcp.GetStream();
            BinaryReader = new BinaryReader(Stream);
            BinaryWriter = new BinaryWriter(Stream);

            if (!FirstTime)
            {
                FirstTime = true;
                _ = Task.Run(async () => { await SendKernel(SenderCts.Token); });
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
        foreach (var service in ServiceSubscriptions.Values)
        {
            foreach (var SseServiceSubscription in service.Values)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(
                        "Resubscribe IServiceSubscription {HostId} to {ServiceId} (userId {UserId}, sessionId {SessionId})",
                        SseServiceSubscription.Id,
                        SseServiceSubscription.ServiceId,
                        SseServiceSubscription.UserId,
                        SseServiceSubscription.SessionId);

                await Send_Subscribe_ToFabricAsync(SseServiceSubscription, ct);
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

    public async Task UpdateSession(SessionId sessionId, string? cookieData, CancellationToken ct)
    {
        // Todo, niet deze call doen als het om AutoApi gaat

        if (Host == null)
        {
            LocalSessionCache.AddOrUpdate(sessionId, cookieData);
            return;
        }

        var updateSessionDto = new UpdateSessionDto(sessionId, cookieData);
        await Send_UpdateSession_ToFabricAsync(updateSessionDto, ct);
    }
    public async Task ClearSession(SessionId sessionId, CancellationToken ct)
    {
        if (Host == null)
        {
            LocalSessionCache.Remove(sessionId);
            return;
        }

        var clearSessionDto = new SendClearSessionDto(sessionId);
        await Send_ClearSession_ToFabricAsync(clearSessionDto, ct);
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
        PendingGetSessionRequests[sessionId] = tcs;

        try
        {
            var getSessionDto = new SendGetSessionCookieDataDto(sessionId);
            await Send_GetSession_ToFabricAsync(getSessionDto, ct);

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
            PendingGetSessionRequests.TryRemove(sessionId, out _);
        }
    }

    public async Task SubscribeAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SubscribeAsync({SseServiceSubscription})",
                SseServiceSubscription);

        var SseServiceSubscriptionsForService = ServiceSubscriptions.AddOrUpdate(
            SseServiceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(),
            (a, b) => b);
        SseServiceSubscriptionsForService[SseServiceSubscription.Id] = SseServiceSubscription;

        if (Host == null)
        {
            return;
        }

        await Send_Subscribe_ToFabricAsync(SseServiceSubscription, ct);
    }
    public async Task UnsubscribeAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({SseServiceSubscription})",
                SseServiceSubscription);

        ServiceSubscriptions[SseServiceSubscription.ServiceId].TryRemove(SseServiceSubscription.Id, out _);

        if (Host == null)
        {
            return;
        }

        await Send_Unsubscribe_ToFabricAsync(SseServiceSubscription, ct);
    }

    public async Task<SendRequestDoneDto> SendAsync(
        RequestId requestId,
        ServiceId serviceId,
        ServiceMethodId methodId,
        UserId? userId,
        SessionId? sessionId,
        bool stateIsChanged,
        string? stateData,
        byte[] data,
        CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({requestId}, {serviceId}, {methodId}, {userId}, {sessionId}, {data})",
                requestId, serviceId, methodId, userId, sessionId, data);

        var request = new SendRequestDto(
            requestId,
            serviceId,
            methodId,
            userId,
            sessionId,
            stateIsChanged,
            stateData,
            data);

        if (Host == null)
        {
            return await Handle_SendRequest_ToClient_Async(request, ct);
        }

        return await Handle_SendRequest_ToFabric_Async(request, ct);
    }
    private async Task<SendRequestDoneDto> Handle_SendRequest_ToFabric_Async(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_SendRequest_ToFabric_Async({request})",
                request);

        var completion = PendingSendRequests.GetOrAdd(
            request.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Send_SendRequest_ToFabricAsync(request, ct);

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
        }
        finally
        {
            PendingSendRequests.TryRemove(request.RequestId, out _);
        }
    }
    public async Task<SendRequestDoneDto> Handle_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_SendRequest_ToClient_Async({message})",
                message);

        var serviceSubscriptions = GetServiceSubscriptions(message.ServiceId, message.UserId, message.SessionId);

        // Todo exceptions verzamelen en die dan throwen
        var exceptionMessage = string.Empty;
        var stateIsChanged = false;
        var stateData = (string?)null;

        foreach (var serviceSubscription in serviceSubscriptions)
        {
            try
            {
                var done = await serviceSubscription.Send_SendRequest_ToClient_Async(message, ct); // deze is blocking vanuit WssServerConnection
                if (done.StateIsChanged)
                {
                    stateIsChanged = true;
                    stateData = done.StateData;
                }
                if (done.ExceptionMessage != null)
                {
                    exceptionMessage += done.ExceptionMessage;
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        return new SendRequestDoneDto(
            message.RequestId,
            message.ServiceId,
            message.MethodId,
            message.UserId,
            message.SessionId,
            stateIsChanged,
            stateData,
            exceptionMessage);
    }

    public IAsyncEnumerable<InvokeResponseDto> InvokeAsync(
        RequestId requestId,
        ServiceId serviceId,
        ServiceMethodId methodId,
        UserId? userId,
        SessionId? sessionId,
        bool stateIsChanged,
        string? stateData,
        byte[] data,
        CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "InvokeAsync({serviceId}, {serviceMethodId}, {userId} {sessionId})",
                serviceId,
                methodId,
                userId,
                sessionId);

        var request = new InvokeRequestDto(
            requestId,
            serviceId,
            methodId,
            userId,
            sessionId,
            stateIsChanged,
            stateData,
            data);
        if (Host == null)
        {
            return Handle_InvokeRequest_ToClientAsync(request, ct);
        }
        return Handle_InvokeRequest_ToFabricAsync(request, ct);
    }
    private async IAsyncEnumerable<InvokeResponseDto> Handle_InvokeRequest_ToFabricAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToFabricAsync({request})",
                request);

        var channel = Channel.CreateUnbounded<InvokeResponseDto>();
        PendingInvokeRequests[request.RequestId] = channel;

        using var timeout = new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
        {
            if (PendingInvokeRequests.TryRemove(request.RequestId, out var pending))
                pending.Writer.TryComplete(new TimeoutException("Fabric invoke request timed out."));
            Timeouts.TryRemove(request.RequestId, out _);
        });
        Timeouts[request.RequestId] = timeout;

        await Send_InvokeRequest_ToFabricAsync(request, ct);

        try
        {
            await foreach (var response in channel.Reader.ReadAllAsync(ct))
            {
                timeout.Reset();
                yield return response;
            }
        }
        finally
        {
            Timeouts.TryRemove(request.RequestId, out _);
            if (PendingInvokeRequests.TryRemove(request.RequestId, out var pending))
                pending.Writer.TryComplete();
        }
    }
    public async IAsyncEnumerable<InvokeResponseDto> Handle_InvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({request})",
                request);

        var SseServiceSubscriptions = GetServiceSubscriptions(request.ServiceId, request.UserId, request.SessionId);
        //if (ServiceSubscriptions.TryGetValue(request.ServiceId, out var hubHosts) == false)
        //    yield break;

        //var SseServiceSubscriptions = hubHosts.Values
        //    .Where(SseServiceSubscription =>
        //        // Mogelijkheid 1: Naar iedereen: Beide null
        //        (request.SessionId == null && request.UserId == null) ||
        //        // Mogelijkheid 2: Naar session: Session not null
        //        (request.SessionId != null && SseServiceSubscription.SessionId == request.SessionId) ||
        //        // Mogelijkheid 3: Naar user: User not null
        //        (request.UserId != null && SseServiceSubscription.UserId == request.UserId));

        foreach (var SseServiceSubscription in SseServiceSubscriptions)
        {
            var responses = SseServiceSubscription.Send_InvokeRequest_ToClient_Async(request, ct);
            await foreach (var response in responses)
            {
                yield return response;
            }
        }
    }

    public async Task<bool> Handle_StreamingRequest_FromFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Timeouts.TryGetValue(request.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingRequestHandlers.TryGetValue((request.RequestId, request.ArgumentIndex), out var handler))
        {
            await handler(request.StreamId, ct);
            return true;
        }

        return false;
    }
    public void RegisterAsyncEnumerableArgument<T>(RequestId requestId, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<Guid, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingRequestHandlers[(requestId, argumentIndex)] = async (streamId, ct) =>
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
                    requestId,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    hasNext ? serializer(enumerator.Current) : []);
                if (Host != null)
                    await Send_StreamingResponse_ToFabricAsync(response, ct);
                else
                    PendingStreamingResponses[(response.RequestId, response.ArgumentIndex, streamId)] = response;

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
                    requestId,
                    argumentIndex,
                    streamId,
                    true,
                    []);
                if (Host != null)
                    await Send_StreamingResponse_ToFabricAsync(response, CancellationToken.None);
                else
                    PendingStreamingResponses[(response.RequestId, response.ArgumentIndex, streamId)] = response;

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
    public bool TryTakeStreamingResponse(RequestId requestId, int argumentIndex, Guid streamId, out StreamingResponseDto response)
        => PendingStreamingResponses.TryRemove((requestId, argumentIndex, streamId), out response!);

    private IEnumerable<IServiceSubscription> GetServiceSubscriptions(ServiceId serviceId, UserId? userId, SessionId? sessionId)
    {
        if (ServiceSubscriptions.TryGetValue(serviceId, out var serviceSubscriptions) == false)
            return [];

        return serviceSubscriptions.Values
            .Where(SseServiceSubscription =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (sessionId == null && userId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (sessionId != null && SseServiceSubscription.SessionId == sessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (userId != null && SseServiceSubscription.UserId == userId));
    }
    private IEnumerable<IServiceSubscription> GetServiceSubscriptions(RequestId requestId)
    {
        return ServiceSubscriptions.Values.SelectMany(a => a.Values)
            .Where(a => a.HasRequest(requestId));
    }

    #region Receiver

    public async Task ReceiveKernel(CancellationToken ct)
    {
        if (BinaryReader == null) return;
        try
        {
            var ids = BinaryReader.ReadSynchronizeFabricIdsDto();

            FabricConnectionId = ids.FabricConnectionId;
            FabricManagerId = ids.FabricManagerId;

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
                        _ = Task.Run(async () => { await Receive_SendRequest_FromFabricAsync(sendRequest, ct); }, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = BinaryReader.ReadSendRequestDoneDto();
                        await Receive_SendRequestDone_FromFabricAsync(sendArgumentedRequestDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingRequest:
                        var argumentRequest = BinaryReader.ReadStreamingRequestDto();
                        await Receive_StreamingRequest_FromFabricAsync(argumentRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.StreamingResponse:
                        var argumentResponse = BinaryReader.ReadStreamingResponseDto();
                        await Receive_StreamingResponse_FromFabricAsync(argumentResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequest:
                        var invokeRequest = BinaryReader.ReadInvokeRequestDto();
                        _ = Task.Run(async () => { await Receive_InvokeRequest_FromFabricAsync(invokeRequest, ct); }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponse:
                        var invokeResponse = BinaryReader.ReadInvokeResponseDto();
                        await Receive_InvokeResponse_FromFabricAsync(invokeResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponseDone:
                        var invokeResponseDone = BinaryReader.ReadInvokeResponseDoneDto();
                        await Receive_InvokeResponseDone_FromFabricAsync(invokeResponseDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = BinaryReader.ReadSendGetSessionCookieDataResponseDto();
                        await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        break;
                        //case FabricHostToClientMessageEnum.Log:
                        //    var log = BinaryReader.ReadWssLoggerLogDto();
                        //    //_ = Task.Run(async () => { await Receive_Log_FromFabricAsync(log, ct); }, ct);
                        //    await Receive_Log_FromFabricAsync(log, ct);
                        //    break;
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
        FabricConnectionId = synchronizeFabricIds.FabricConnectionId;
        FabricManagerId = synchronizeFabricIds.FabricManagerId;
    }

    private async Task Receive_SendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequest_FromFabricAsync({message})", message);

        try
        {
            await Handle_SendRequest_ToClient_Async(message, ct);
            await Send_SendRequestDone_ToFabricAsync(
                new SendRequestDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    message.StateIsChanged,
                    message.StateData,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await Send_SendRequestDone_ToFabricAsync(
                new SendRequestDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    message.StateIsChanged,
                    message.StateData,
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequestDone_FromFabricAsync({done})", done);

        if (PendingSendRequests.TryRemove(done.RequestId, out var completion))
            completion.TrySetResult(done);
    }
    private async Task Receive_StreamingRequest_FromFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_StreamingRequest_FromFabricAsync({message})", message);

        await Handle_StreamingRequest_FromFabricAsync(message, ct);
    }
    private async Task Receive_StreamingResponse_FromFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_StreamingResponse_FromFabricAsync({message})", message);

        if (Timeouts.TryGetValue(message.RequestId, out var timeout))
            timeout.Reset();

        var SseServiceSubscriptions = GetServiceSubscriptions(message.RequestId);
        foreach (var SseServiceSubscription in SseServiceSubscriptions)
            await SseServiceSubscription.SendStreamingResponseAsync(message, ct);

    }
    private async Task Receive_InvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Receive_InvokeRequest_FromFabricAsync({message})",
                message);

        try
        {
            var list = Handle_InvokeRequest_ToClientAsync(message, ct);

            await foreach (var item in list)
                await Send_InvokeResponse_ToFabricAsync(item, ct);

            // Send done for this host
            await Send_InvokeResponseDone_ToFabricAsync(
                new InvokeResponseDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await Send_InvokeResponseDone_ToFabricAsync(
                new InvokeResponseDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_InvokeResponse_FromFabricAsync(InvokeResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponse_FromFabricAsync({response})", response);

        if (Timeouts.TryGetValue(response.RequestId, out var timeout))
            timeout.Reset();

        if (PendingInvokeRequests.TryGetValue(response.RequestId, out var channel))
            channel.Writer.TryWrite(response);
    }
    private async Task Receive_InvokeResponseDone_FromFabricAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponseDone_FromFabricAsync({invokeResponseDone})", invokeResponseDone);


        if (Timeouts.TryRemove(invokeResponseDone.RequestId, out var timeout))
            timeout.Dispose();

        if (PendingInvokeRequests.TryRemove(invokeResponseDone.RequestId, out var channel))
            channel.Writer.TryComplete();
    }

    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", getSessionResponse);

        // Zoek de wachtende taak op en zet het resultaat zodra het antwoord binnen is
        if (PendingGetSessionRequests.TryRemove(getSessionResponse.SessionId, out var tcs))
            tcs.TrySetResult(getSessionResponse.CookieData);
    }

    #endregion

    #region Sender

    public async Task SendKernel(CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            while (BinaryWriter == null)
            {
                await Task.Delay(10, ct);
            }
            item(BinaryWriter);
            BinaryWriter.Flush();
        }
    }

    public async Task Send_UpdateSession_ToFabricAsync(UpdateSessionDto updateSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_UpdateSession_ToFabricAsync({updateSessionDto})", updateSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.UpdateSession);
            writer.Write(updateSessionDto);
        }, ct);
    }
    public async Task Send_ClearSession_ToFabricAsync(SendClearSessionDto clearSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_ClearSession_ToFabricAsync({clearSessionDto})", clearSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(clearSessionDto);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(SendGetSessionCookieDataDto getSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_GetSession_ToFabricAsync({getSessionDto})", getSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Subscribe_ToFabricAsync({SseServiceSubscription})", SseServiceSubscription);
        var request = new SubscribeDto()
        {
            ServiceId = SseServiceSubscription.ServiceId,
            UserId = SseServiceSubscription.UserId,
            SessionId = SseServiceSubscription.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(request);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Unsubscribe_ToFabricAsync({SseServiceSubscription})", SseServiceSubscription);
        var request = new UnsubscribeDto()
        {
            ServiceId = SseServiceSubscription.ServiceId,
            UserId = SseServiceSubscription.UserId,
            SessionId = SseServiceSubscription.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(request);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequest_ToFabricAsync({request})", request);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequestDone_ToFabricAsync({request})", done);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(done);
        }, ct);
    }
    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequest_ToFabricAsync({request})", request);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToFabricAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequestCancelled_ToFabricAsync({cancel})", cancel);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(cancel);
        }, ct);
    }
    public async Task Send_StreamingRequest_ToFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_StreamingResponse_ToFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponse);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_InvokeResponse_ToFabricAsync(InvokeResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponse_ToFabricAsync({response})", response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponse);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_InvokeResponseDone_ToFabricAsync(InvokeResponseDoneDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponseDone_ToFabricAsync({requestId})", response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponseDone);
            writer.Write(response);
        }, ct);
    }

    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
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
        await SenderCts.CancelAsync();
        SenderCts.Dispose();
    }
}