using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

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

    public FabricClient(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<FabricClient>();
        Sender = new FabricClientSender(this, loggerFactory);
        Receiver = new FabricClientReceiver(this, loggerFactory);
    }
    public FabricClient(ILoggerFactory loggerFactory, string? fabricConnectionString) : this(loggerFactory)
    {
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
    public FabricClient(string host, int port, ILoggerFactory loggerFactory) : this(loggerFactory)
    {
        Host = host;
        Port = port;
    }

    public Channel<Action<BinaryWriter>> SendQueue { get; } = Channel.CreateUnbounded<Action<BinaryWriter>>();
    public ConcurrentDictionary<ServiceId, ConcurrentDictionary<SseHostId, ISseHost>> Services { get; } = new();
    public ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingRequests { get; } = new();

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
            foreach (var sseHost in service.Values)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning(
                        "Resubscribe ISseHost {HostId} to {ServiceId} (userId {UserId}, sessionId {SessionId})",
                        sseHost.Id,
                        sseHost.ServiceId,
                        sseHost.UserId,
                        sseHost.SessionId);

                await Sender.Send_Subscribe_ToFabricAsync(sseHost, ct);
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

    public async Task SubscribeAsync(ISseHost sseHost, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SubscribeAsync({sseHost})",
                sseHost);

        var sseHostsForService = Services.AddOrUpdate(
            sseHost.ServiceId,
            new ConcurrentDictionary<SseHostId, ISseHost>(),
            (a, b) => b);
        sseHostsForService[sseHost.Id] = sseHost;

        if (Host == null)
        {
            return;
        }

        await Sender.Send_Subscribe_ToFabricAsync(sseHost, ct);
    }
    public async Task UnsubscribeAsync(ISseHost sseHost, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "UnsubscribeAsync({sseHost})",
                sseHost);

        Services[sseHost.ServiceId].TryRemove(sseHost.Id, out _);

        if (Host == null)
        {
            return;
        }

        await Sender.Send_Unsubscribe_ToFabricAsync(sseHost, ct);
    }

    public async Task SendAsync(ServiceId serviceId, ServiceMethodId methodId, UserId? userId, SessionId? sessionId, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "SendAsync({serviceId}, {methodId} {userId}, {sessionId}, {data})",
                serviceId,
                methodId,
                userId,
                sessionId,
                data);

        var sendRequest = new SendRequestDto()
        {
            ServiceId = serviceId,
            MethodId = methodId,
            UserId = userId,
            SessionId = sessionId,
            BinaryData = data
        };

        if (Host == null)
        {
            await Receiver.Receive_SendRequest_FromFabricAsync(sendRequest, ct);
            return;
        }

        await Sender.Send_SendRequest_ToFabricAsync(sendRequest, ct);
    }

    public IAsyncEnumerable<InvokeResponseDto> InvokeAsync(ServiceId serviceId, ServiceMethodId serviceMethodId, UserId? userId, SessionId? sessionId, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "InvokeAsync({serviceId}, {serviceMethodId}, {userId} {sessionId})",
                serviceId,
                serviceMethodId,
                userId,
                sessionId);
        }

        var requestId = RequestId.New();
        var request = new InvokeRequestDto()
        {
            RequestId = requestId,
            ServiceId = serviceId,
            MethodId = serviceMethodId,
            SessionId = sessionId,
            UserId = userId,
            BinaryData = data
        };
        if (Host == null)
        {
            return Send_InvokeRequest_ToClientAsync(request, ct);
        }
        return Send_InvokeRequest_ToFabricAsync(request, ct);
    }

    private async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToFabricAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Send_InvokeRequest_ToFabricAsync({request})",
                request);
        }

        var channel = Channel.CreateUnbounded<InvokeResponseDto>();
        PendingRequests[request.RequestId] = channel;

        using var activityCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timeout = TimeSpan.FromSeconds(10);
        var activity = new SemaphoreSlim(0, 1);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!activityCts.IsCancellationRequested)
                {
                    if (!await activity.WaitAsync(timeout))
                        throw new TimeoutException("Client did not ACK");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Send_InvokeRequest_ToFabricAsync => Exception: {ex}", ex);
                if (PendingRequests.TryRemove(request.RequestId, out var pending))
                    pending.Writer.TryComplete(ex);
            }
        }, ct);

        await Sender.Send_InvokeRequest_ToFabricAsync(request, ct);

        try
        {
            await foreach (var response in channel.Reader.ReadAllAsync(activityCts.Token))
            {
                activity.Release();   // reset timeout
                yield return response;
            }
        }
        finally
        {
            activityCts.Cancel();
            activity.Release();

            if (PendingRequests.TryRemove(request.RequestId, out var pending))
                pending.Writer.TryComplete();
        }
    }
    public async Task Receive_InvokeResponse_FromFabricAsync(InvokeResponseDto response)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Receive_InvokeResponse_FromFabricAsync({response})",
                response);
        }

        if (PendingRequests.TryGetValue(response.RequestId, out var channel))
            channel.Writer.TryWrite(response);
    }
    public async Task Receive_InvokeResponseDone_FromFabricAsync(RequestId requestId)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Receive_InvokeResponseDone_FromFabricAsync({requestId})",
                requestId);
        }

        if (PendingRequests.TryRemove(requestId, out var channel))
            channel.Writer.TryComplete();
    }

    private async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(InvokeRequestDto request, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({request})",
                request);
        }

        if (Services.TryGetValue(request.ServiceId, out var hubHosts) == false)
            yield break;

        var sseHosts = hubHosts.Values
            .Where(sseHost =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (request.SessionId == null && request.UserId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (request.SessionId != null && sseHost.SessionId == request.SessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (request.UserId != null && sseHost.UserId == request.UserId));

        foreach (var sseHost in sseHosts)
        {
            var responses = sseHost.InvokeAsync(request, ct);
            await foreach (var response in responses)
            {
                yield return response;
            }
        }
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
