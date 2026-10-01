using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;
using System.Net.ServerSentEvents;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Sse;

public class SseServiceSubscription : IServiceSubscription
    , IServerConnection
{
    private byte closed;

    public IServerAuthenticationService AuthenticationService { get; }

    private readonly ServiceSubscriptionCollection ServiceSubscriptionCollection;
    private readonly FabricClient FabricClient;

    private readonly CancellationTokenSource Cts = new();

    public Channel<SseEvent> Channel { get; }
    public StreamingCache StreamingCache { get; }
    public ClientConnectionId ClientConnectionId { get; }
    public ServiceSubscriptionId ServiceSubscriptionId { get; private set; }
    public ServiceId ServiceId { get; }
    public SessionId SessionId { get; }
    public UserId UserId { get; }

    public SseServiceSubscription(
        IServerAuthenticationService authenticationService,
        ServerConnectionCollection serverConnectionCollection,
        ServiceSubscriptionCollection serviceSubscriptionCollection,
        StreamingCache streamingCache,
        FabricClient fabricClient,
        ServiceId serviceId,
        UserId userId,
        SessionId sessionId)
    {
        AuthenticationService = authenticationService;
        ServiceSubscriptionCollection = serviceSubscriptionCollection;
        FabricClient = fabricClient;
        StreamingCache = streamingCache;
        ServiceId = serviceId;
        SessionId = sessionId;
        UserId = userId;
        Channel = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
        ServiceSubscriptionId = serviceSubscriptionCollection.Add(this, serviceId);
        ClientConnectionId = serverConnectionCollection.AddConnection(this);
    }

    readonly ConcurrentDictionary<RequestId, int> Requests = [];

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        //Console.WriteLine($"SseServiceSubscription {Id} started");
        await FabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(ServiceSubscriptionId.Value.ToString(), "ServiceSubscriptionId");
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
                ServiceSubscriptionCollection.Remove(ServiceSubscriptionId);
                Cts.Dispose();
            }
        }
    }

    // SEND
    public async Task SendRequestAsync(SendRequestDto sendRequest, CancellationToken ct)
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
    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        // TODO kijken waarom dit zo complex is
        Requests.TryAdd(sendRequest.Routing.RequestId, 0);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(
            sendRequest.Routing,
            sendRequest.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("SendRequestClientDto", sendRequestClient);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;

        await Channel.Writer.WriteAsync(sseEvent, Cts.Token);
        if (ct.IsCancellationRequested || Requests.TryGetValue(sendRequest.Routing.RequestId, out _) == false)
            return;

        if (Requests.TryRemove(sendRequest.Routing.RequestId, out _))
        {
            // TODO ja deze done moet wel verstuurt worden, want we wachten niet bij SSE
            var done = new SendRequestDoneDto(sendRequest.Routing, false, null);
            await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(done, Cts.Token);
        }
    }
    public async Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Requests.TryRemove(cancel.Routing.RequestId, out _))
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
            var done = new SendRequestDoneDto(cancel.Routing, true, null);
            await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(done, ct);
        }
    }

    public async IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto invokeRequest, [EnumeratorCancellation] CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_InvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        var completion = new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingClientInvokeRequests[invokeRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(
            invokeRequest.Routing, 
            invokeRequest.BinaryData, 
            stateIsChanged, 
            stateData);

        var sseEvent = new SseEvent("InvokeRequestClientDto", invokeRequestClient);
        await Channel.Writer.WriteAsync(sseEvent, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
            if (response.Cancelled)
                throw new TaskCanceledException();
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);

            var enumerable = FabricClient.RegisterRemoteAsyncEnumerableArgumentByte(invokeRequest.Routing, -1);
            await foreach (var item in enumerable)
                yield return item;
        }
        finally
        {
            StreamingCache.PendingClientInvokeRequests.TryRemove(invokeRequest.Routing.RequestId, out _);
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
            message.ExceptionMessage,
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
            message.ExceptionMessage,
            message.BinaryData,
            stateIsChanged,
            stateData);
        var sseEvent = new SseEvent("FabricStreamingResponseClientDto", streamingResponse);
        await Channel.Writer.WriteAsync(sseEvent, ct);
    }
}