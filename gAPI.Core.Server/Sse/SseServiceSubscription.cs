using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Server.Sse;

public class SseServiceSubscription
    : IServiceSubscription
    , IServerConnection
{
    private readonly CancellationTokenSource Cts = new();
    private readonly Channel<SseEvent> Channel = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    private readonly ServiceSubscriptionCollection ServiceSubscriptionCollection;
    private readonly IServerAuthenticationService AuthenticationService;
    private readonly FabricClient FabricClient;
    private byte closed;

    IServerConnection IServiceSubscription.Connection => this;
    string IServerConnection.Id => Id.Value.ToString();

    public ClientConnectionId ClientConnectionId { get; }
    public ServiceSubscriptionId Id { get; }
    public ServiceId ServiceId { get; }
    public SessionId SessionId { get; }
    public UserId? UserId { get; }
    public Stopwatch Stopwatch { get; }

    public bool IsAuthenticated => UserId != null;

    public SseServiceSubscription(
        IServerAuthenticationService authenticationService,
        ServerConnectionCollection serverConnectionCollection,
        ServiceSubscriptionCollection serviceSubscriptionCollection,
        StreamingCache streamingCache,
        FabricClient fabricClient,
        ServiceId serviceId,
        UserId? userId,
        SessionId sessionId)
    {
        AuthenticationService = authenticationService;
        ServiceSubscriptionCollection = serviceSubscriptionCollection;
        FabricClient = fabricClient;
        ServiceId = serviceId;
        SessionId = sessionId;
        Id = ServiceSubscriptionId.New();
        UserId = userId;
        ClientConnectionId = serverConnectionCollection.Add(this);
        serviceSubscriptionCollection.Add(this, serviceId);
        Stopwatch = Stopwatch.StartNew();
    }

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        //Console.WriteLine($"SseServiceSubscription {Id} started");
        await FabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(Id.Value.ToString(), "ServiceSubscriptionId");
            yield return new SseItem<string>(ClientConnectionId.Value.ToString(), "ClientConnectionId");
            yield return new SseItem<string>(FabricClient.FabricConnectionId.Value.ToString(), "FabricConnectionId");
            yield return new SseItem<string>(FabricClient.FabricManagerId.Value.ToString(), "FabricManagerId");

            while (true)
            {
                SseEvent sseMessage;
                try
                {
                    sseMessage = await Channel.Reader.ReadAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    yield break; // <- GEEN ERROR, normale shutdown
                }
                catch (ChannelClosedException)
                {
                    yield break;
                }

                if (sseMessage.EventData == null) continue;
                yield return new SseItem<string>(sseMessage.EventData, sseMessage.EventName);
            }
        }
        finally
        {
            if (Interlocked.Exchange(ref closed, 1) == 0)
            {
                await FabricClient.UnsubscribeAsync(this, ct);
                ServiceSubscriptionCollection.Remove(Id);
                Cts.Dispose();
            }
        }
    }

    public async Task Send_FabricSendAndForgetRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(
            sendRequest.Routing,
            sendRequest.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("SendAndForgetRequestClientDto", sendRequestClient);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(
            sendRequest.Routing,
            sendRequest.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("SendRequestClientDto", sendRequestClient);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        //if (Requests.TryRemove(cancel.Routing.RequestId, out _))
        {
            var stateIsChanged = AuthenticationService.IsStateDataChanged();
            var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            var cancelClient = new SendRequestCancelledClientDto(
                cancel.Routing,
                cancel.Reason,
                stateIsChanged,
                stateData);
            var sseEvent = new SseEvent("SendRequestCancelledClientDto", cancelClient);
            await Channel.Writer.WriteAsync(sseEvent, ct);
            //var done = new SendRequestDoneDto(cancel.Routing, true, null);
            //await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(done, ct);
        }
    }

    public async Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(
            invokeRequest.Routing,
            invokeRequest.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("InvokeRequestClientDto", invokeRequestClient);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestCancelledClientDto = new InvokeRequestCancelledClientDto(
            cancel.Routing,
            cancel.Reason,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("InvokeRequestCancelledClientDto", invokeRequestCancelledClientDto);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingRequest = new StreamingRequestClientDto(
            message.Routing,
            message.ArgumentIndex,
            message.StreamId,
            message.Cancelled,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("StreamingRequestClientDto", streamingRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponse = new StreamingResponseClientDto(
            message.Routing,
            message.ArgumentIndex,
            message.StreamId,
            message.IsCompleted,
            message.IsCancelled,
            message.Exception,
            message.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("StreamingResponseClientDto", streamingResponse);
        await Channel.Writer.WriteAsync(sseEvent, ct);

    }

    public async Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingRequest = new StreamingRequestClientDto(
            message.Routing,
            message.ArgumentIndex,
            message.StreamId,
            message.Cancelled,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("FabricStreamingRequestClientDto", streamingRequest);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponse = new StreamingResponseClientDto(
            message.Routing,
            message.ArgumentIndex,
            message.StreamId,
            message.IsCompleted,
            message.IsCancelled,
            message.Exception,
            message.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("FabricStreamingResponseClientDto", streamingResponse);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    public async Task Send_OnDisconnected_ToClientAsync(CancellationToken ct)
    {
        var fabricDisconnectedDto = new FabricDisconnectedDto(false);
        var sseEvent = new SseEvent("FabricDisconnectedDto", fabricDisconnectedDto);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_OnRestoreSubscriptions_ToClientAsync(CancellationToken ct)
    {
        var fabricRestoreSubscriptionsDto = new FabricRestoreSubscriptionsDto(false);
        var sseEvent = new SseEvent("FabricRestoreSubscriptionsDto", fabricRestoreSubscriptionsDto);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    public async Task Send_SubscribeAck_ToClientAsync(SubscribeAckDto message, CancellationToken ct)
    {
        var sseEvent = new SseEvent("SubscribeAckDto", message);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
    public async Task Send_UnsubscribeAck_ToClientAsync(UnsubscribeAckDto message, CancellationToken ct)
    {
        var sseEvent = new SseEvent("UnsubscribeAckDto", message);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }

    private readonly ConcurrentQueue<(double time, long bytes)> SendBytesLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceivedBytesLogger = new();

    public (int count, long bytes) GetReceiveBytesPerSecond() => GetSpeed(SendBytesLogger);
    public (int count, long bytes) GetSendBytesPerSecond() => GetSpeed(ReceivedBytesLogger);
    private (int count, long bytes) GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = 0L;
        var count = 0;
        foreach (var item in queue)
        {
            bytes += item.bytes;
            count++;
        }
        return new((int)(count / interval), (int)(bytes / interval));
    }
}