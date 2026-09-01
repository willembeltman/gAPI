using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
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
    private readonly ILogger Logger;
    private readonly string? Host;
    private readonly int? Port;
    private TcpClient? Tcp;
    private NetworkStream? Stream;
    private bool FirstTime;
    private bool IsConnecting;
    private bool IsDisconnecting;
    private SessionCache LocalSessionCache;

    public FabricClient(SessionCache sessionCache, ILoggerFactory loggerFactory, string? fabricConnectionString) //: this(sessionCache, loggerFactory)
    {
        LocalSessionCache = sessionCache;
        Logger = loggerFactory.CreateLogger<FabricClient>();
        Sender = new FabricClientSender(this, loggerFactory);
        Receiver = new FabricClientReceiver(this, loggerFactory);

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
    //private FabricClient(SessionCache sessionCache, ILoggerFactory loggerFactory)
    //{
    //    LocalSessionCache = sessionCache;
    //    Logger = loggerFactory.CreateLogger<FabricClient>();
    //    Sender = new FabricClientSender(this, loggerFactory);
    //    Receiver = new FabricClientReceiver(this, loggerFactory);
    //}
    //public FabricClient(string host, int port, SessionCache sessionCache, ILoggerFactory loggerFactory) : this(sessionCache, loggerFactory)
    //{
    //    Host = host;
    //    Port = port;
    //}

    public Channel<Action<BinaryWriter>> SendQueue { get; } = Channel.CreateUnbounded<Action<BinaryWriter>>();
    public ConcurrentDictionary<ServiceId, ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>> Services { get; } = [];
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    private readonly ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingInvokeRequests = [];
    private readonly ConcurrentDictionary<RequestId, ResettableTimeout> InvokeTimeouts = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<Guid, CancellationToken, Task>> ArgumentRequestHandlers = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, Guid StreamId), InvokeArgumentResponseDto> PendingArgumentResponses = [];
    private readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];

    private readonly CancellationTokenSource SenderCts = new();
    public FabricClientSender Sender { get; private set; }
    public BinaryWriter? BinaryWriter { get; private set; }

    private CancellationTokenSource? ReceiverCts;
    public FabricClientReceiver Receiver { get; private set; }
    public BinaryReader? BinaryReader { get; private set; }

    public FabricHostId? Id { get; set; }
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
                _ = Task.Run(async () => { await Sender.SendKernel(SenderCts.Token); });
            }

            _ = Task.Run(async () => { await Receiver.ReceiveKernel(SenderCts.Token); });
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
        foreach (var service in Services.Values)
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

                await Sender.Send_Subscribe_ToFabricAsync(SseServiceSubscription, ct);
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
                Id);

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
        PendingGetSessionRequests[sessionId] = tcs;

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
            PendingGetSessionRequests.TryRemove(sessionId, out _);
        }
    }

    public async Task SubscribeAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SubscribeAsync({SseServiceSubscription})",
                SseServiceSubscription);

        var SseServiceSubscriptionsForService = Services.AddOrUpdate(
            SseServiceSubscription.ServiceId,
            new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(),
            (a, b) => b);
        SseServiceSubscriptionsForService[SseServiceSubscription.Id] = SseServiceSubscription;

        if (Host == null)
        {
            return;
        }

        await Sender.Send_Subscribe_ToFabricAsync(SseServiceSubscription, ct);
    }
    public async Task UnsubscribeAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({SseServiceSubscription})",
                SseServiceSubscription);

        Services[SseServiceSubscription.ServiceId].TryRemove(SseServiceSubscription.Id, out _);

        if (Host == null)
        {
            return;
        }

        await Sender.Send_Unsubscribe_ToFabricAsync(SseServiceSubscription, ct);
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
            return await Send_SendRequest_ToClient_Async(request, ct);
        }

        return await Send_SendRequest_ToFabric_Async(request, ct);
    }
    private async Task<SendRequestDoneDto> Send_SendRequest_ToFabric_Async(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_SendRequest_ToFabric_Async({request})",
                request);

        var completion = PendingSendRequests.GetOrAdd(
            request.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));
        await Sender.Send_SendRequest_ToFabricAsync(request, ct);

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
        }
        finally
        {
            PendingSendRequests.TryRemove(request.RequestId, out _);
        }
    }
    public async Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct)
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
            return Send_InvokeRequest_ToClientAsync(request, ct);
        }
        return Send_InvokeRequest_ToFabricAsync(request, ct);
    }
    private async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToFabricAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
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
            InvokeTimeouts.TryRemove(request.RequestId, out _);
        });
        InvokeTimeouts[request.RequestId] = timeout;

        await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

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
            InvokeTimeouts.TryRemove(request.RequestId, out _);
            if (PendingInvokeRequests.TryRemove(request.RequestId, out var pending))
                pending.Writer.TryComplete();
        }
    }
    public async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({request})",
                request);

        if (Services.TryGetValue(request.ServiceId, out var hubHosts) == false)
            yield break;

        var SseServiceSubscriptions = hubHosts.Values
            .Where(SseServiceSubscription =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (request.SessionId == null && request.UserId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (request.SessionId != null && SseServiceSubscription.SessionId == request.SessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (request.UserId != null && SseServiceSubscription.UserId == request.UserId));

        foreach (var SseServiceSubscription in SseServiceSubscriptions)
        {
            var responses = SseServiceSubscription.Send_InvokeRequest_ToClient_Async(request, ct);
            await foreach (var response in responses)
            {
                yield return response;
            }
        }
    }
    
    public async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto done)
    {
        if (PendingSendRequests.TryRemove(done.RequestId, out var completion))
            completion.TrySetResult(done);
    }
    public async Task Receive_InvokeResponse_FromFabricAsync(InvokeResponseDto response)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Receive_InvokeResponse_FromFabricAsync({response})",
                response);
        }

        if (InvokeTimeouts.TryGetValue(response.RequestId, out var timeout))
            timeout.Reset();

        if (PendingInvokeRequests.TryGetValue(response.RequestId, out var channel))
            channel.Writer.TryWrite(response);
    }
    public async Task Receive_InvokeResponseDone_FromFabricAsync(InvokeResponseDoneDto done)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Receive_InvokeResponseDone_FromFabricAsync({requestId})",
                done.RequestId);
        }

        if (InvokeTimeouts.TryRemove(done.RequestId, out var timeout))
            timeout.Dispose();

        if (PendingInvokeRequests.TryRemove(done.RequestId, out var channel))
            channel.Writer.TryComplete();
    }
    public async Task Receive_InvokeArgumentResponse_FromFabricAsync(InvokeArgumentResponseDto message, CancellationToken ct)
    {
        if (InvokeTimeouts.TryGetValue(message.RequestId, out var timeout))
            timeout.Reset();

        var SseServiceSubscriptions = GetHosts(message.RequestId);
        foreach (var SseServiceSubscription in SseServiceSubscriptions)
            await SseServiceSubscription.SendArgumentResponseAsync(message, ct);
    }
    public async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse)
    {
        // Zoek de wachtende taak op en zet het resultaat zodra het antwoord binnen is
        if (PendingGetSessionRequests.TryRemove(getSessionResponse.SessionId, out var tcs))
            tcs.TrySetResult(getSessionResponse.CookieData);
    }

    public void RegisterAsyncEnumerableArgument<T>(RequestId requestId, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<Guid, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate)>();
        ArgumentRequestHandlers[(requestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate) = activeStreams.GetOrAdd(streamId, _ => (source.GetAsyncEnumerator(cancellationToken), new SemaphoreSlim(1, 1)));
            await gate.WaitAsync(ct);
            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                var response = new InvokeArgumentResponseDto(
                    requestId,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    hasNext ? serializer(enumerator.Current) : []);
                if (Host != null)
                    await Sender.Send_InvokeArgumentResponse_ToFabricAsync(response, ct);
                else
                    PendingArgumentResponses[(response.RequestId, response.ArgumentIndex, streamId)] = response;

                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
                    await enumerator.DisposeAsync();
                }
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public async Task<bool> Receive_InvokeArgumentRequest_FromFabricAsync(InvokeArgumentRequestDto request, CancellationToken ct)
    {
        if (InvokeTimeouts.TryGetValue(request.RequestId, out var timeout))
            timeout.Reset();

        if (ArgumentRequestHandlers.TryGetValue((request.RequestId, request.ArgumentIndex), out var handler))
        {
            await handler(request.StreamId, ct);
            return true;
        }

        return false;
    }
    public bool TryTakeInvokeArgumentResponse(RequestId requestId, int argumentIndex, Guid streamId, out InvokeArgumentResponseDto response)
        => PendingArgumentResponses.TryRemove((requestId, argumentIndex, streamId), out response!);


    private IEnumerable<IServiceSubscription> GetServiceSubscriptions(ServiceId serviceId, UserId? userId, SessionId? sessionId)
    {
        if (Services.TryGetValue(serviceId, out var hubHosts) == false)
            return [];

        return hubHosts.Values
            .Where(SseServiceSubscription =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (sessionId == null && userId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (sessionId != null && SseServiceSubscription.SessionId == sessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (userId != null && SseServiceSubscription.UserId == userId));
    }
    private IEnumerable<IServiceSubscription> GetHosts(RequestId requestId)
    {
        return Services.Values.SelectMany(a => a.Values)
            .Where(a => a.HasRequest(requestId));
    }


    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Closing FabricClient {Id}",
                Id);
        }
        await DisconnectAsync();
        await SenderCts.CancelAsync();
        SenderCts.Dispose();
    }

}
